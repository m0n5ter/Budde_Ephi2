// Decompiled with JetBrains decompiler
// Type: PharmaProject.Wms_Communication.Messages.AnmeldungLabeldruck
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using System.Collections.Generic;

namespace PharmaProject.Wms_Communication.Messages
{
    internal class AnmeldungLabeldruck : BaseMessage
    {
        public AnmeldungLabeldruck(
            bool printer1Available,
            bool printer2Available,
            bool secondLogin,
            bool onlyReceiveCheckCode,
            byte[] barcode,
            uint location)
            : base(GetAndIncrementMessageCounter(), 40U, location)
        {
            uint num = 0;
            if (printer1Available)
                num |= 1U;
            if (printer2Available)
                num |= 2U;
            if (secondLogin)
                num |= 4U;
            if (onlyReceiveCheckCode)
                num |= 8U;
            var byteList = new List<byte>();
            var bytes = BitConverter.GetBytes(num);
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