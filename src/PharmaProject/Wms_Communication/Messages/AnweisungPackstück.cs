// Decompiled with JetBrains decompiler
// Type: PharmaProject.Wms_Communication.Messages.AnweisungPackstück
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;

namespace PharmaProject.Wms_Communication.Messages
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