// Decompiled with JetBrains decompiler
// Type: PharmaProject.Wms_Communication.Messages.AufbringenLabel
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using System.Collections.Generic;

namespace PharmaProject.Wms_Communication.Messages
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