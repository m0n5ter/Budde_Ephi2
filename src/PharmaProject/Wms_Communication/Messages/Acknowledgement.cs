// Decompiled with JetBrains decompiler
// Type: PharmaProject.Wms_Communication.Messages.Acknowledgement
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe


namespace PharmaProject.Wms_Communication.Messages
{
    internal class Acknowledgement : BaseMessage
    {
        public Acknowledgement(uint messageCounter)
            : base(messageCounter, 0U, 0U)
        {
        }
    }
}