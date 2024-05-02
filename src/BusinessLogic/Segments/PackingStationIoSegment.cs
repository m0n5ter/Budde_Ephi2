// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Segments.PackingStationIoSegment
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Misc;

namespace PharmaProject.BusinessLogic.Segments
{
    public class PackingStationIoSegment
    {
        private const int INVALIDATE_DEBOUNCE_ms = 1000;
        private readonly InPin inSensor;
        private readonly OutPin outMotor;
        private readonly DelayedEvent CheckValid;
        private Conditional dispatch;
        private Conditional load;
        private Conditional passthrough;
        private Route route;
        private SEGMENT_STATE state;
        private DateTime stateChanged = DateTime.Now;

        public PackingStationIoSegment(InPin inSensor, OutPin outMotor)
        {
            this.inSensor = inSensor;
            this.outMotor = outMotor;
            inSensor.Utc.OnStatusChanged += Utc_OnStatusChanged;
            inSensor.OnStateChanged += InSensor_OnStateChanged;
            InitScripts();
            CheckValid = new DelayedEvent(1000U, DoCheckValid);
        }

        public TimeSpan StateAge => DateTime.Now.Subtract(stateChanged);

        public SEGMENT_STATE State
        {
            get => state;
            private set
            {
                if (state == value)
                    return;
                state = value;
                stateChanged = DateTime.Now;
                switch (value)
                {
                    case SEGMENT_STATE.IDLE:
                        Route = null;
                        break;
                    case SEGMENT_STATE.OCCUPIED:
                        var route = Route ?? new Route(START.LOAD_CSD1);
                        if (route.Destination == DESTINATION.TBD)
                            route.Destination = DESTINATION.DISPATCH_CSD2;
                        Route = route;
                        break;
                    case SEGMENT_STATE.DISPATCHING:
                        Route = null;
                        break;
                }

                var segmentChanged = SegmentChanged;
                if (segmentChanged != null)
                    segmentChanged(this);
                CheckValid.Start();
            }
        }

        public Route Route
        {
            get => route;
            set
            {
                if (route == value)
                    return;
                route = value;
                var segmentChanged = SegmentChanged;
                if (segmentChanged == null)
                    return;
                segmentChanged(this);
            }
        }

        private void InitScripts()
        {
            var utc = inSensor.Utc;
            load = utc.MakeConditionalStatement("PackingIO Seg load", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(7000U).AddCondition(inSensor).AddOutputState(outMotor);
            dispatch = utc.MakeConditionalStatement("PackingIO Seg dispatch", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(5000U).AddCondition(inSensor, PIN_STATE.INACTIVE)
                .AddOutputState(outMotor);
            passthrough = utc.MakeConditionalMacro("PackingIO Seg.load + dispatch").AddStatement(load).AddStatement(dispatch);
            load.OnStateChanged += LoadScript_OnStateChanged;
            dispatch.OnStateChanged += DispatchScript_OnStateChanged;
        }

        public bool Load()
        {
            if (State != SEGMENT_STATE.IDLE || !load.Run(true))
                return false;
            State = SEGMENT_STATE.LOADING_PENDING;
            return true;
        }

        public bool Dispatch()
        {
            if (State != SEGMENT_STATE.OCCUPIED || !dispatch.Run(true))
                return false;
            State = SEGMENT_STATE.DISPATCHING_PENDING;
            return true;
        }

        public bool Passthrough()
        {
            switch (State)
            {
                case SEGMENT_STATE.LOADING_PENDING:
                case SEGMENT_STATE.LOADING:
                    if (passthrough.Run(true))
                    {
                        State = SEGMENT_STATE.DISPATCHING_PENDING;
                        return true;
                    }

                    break;
                case SEGMENT_STATE.OCCUPIED:
                    if (dispatch.Run(true))
                    {
                        State = SEGMENT_STATE.DISPATCHING_PENDING;
                        return true;
                    }

                    break;
            }

            return false;
        }

        private void DoCheckValid()
        {
            if (inSensor.PinStateAge < TimeSpan.FromMilliseconds(1000.0))
                return;
            switch (State)
            {
                case SEGMENT_STATE.IDLE:
                case SEGMENT_STATE.OCCUPIED:
                    Invalidate();
                    break;
            }
        }

        private void Invalidate()
        {
            State = inSensor.Active ? SEGMENT_STATE.OCCUPIED : SEGMENT_STATE.IDLE;
        }

        public event Action<PackingStationIoSegment> SegmentChanged;

        private void Utc_OnStatusChanged(RemoteUtc utc)
        {
            if (utc.Status != UTC_STATUS.OPERATIONAL)
                return;
            Invalidate();
        }

        private void InSensor_OnStateChanged(InPin obj)
        {
            CheckValid.Start();
        }

        private void LoadScript_OnStateChanged(Conditional obj)
        {
            switch (obj.Status)
            {
                case CONDITIONAL_STATE.RUNNING:
                    State = SEGMENT_STATE.LOADING;
                    break;
                case CONDITIONAL_STATE.FINISHED:
                    State = SEGMENT_STATE.OCCUPIED;
                    break;
                case CONDITIONAL_STATE.CANCELLED:
                case CONDITIONAL_STATE.TIMED_OUT:
                    Invalidate();
                    break;
            }
        }

        private void DispatchScript_OnStateChanged(Conditional obj)
        {
            switch (obj.Status)
            {
                case CONDITIONAL_STATE.RUNNING:
                    State = SEGMENT_STATE.DISPATCHING;
                    break;
                case CONDITIONAL_STATE.FINISHED:
                    State = SEGMENT_STATE.IDLE;
                    break;
                case CONDITIONAL_STATE.CANCELLED:
                case CONDITIONAL_STATE.TIMED_OUT:
                    Invalidate();
                    break;
            }
        }
    }
}