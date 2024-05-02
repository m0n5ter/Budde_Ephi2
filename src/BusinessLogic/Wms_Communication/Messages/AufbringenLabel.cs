// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Wms_Communication.Messages.AufbringenLabel
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using System.Collections.Generic;

namespace PharmaProject.BusinessLogic.Wms_Communication.Messages
{
    internal class AufbringenLabel : BaseMessage
    {
        public AufbringenLabel(bool succeeded, byte[] barcode, uint location)
            : base(GetAndIncrementMessageCounter(), 44U, location)
        {
            var byteList = new List<byte>();
            var bytes = BitConverter.GetBytes(succeeded ? 1 : 0);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            byteList.AddRange(bytes);
            byteList.AddRange(new byte[4]);
            byteList.AddRange(barcode);
            byteList.AddRange(new byte[32 - barcode.Length]);
            FunctionData = byteList.ToArray();
        }
    }
}