// Decompiled with JetBrains decompiler
// Type: PharmaProject.Wms_Communication.Messages.RückmeldungPackstück
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;

namespace PharmaProject.Wms_Communication.Messages
{
    internal class RückmeldungPackstück : BaseMessage
    {
        public RückmeldungPackstück(WMS_TOTE_DIRECTION direction, byte[] barcode, uint location)
            : base(GetAndIncrementMessageCounter(), 3U, location)
        {
            if (barcode.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(barcode), "Buffer size not correct");
            var dst = new byte[40];
            var bytes = BitConverter.GetBytes((uint)direction);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            Buffer.BlockCopy(bytes, 0, dst, 0, 4);
            Buffer.BlockCopy(barcode, 0, dst, 8, barcode.Length);
            FunctionData = dst;
        }
    }
}