// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Wms_Communication.Messages.RückmeldungPackstück
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using PharmaProject.BusinessLogic.Misc;

namespace PharmaProject.BusinessLogic.Wms_Communication.Messages
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