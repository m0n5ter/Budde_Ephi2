// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Segments.CSD_PostPrinting
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using PharmaProject.BusinessLogic.Locations;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.UTC;

namespace PharmaProject.BusinessLogic.Segments
{
    public class CSD_PostPrinting : CSD, IPrintStationSegmentRx, IPrintStationSegment
    {
        private PrintSegJob job;

        public CSD_PostPrinting(
            uint locationNumber,
            uint csdNum,
            CSD_PinsAndScripts scripts,
            BaseLocation parent)
            : base(locationNumber, csdNum, scripts, parent)
        {
        }

        public PrintSegJob Job
        {
            get => job;
            private set
            {
                if (job == value)
                    return;
                job = value;
            }
        }

        public PRINT_SEG_JOB_TYPE? GetJobType(string forBarcode)
        {
            var job = Job;
            return forBarcode.Equals(job?.BarcodeSide) ? job.JobType : new PRINT_SEG_JOB_TYPE?();
        }

        public void SetJobType(string forBarcode, PRINT_SEG_JOB_TYPE jobType)
        {
            var job = Job;
            if (!forBarcode.Equals(job?.BarcodeSide))
                return;
            job.JobType = jobType;
        }

        public event Action<IPrintStationSegment> OnPrintStationSegmentStateChanged;

        public bool JobRequest(PrintSegJob job)
        {
            if (job == null || !IsIdle)
                return false;
            Job = job;
            return LoadNormal();
        }

        protected override void HandleStateChanged()
        {
            base.HandleStateChanged();
            var segmentStateChanged = OnPrintStationSegmentStateChanged;
            if (segmentStateChanged == null)
                return;
            segmentStateChanged(this);
        }
    }
}