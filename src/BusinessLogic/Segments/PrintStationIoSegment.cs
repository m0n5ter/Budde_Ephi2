// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Segments.PrintStationIoSegment
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
    public class PrintStationIoSegment :
        IPrintStationSegmentRx,
        IPrintStationSegment,
        IPrintStationSegmentTx
    {
        private const int INVALIDATE_DEBOUNCE_ms = 1000;
        private readonly InPin inSensor;
        private readonly OutPin outMotor;
        public readonly uint SegNum;
        private bool allowDispatch;
        private readonly DelayedEvent CheckValid;
        private Conditional dispatch;
        private Conditional dispatchLoad;
        private IPrintStationSegmentRx downstreamNeighbor;
        protected bool evalPending;
        protected bool evalRunning;
        protected object EvaluateLock = new object();
        private PrintSegJob job;
        private Conditional load;
        private SEGMENT_STATE state;
        private DateTime stateChanged = DateTime.Now;

        public PrintStationIoSegment(uint segNum, InPin inSensor, OutPin outMotor)
        {
            this.inSensor = inSensor;
            this.outMotor = outMotor;
            SegNum = segNum;
            inSensor.Utc.OnStatusChanged += Utc_OnStatusChanged;
            inSensor.OnStateChanged += InSensor_OnStateChanged;
            InitScripts();
            CheckValid = new DelayedEvent(1000U, DoCheckValid);
        }

        public TimeSpan StateAge => DateTime.Now.Subtract(stateChanged);

        public event Action<IPrintStationSegment> OnPrintStationSegmentStateChanged;

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
                        Job = null;
                        break;
                    case SEGMENT_STATE.OCCUPIED:
                        if (Job == null)
                        {
                            Job = PrintSegJob.Make(PRINT_SEG_JOB_TYPE.NO_PRINT);
                        }

                        break;
                    case SEGMENT_STATE.DISPATCHING:
                        Job = null;
                        break;
                }

                var segmentStateChanged = OnPrintStationSegmentStateChanged;
                if (segmentStateChanged != null)
                    segmentStateChanged(this);
                Evaluate();
                CheckValid.Start();
            }
        }

        public PrintSegJob Job
        {
            get => job;
            private set
            {
                if (job == value)
                    return;
                job = value;
                Evaluate();
            }
        }

        public PRINT_SEG_JOB_TYPE? GetJobType(string forBarcode)
        {
            var job = Job;
            if (forBarcode.Equals(job?.BarcodeSide))
                return job.JobType;
            return DownstreamNeighbor?.GetJobType(forBarcode);
        }

        public void SetJobType(string forBarcode, PRINT_SEG_JOB_TYPE jobType)
        {
            var job = Job;
            if (forBarcode.Equals(job?.BarcodeSide))
                job.JobType = jobType;
            else
                DownstreamNeighbor?.SetJobType(forBarcode, jobType);
        }

        public bool JobRequest(PrintSegJob job)
        {
            DoCheckValid();
            if (State == SEGMENT_STATE.NONE || Job != null)
                return false;
            Job = job;
            return true;
        }

        public IPrintStationSegmentRx DownstreamNeighbor
        {
            get => downstreamNeighbor;
            set
            {
                if (downstreamNeighbor != null)
                    downstreamNeighbor.OnPrintStationSegmentStateChanged -= DownstreamNeighbor_OnStateChanged;
                downstreamNeighbor = value;
                if (downstreamNeighbor == null)
                    return;
                downstreamNeighbor.OnPrintStationSegmentStateChanged += DownstreamNeighbor_OnStateChanged;
            }
        }

        public bool AllowDispatch
        {
            get => allowDispatch;
            set
            {
                if (allowDispatch == value)
                    return;
                allowDispatch = value;
                Evaluate();
            }
        }

        private void InitScripts()
        {
            var utc = inSensor.Utc;
            load = utc.MakeConditionalStatement(string.Format("PrintIO Seg {0}.load", SegNum), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(5000U).AddCondition(inSensor)
                .AddOutputState(outMotor);
            dispatch = utc.MakeConditionalStatement(string.Format("PrintIO Seg {0}.dispatch", SegNum), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(5000U)
                .AddCondition(inSensor, PIN_STATE.INACTIVE).AddOutputState(outMotor);
            dispatchLoad = utc.MakeConditionalMacro(string.Format("PrintIO Seg.dispatch + load {0}", SegNum)).AddStatement(dispatch).AddStatement(load);
            load.OnStateChanged += Script_OnStateChanged;
            dispatch.OnStateChanged += Script_OnStateChanged;
        }

        protected void Evaluate()
        {
            lock (EvaluateLock)
            {
                evalPending = true;
                if (evalRunning)
                    return;
                evalRunning = true;
            }

            while (evalRunning)
            {
                lock (EvaluateLock)
                {
                    evalPending = false;
                }

                try
                {
                    DoEvaluate();
                }
                catch (Exception ex)
                {
                    evalRunning = false;
                    throw ex;
                }

                lock (EvaluateLock)
                {
                    if (!evalPending)
                    {
                        evalRunning = false;
                        break;
                    }
                }
            }
        }

        private void DoEvaluate()
        {
            DoCheckValid();
            switch (State)
            {
                case SEGMENT_STATE.IDLE:
                    if (Job == null)
                        break;
                    load.Run();
                    State = SEGMENT_STATE.LOADING;
                    break;
                case SEGMENT_STATE.LOADING:
                    if (load.IsRunningOrAboutToBe)
                        break;
                    State = load.IsTimedOut ? SEGMENT_STATE.IDLE : SEGMENT_STATE.OCCUPIED;
                    break;
                case SEGMENT_STATE.OCCUPIED:
                    if (!AllowDispatch && Job.JobType != PRINT_SEG_JOB_TYPE.NO_PRINT)
                        break;
                    var downstreamNeighbor = DownstreamNeighbor;
                    if ((downstreamNeighbor != null ? downstreamNeighbor.JobRequest(Job) ? 1 : 0 : 0) == 0)
                        break;
                    dispatch.Run();
                    State = SEGMENT_STATE.DISPATCHING;
                    break;
                case SEGMENT_STATE.DISPATCHING:
                    if (dispatch.IsRunningOrAboutToBe)
                    {
                        if (Job == null)
                            break;
                        dispatchLoad.Run();
                        State = SEGMENT_STATE.DISPATCHING_LOADING;
                        break;
                    }

                    State = dispatch.IsTimedOut ? SEGMENT_STATE.OCCUPIED : SEGMENT_STATE.IDLE;
                    break;
                case SEGMENT_STATE.DISPATCHING_LOADING:
                    if (dispatch.IsRunningOrAboutToBe)
                        break;
                    State = SEGMENT_STATE.LOADING;
                    break;
            }
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

        private void Utc_OnStatusChanged(RemoteUtc utc)
        {
            if (utc.Status != UTC_STATUS.OPERATIONAL)
                return;
            Invalidate();
        }

        private void DownstreamNeighbor_OnStateChanged(IPrintStationSegment obj)
        {
            Evaluate();
        }

        private void InSensor_OnStateChanged(InPin obj)
        {
            CheckValid.Start();
        }

        private void Script_OnStateChanged(Conditional obj)
        {
            Evaluate();
        }
    }
}