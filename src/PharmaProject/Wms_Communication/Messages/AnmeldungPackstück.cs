﻿// Decompiled with JetBrains decompiler
// Type: PharmaProject.Wms_Communication.Messages.AnmeldungPackstück
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using System.Collections.Generic;

namespace PharmaProject.Wms_Communication.Messages
{
    internal class AnmeldungPackstück : BaseMessage
    {
        public AnmeldungPackstück(
            uint value1,
            uint value2,
            byte[] barcode,
            uint noOfGoodReads,
            uint noOfBadReads,
            uint location)
            : base(GetAndIncrementMessageCounter(), 1U, location)
        {
            if (barcode.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(barcode), "size is too large");
            var byteList = new List<byte>();
            var bytes1 = BitConverter.GetBytes(value1);
            var bytes2 = BitConverter.GetBytes(value2);
            var bytes3 = BitConverter.GetBytes(noOfGoodReads);
            var bytes4 = BitConverter.GetBytes(noOfBadReads);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes1);
                Array.Reverse(bytes2);
                Array.Reverse(bytes3);
                Array.Reverse(bytes4);
            }

            byteList.AddRange(bytes1);
            byteList.AddRange(bytes2);
            byteList.AddRange(barcode);
            byteList.AddRange(new byte[32 - barcode.Length]);
            byteList.AddRange(bytes3);
            byteList.AddRange(bytes4);
            FunctionData = byteList.ToArray();
        }
    }
}