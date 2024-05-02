// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Wms_Communication.Messages.FehlerLabeldruck
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll


namespace PharmaProject.BusinessLogic.Wms_Communication.Messages
{
    internal class FehlerLabeldruck : BaseMessage
    {
        public FehlerLabeldruck(uint counter, uint location, byte[] functionData)
            : base(counter, 41U, location)
        {
            FunctionData = functionData;
        }

        public byte[] Barcode => TakeBytesFromFunctionData(8, 32);
    }
}