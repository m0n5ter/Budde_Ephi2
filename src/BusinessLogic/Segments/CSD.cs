// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Segments.CSD
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using System.Collections.Generic;
using System.Linq;
using Ephi.Core.Helping;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Devices;
using PharmaProject.BusinessLogic.Locations;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.UTC;

namespace PharmaProject.BusinessLogic.Segments
{
    public class CSD
    {
        private static readonly List<CSD> allCsds = new List<CSD>();
        private static readonly uint DEBOUNCE_INVALIDATE_ms = 1000;
        private TABLE_POSITION currentTablePosition;
        public bool InvalidateRouteRequested;
        protected BaseLocation parent;
        private Route route;
        private CSD_PinsAndScripts scripts;
        private SEGMENT_STATE state;
        private DateTime stateChanged = DateTime.Now;
        private readonly DelayedEvent stateChangedCheckValid;
        private readonly object stateLock = new object();

        public CSD(uint locationNumber, uint csdNum, CSD_PinsAndScripts scripts, BaseLocation parent)
        {
            LocationNumber = locationNumber;
            CsdNum = csdNum;
            route = null;
            this.parent = parent;
            Scripts = scripts;
            allCsds.Add(this);
            stateChangedCheckValid = new DelayedEvent(DEBOUNCE_INVALIDATE_ms, StateCheckValid);
        }

        public uint LocationNumber { get; }

        public uint CsdNum { get; }

        public Route Route
        {
            get => route;
            set
            {
                if (route == value)
                    return;
                route = value;
                if (value == null)
                    parent.Log(string.Format("\tCSD{0}, ROUTE CLEARED", CsdNum));
                else
                    parent.Log(string.Format("\tCSD{0}, ROUTE SET {1}", CsdNum, value));
            }
        }

        public CSD_PinsAndScripts Scripts
        {
            get => scripts;
            protected set
            {
                if (scripts != null)
                {
                    Scripts.TableUp.OnStateChanged -= TableUp_OnStateChanged;
                    Scripts.TableDown.OnStateChanged -= TableDown_OnStateChanged;
                    if (scripts.LoadNormal != null)
                        scripts.LoadNormal.OnStateChanged -= Load_OnStateChanged;
                    if (scripts.LoadAlternative != null)
                        scripts.LoadAlternative.OnStateChanged -= Load_OnStateChanged;
                    if (scripts.DispatchNormal != null)
                        scripts.DispatchNormal.OnStateChanged -= Dispatch_OnStateChanged;
                    if (scripts.DispatchAlternative != null)
                        scripts.DispatchAlternative.OnStateChanged -= Dispatch_OnStateChanged;
                    foreach (var sensorPin in SensorPins)
                        sensorPin?.UnregisterDebouncedPinStateHandler(DebouncedCheckValid);
                }

                scripts = value;
                if (value == null)
                    return;
                Scripts.TableUp.OnStateChanged += TableUp_OnStateChanged;
                Scripts.TableDown.OnStateChanged += TableDown_OnStateChanged;
                if (scripts.LoadNormal != null)
                    scripts.LoadNormal.OnStateChanged += Load_OnStateChanged;
                if (scripts.LoadAlternative != null)
                    scripts.LoadAlternative.OnStateChanged += Load_OnStateChanged;
                if (scripts.DispatchNormal != null)
                    scripts.DispatchNormal.OnStateChanged += Dispatch_OnStateChanged;
                if (scripts.DispatchAlternative != null)
                    scripts.DispatchAlternative.OnStateChanged += Dispatch_OnStateChanged;
                var debounceTimeout = TimeSpan.FromMilliseconds(DEBOUNCE_INVALIDATE_ms);
                foreach (var sensorPin in SensorPins)
                    sensorPin?.RegisterDebouncedPinStateHandler(debounceTimeout, DebouncedCheckValid);
            }
        }

        public bool MeasureOccupiedDebouned
        {
            get
            {
                var timeSpan = TimeSpan.FromMilliseconds(DEBOUNCE_INVALIDATE_ms);
                foreach (var sensorPin in SensorPins)
                    if (sensorPin != null && sensorPin.Active && sensorPin.PinStateAge > timeSpan)
                        return true;
                return false;
            }
        }

        public bool MeasureOccupied
        {
            get
            {
                foreach (var sensorPin in SensorPins)
                    if ((sensorPin != null ? sensorPin.Active ? 1 : 0 : 0) != 0)
                        return true;
                return false;
            }
        }

        public bool MeasureEmptyDebounced
        {
            get
            {
                var timeSpan = TimeSpan.FromMilliseconds(DEBOUNCE_INVALIDATE_ms);
                foreach (var sensorPin in SensorPins)
                    if (sensorPin != null && (!sensorPin.Inactive || sensorPin.PinStateAge < timeSpan))
                        return false;
                return true;
            }
        }

        public bool MeasureEmpty
        {
            get
            {
                foreach (var sensorPin in SensorPins)
                    if ((sensorPin != null ? sensorPin.Inactive ? 1 : 0 : 1) == 0)
                        return false;
                return true;
            }
        }

        public TimeSpan StateAge => DateTime.Now.Subtract(stateChanged);

        public SEGMENT_STATE State
        {
            get => state;
            private set
            {
                lock (stateLock)
                {
                    if (state == value)
                        return;
                    switch (value)
                    {
                        case SEGMENT_STATE.LOADING_PENDING:
                            if (MeasureOccupied || state == SEGMENT_STATE.LOADING)
                                return;
                            break;
                        case SEGMENT_STATE.DISPATCHING_PENDING:
                            if (MeasureEmpty || state == SEGMENT_STATE.DISPATCHING)
                                return;
                            break;
                    }

                    parent.Log(string.Format("CSD {0}, {1} => {2}", CsdNum, state, value));
                    state = value;
                    stateChanged = DateTime.Now;
                }

                HandleStateChanged();
            }
        }

        public bool IsIdle => state == SEGMENT_STATE.IDLE;

        public bool IsOccupied => State == SEGMENT_STATE.OCCUPIED;

        private InPin[] SensorPins
        {
            get
            {
                return new InPin[3]
                {
                    scripts.OccupiedExtra,
                    scripts.OccupiedRollers,
                    scripts.OccupiedBelts
                }.ToList().Where(p => p != null).ToArray();
            }
        }

        public static void RunWatchdog()
        {
            foreach (var allCsd in allCsds)
                switch (allCsd.State)
                {
                    case SEGMENT_STATE.LOADING_PENDING:
                    case SEGMENT_STATE.DISPATCHING_PENDING:
                    case SEGMENT_STATE.LOADING:
                    case SEGMENT_STATE.DISPATCHING:
                        if (allCsd.StateAge > TimeSpan.FromSeconds(10.0)) allCsd.InvalidateState();
                        continue;
                    default:
                        continue;
                }
        }

        ~CSD()
        {
            Scripts = null;
            allCsds.Remove(this);
        }

        private void TableUp_OnStateChanged(Conditional obj)
        {
            if (obj.Status != CONDITIONAL_STATE.FINISHED)
                return;
            currentTablePosition = TABLE_POSITION.UP;
            SendUpdate();
        }

        private void TableDown_OnStateChanged(Conditional obj)
        {
            if (obj.Status != CONDITIONAL_STATE.FINISHED)
                return;
            currentTablePosition = TABLE_POSITION.DOWN;
            SendUpdate();
        }

        protected virtual void HandleStateChanged()
        {
            var onStateChanged = OnStateChanged;
            if (onStateChanged != null)
                onStateChanged(this);
            switch (State)
            {
                case SEGMENT_STATE.IDLE:
                case SEGMENT_STATE.LOADING:
                case SEGMENT_STATE.OCCUPIED:
                case SEGMENT_STATE.DISPATCHING:
                    SendUpdate();
                    break;
            }

            stateChangedCheckValid.Start();
        }

        private void SendUpdate()
        {
            NdwConnectCommunicator.CsdStatusUpdate(LocationNumber, CsdNum, State, currentTablePosition, CheckStatusDirection(Scripts.RollersRun, Scripts.RollersDir),
                CheckStatusDirection(Scripts.BeltsRun, Scripts.BeltsDir));
        }

        private string CheckStatusDirection(OutPin run, OutPin dir)
        {
            if (!run.Active)
                return "NO";
            return dir.Active ? "CCW" : "CW";
        }

        private void DebouncedCheckValid(InPin pin)
        {
            CheckValid();
        }

        private void StateCheckValid()
        {
            CheckValid();
        }

        public event Action<CSD> OnStateChanged;

        public void ForceLoadPending()
        {
            State = SEGMENT_STATE.LOADING_PENDING;
        }

        public void ForceDispatchPending()
        {
            State = SEGMENT_STATE.DISPATCHING_PENDING;
        }

        public void ForceDispatching()
        {
            State = SEGMENT_STATE.DISPATCHING;
        }

        public bool CanPassThrough()
        {
            return Helpers.Contains(State, SEGMENT_STATE.IDLE, SEGMENT_STATE.LOADING_PENDING, SEGMENT_STATE.LOADING, SEGMENT_STATE.OCCUPIED);
        }

        public void InvalidateRoute()
        {
            if (!InvalidateRouteRequested)
                return;
            InvalidateRouteRequested = false;
            Route = null;
        }

        public bool CheckValid()
        {
            if (StateAge < TimeSpan.FromMilliseconds(DEBOUNCE_INVALIDATE_ms))
                return true;
            if (Helpers.Contains(State, SEGMENT_STATE.DISPATCHING_PENDING, SEGMENT_STATE.OCCUPIED) && MeasureEmptyDebounced)
            {
                InvalidateState();
                return false;
            }

            if (!Helpers.Contains(State, SEGMENT_STATE.IDLE, SEGMENT_STATE.LOADING_PENDING) || !MeasureOccupiedDebouned)
                return true;
            InvalidateState();
            return false;
        }

        public void InvalidateState()
        {
            InvalidateRouteRequested = true;
            if (MeasureOccupied)
            {
                State = SEGMENT_STATE.OCCUPIED;
            }
            else
            {
                if (!MeasureEmpty)
                    return;
                State = SEGMENT_STATE.IDLE;
            }
        }

        public void Load_OnStateChanged(Conditional obj)
        {
            switch (obj.Status)
            {
                case CONDITIONAL_STATE.ERROR:
                case CONDITIONAL_STATE.TIMED_OUT:
                    InvalidateState();
                    break;
                case CONDITIONAL_STATE.RUNNING:
                    if (!MeasureEmpty)
                        break;
                    State = SEGMENT_STATE.LOADING;
                    break;
                case CONDITIONAL_STATE.RUNNING_PRECONDITIONS:
                case CONDITIONAL_STATE.RUN_REQUESTED:
                    State = SEGMENT_STATE.LOADING_PENDING;
                    break;
                case CONDITIONAL_STATE.FINISHED:
                    var dispatchNormal = Scripts.DispatchNormal;
                    if ((dispatchNormal != null ? dispatchNormal.IsRunningOrAboutToBe ? 1 : 0 : 0) != 0)
                        break;
                    var dispatchAlternative = Scripts.DispatchAlternative;
                    if ((dispatchAlternative != null ? dispatchAlternative.IsRunningOrAboutToBe ? 1 : 0 : 0) != 0)
                        break;
                    State = SEGMENT_STATE.OCCUPIED;
                    break;
            }
        }

        public void Dispatch_OnStateChanged(Conditional obj)
        {
            switch (obj.Status)
            {
                case CONDITIONAL_STATE.ERROR:
                case CONDITIONAL_STATE.TIMED_OUT:
                    InvalidateState();
                    break;
                case CONDITIONAL_STATE.RUNNING:
                    State = SEGMENT_STATE.DISPATCHING;
                    break;
                case CONDITIONAL_STATE.RUNNING_PRECONDITIONS:
                case CONDITIONAL_STATE.RUN_REQUESTED:
                    State = SEGMENT_STATE.DISPATCHING_PENDING;
                    break;
                case CONDITIONAL_STATE.FINISHED:
                    switch (State)
                    {
                        case SEGMENT_STATE.DISPATCHING_PENDING:
                        case SEGMENT_STATE.DISPATCHING:
                            State = SEGMENT_STATE.IDLE;
                            return;
                        case SEGMENT_STATE.LOADING:
                            return;
                        case SEGMENT_STATE.OCCUPIED:
                            return;
                        case SEGMENT_STATE.DISPATCHING_LOADING:
                            State = SEGMENT_STATE.LOADING;
                            return;
                        default:
                            return;
                    }
            }
        }

        public bool LoadNormal()
        {
            if (!IsIdle)
                return false;
            ForceLoadPending();
            return Scripts.LoadNormal.Run(true);
        }

        public bool LoadAlternative()
        {
            if (!CanPassThrough())
                return false;
            ForceLoadPending();
            return Scripts.LoadAlternative.Run(true);
        }

        public bool DispatchNormal()
        {
            if (!IsOccupied)
                return false;
            ForceDispatchPending();
            return Scripts.DispatchNormal.Run(true);
        }

        public bool DispatchAlternative()
        {
            if (!IsOccupied)
                return false;
            ForceDispatchPending();
            return Scripts.DispatchAlternative.Run(true);
        }

        public bool PassThrough()
        {
            if (!CanPassThrough())
                return false;
            ForceLoadPending();
            return Scripts.PassThrough.Run(true);
        }
    }
}