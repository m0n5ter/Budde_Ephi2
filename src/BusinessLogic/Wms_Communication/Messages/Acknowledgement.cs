// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Wms_Communication.Messages.Acknowledgement
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll


namespace PharmaProject.BusinessLogic.Wms_Communication.Messages
{
    internal class Acknowledgement : BaseMessage
    {
        public Acknowledgement(uint messageCounter)
            : base(messageCounter, 0U, 0U)
        {
        }
    }
}