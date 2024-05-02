// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Misc.SEGMENT_STATE
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll


namespace PharmaProject.BusinessLogic.Misc
{
    public enum SEGMENT_STATE
    {
        NONE,
        IDLE,
        LOADING_PENDING,
        DISPATCHING_PENDING,
        LOADING,
        OCCUPIED,
        DISPATCHING,
        DISPATCHING_LOADING
    }
}