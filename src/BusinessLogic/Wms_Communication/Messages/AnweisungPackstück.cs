// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Wms_Communication.Messages.AnweisungPackstück
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using PharmaProject.BusinessLogic.Misc;

namespace PharmaProject.BusinessLogic.Wms_Communication.Messages
{
    internal class AnweisungPackstück : BaseMessage
    {
        public AnweisungPackstück(uint counter, uint location, byte[] functionData)
            : base(counter, 2U, location)
        {
            FunctionData = functionData;
        }

        public WMS_TOTE_DIRECTION Direction => (WMS_TOTE_DIRECTION)BitConverter.ToUInt32(TakeBytesFromFunctionData(0, 4), 0);

        public uint Value1 => BitConverter.ToUInt32(TakeBytesFromFunctionData(4, 4), 0);

        public byte[] Barcode => TakeBytesFromFunctionData(8, 32);
    }
}