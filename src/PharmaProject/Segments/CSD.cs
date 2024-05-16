// Decompiled with JetBrains decompiler
// Type: PharmaProject.Segments.CSD
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using System.Collections.Generic;
using Ephi.Core.Helping;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.Locations;
using PharmaProject.UTC;

namespace PharmaProject.Segments
{
    public class CSD
    {
        private static readonly List<CSD> allCsds = new List<CSD>();
        private static readonly int DEBOUNCE_INVALIDATE_ms = 1000;
        private TABLE_POSITION currentTablePosition;
        public bool InvalidateRouteRequested;
        private readonly BaseLocation parent;
        private Route route;
        private CSD_PinsAndScripts scripts;
        private CSD_STATE state;
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
            stateChangedCheckValid = new DelayedEvent(TimeSpan.FromMilliseconds(DEBOUNCE_INVALIDATE_ms), StateCheckValid);
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
                    parent.Log($"\tCSD{CsdNum}, ROUTE CLEARED");
                else
                    parent.Log($"\tCSD{CsdNum}, ROUTE SET");
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
                    var inPinArray = new InPin[2]
                    {
                        scripts.OccupiedRollers,
                        scripts.OccupiedBelts
                    };
                    foreach (var inPin in inPinArray)
                        inPin?.UnregisterDebouncedPinStateHandler(DebouncedCheckValid);
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
                var inPinArray1 = new InPin[2]
                {
                    scripts.OccupiedRollers,
                    scripts.OccupiedBelts
                };
                foreach (var inPin in inPinArray1)
                    inPin?.RegisterDebouncedPinStateHandler(debounceTimeout, DebouncedCheckValid);
            }
        }

        public bool MeasureOccupiedDebouned
        {
            get
            {
                var timeSpan = TimeSpan.FromMilliseconds(DEBOUNCE_INVALIDATE_ms);
                var inPinArray = new InPin[3]
                {
                    scripts.OccupiedExtra,
                    scripts.OccupiedRollers,
                    scripts.OccupiedBelts
                };
                foreach (var inPin in inPinArray)
                    if (inPin != null)
                    {
                        var pinStateAge = inPin.PinStateAge;
                        if (inPin.Active && inPin.PinStateAge > timeSpan)
                            return true;
                    }

                return false;
            }
        }

        public bool MeasureOccupied
        {
            get
            {
                var inPinArray = new InPin[3]
                {
                    scripts.OccupiedExtra,
                    scripts.OccupiedRollers,
                    scripts.OccupiedBelts
                };
                foreach (var inPin in inPinArray)
                    if ((inPin != null ? inPin.Active ? 1 : 0 : 0) != 0)
                        return true;
                return false;
            }
        }

        public bool MeasureEmptyDebounced
        {
            get
            {
                var timeSpan = TimeSpan.FromMilliseconds(DEBOUNCE_INVALIDATE_ms);
                var inPinArray = new InPin[3]
                {
                    scripts.OccupiedExtra,
                    scripts.OccupiedRollers,
                    scripts.OccupiedBelts
                };
                foreach (var inPin in inPinArray)
                    if (inPin != null && (!inPin.Inactive || inPin.PinStateAge < timeSpan))
                        return false;
                return true;
            }
        }

        public bool MeasureEmpty
        {
            get
            {
                var inPinArray = new InPin[3]
                {
                    scripts.OccupiedExtra,
                    scripts.OccupiedRollers,
                    scripts.OccupiedBelts
                };
                foreach (var inPin in inPinArray)
                    if ((inPin != null ? inPin.Inactive ? 1 : 0 : 1) == 0)
                        return false;
                return true;
            }
        }

        public TimeSpan StateAge => DateTime.Now.Subtract(stateChanged);

        public CSD_STATE State
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
                        case CSD_STATE.LOADING_PENDING:
                            if (MeasureOccupied || state == CSD_STATE.LOADING)
                                return;
                            break;
                        case CSD_STATE.DISPATCHING_PENDING:
                            if (MeasureEmpty || state == CSD_STATE.DISPATCHING)
                                return;
                            break;
                    }

                    parent.Log($"CSD {CsdNum}, {state} => {value}");
                    state = value;
                    stateChanged = DateTime.Now;
                }

                HandleStateChanged();
            }
        }

        public bool IsIdle => state == CSD_STATE.IDLE;

        public bool IsOccupied => State == CSD_STATE.OCCUPIED;

        public static void RunWatchdog()
        {
            foreach (var allCsd in allCsds)
                switch (allCsd.State)
                {
                    case CSD_STATE.LOADING_PENDING:
                    case CSD_STATE.DISPATCHING_PENDING:
                    case CSD_STATE.LOADING:
                    case CSD_STATE.DISPATCHING:
                        if (allCsd.StateAge > TimeSpan.FromSeconds(10.0))
                        {
                            allCsd.InvalidateState();
                        }

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

        private void HandleStateChanged()
        {
            var onStateChanged = OnStateChanged;
            onStateChanged?.Invoke(this);
            switch (State)
            {
                case CSD_STATE.IDLE:
                case CSD_STATE.LOADING:
                case CSD_STATE.OCCUPIED:
                case CSD_STATE.DISPATCHING:
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
            State = CSD_STATE.LOADING_PENDING;
        }

        public void ForceDispatchPending()
        {
            State = CSD_STATE.DISPATCHING_PENDING;
        }

        public bool CanPassThrough()
        {
            return Helpers.Contains(State, CSD_STATE.IDLE, CSD_STATE.LOADING_PENDING, CSD_STATE.LOADING, CSD_STATE.OCCUPIED);
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
            if (Helpers.Contains(State, CSD_STATE.DISPATCHING_PENDING, CSD_STATE.OCCUPIED) && MeasureEmptyDebounced)
            {
                InvalidateState();
                return false;
            }

            if (!Helpers.Contains(State, CSD_STATE.IDLE, CSD_STATE.LOADING_PENDING) || !MeasureOccupiedDebouned)
                return true;
            InvalidateState();
            return false;
        }

        public void InvalidateState()
        {
            InvalidateRouteRequested = true;
            if (MeasureOccupied)
            {
                State = CSD_STATE.OCCUPIED;
            }
            else
            {
                if (!MeasureEmpty)
                    return;
                State = CSD_STATE.IDLE;
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
                    State = CSD_STATE.LOADING;
                    break;
                case CONDITIONAL_STATE.RUNNING_PRECONDITIONS:
                case CONDITIONAL_STATE.RUN_REQUESTED:
                    State = CSD_STATE.LOADING_PENDING;
                    break;
                case CONDITIONAL_STATE.FINISHED:
                    var dispatchNormal = Scripts.DispatchNormal;
                    if ((dispatchNormal != null ? dispatchNormal.IsRunningOrAboutToBe ? 1 : 0 : 0) != 0)
                        break;
                    var dispatchAlternative = Scripts.DispatchAlternative;
                    if ((dispatchAlternative != null ? dispatchAlternative.IsRunningOrAboutToBe ? 1 : 0 : 0) != 0)
                        break;
                    State = CSD_STATE.OCCUPIED;
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
                    State = CSD_STATE.DISPATCHING;
                    break;
                case CONDITIONAL_STATE.RUNNING_PRECONDITIONS:
                case CONDITIONAL_STATE.RUN_REQUESTED:
                    State = CSD_STATE.DISPATCHING_PENDING;
                    break;
                case CONDITIONAL_STATE.FINISHED:
                    State = CSD_STATE.IDLE;
                    break;
            }
        }

        public bool LoadNormal()
        {
            if (!IsIdle)
                return false;
            ForceLoadPending();
            return Scripts.LoadNormal.Run();
        }

        public bool LoadAlternative()
        {
            if (!CanPassThrough())
                return false;
            ForceLoadPending();
            return Scripts.LoadAlternative.Run();
        }

        public bool DispatchNormal()
        {
            if (!IsOccupied)
                return false;
            ForceDispatchPending();
            return Scripts.DispatchNormal.Run();
        }

        public bool DispatchAlternative()
        {
            if (!IsOccupied)
                return false;
            ForceDispatchPending();
            return Scripts.DispatchAlternative.Run();
        }

        public bool PassThrough()
        {
            if (!CanPassThrough())
                return false;
            ForceLoadPending();
            return Scripts.PassThrough.Run();
        }
    }
}