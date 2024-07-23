// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Wms_Communication.Messages.AufbringenLabel
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System.Collections.Generic;

namespace PharmaProject.BusinessLogic.Wms_Communication.Messages
{
    internal class LabelCheckRequest : BaseMessage
    {
        public LabelCheckRequest(byte[] checkBarcode, byte[] scanBarcode, uint location)
            : base(GetAndIncrementMessageCounter(), 46U, location)
        {
            var byteList = new List<byte>();
            byteList.AddRange(new byte[8]);
            byteList.AddRange(checkBarcode);
            byteList.AddRange(new byte[32 - checkBarcode.Length]);
            byteList.AddRange(scanBarcode);
            byteList.AddRange(new byte[32 - scanBarcode.Length]);
            FunctionData = byteList.ToArray();
        }
    }
}