// Decompiled with JetBrains decompiler
// Type: PharmaProject.Wms_Communication.Messages.FehlerLabeldruck
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe


namespace PharmaProject.Wms_Communication.Messages
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