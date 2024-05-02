// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Segments.IPrintStationSegmentRx
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using PharmaProject.BusinessLogic.Misc;

namespace PharmaProject.BusinessLogic.Segments
{
    public interface IPrintStationSegmentRx : IPrintStationSegment
    {
        bool JobRequest(PrintSegJob job);
    }
}