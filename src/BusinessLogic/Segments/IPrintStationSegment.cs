// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Segments.IPrintStationSegment
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using PharmaProject.BusinessLogic.Misc;

namespace PharmaProject.BusinessLogic.Segments
{
    public interface IPrintStationSegment
    {
        SEGMENT_STATE State { get; }

        PrintSegJob Job { get; }

        PRINT_SEG_JOB_TYPE? GetJobType(string forBarcode);

        void SetJobType(string forBarcode, PRINT_SEG_JOB_TYPE jobType);

        event Action<IPrintStationSegment> OnPrintStationSegmentStateChanged;
    }
}