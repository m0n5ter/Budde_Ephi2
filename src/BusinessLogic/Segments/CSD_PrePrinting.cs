// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Segments.CSD_PrePrinting
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using PharmaProject.BusinessLogic.Locations;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.UTC;

namespace PharmaProject.BusinessLogic.Segments
{
    internal class CSD_PrePrinting : CSD, IPrintStationSegmentTx, IPrintStationSegment
    {
        private bool allowDispatch;
        private IPrintStationSegmentRx downstreamNeighbor;
        protected bool evalPending;
        protected bool evalRunning;
        protected object EvaluateLock = new object();
        private PrintSegJob job;

        public CSD_PrePrinting(
            uint locationNumber,
            uint csdNum,
            CSD_PinsAndScripts scripts,
            BaseLocation parent)
            : base(locationNumber, csdNum, scripts, parent)
        {
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

        public PrintSegJob Job
        {
            get => job;
            set
            {
                if (job == value)
                    return;
                parent.Log($"PreCsd {State}=>{value}");
                job = value;
                Evaluate();
            }
        }

        public event Action<IPrintStationSegment> OnPrintStationSegmentStateChanged;

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

        protected override void HandleStateChanged()
        {
            switch (State)
            {
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
            segmentStateChanged?.Invoke(this);
            parent.Log($"CSD state changed to : {State}");
            Evaluate();
            base.HandleStateChanged();
        }

        private void Evaluate()
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
            if (State != SEGMENT_STATE.OCCUPIED || !AllowDispatch)
                return;
            if (Job == null)
                Job = PrintSegJob.Make(PRINT_SEG_JOB_TYPE.NO_PRINT);
            var downstreamNeighbor = DownstreamNeighbor;
            if ((downstreamNeighbor != null ? downstreamNeighbor.JobRequest(Job) ? 1 : 0 : 0) == 0 || !DispatchNormal())
                return;
            Job = null;
        }

        private void DownstreamNeighbor_OnStateChanged(IPrintStationSegment obj)
        {
            Evaluate();
        }
    }
}