// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Wms_Communication.Messages.LabeldruckErfolgreich
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using System.Linq;
using Ephi.Core.Helping.Lists;

namespace PharmaProject.BusinessLogic.Wms_Communication.Messages
{
    internal class LabeldruckErfolgreich : BaseMessage
    {
        public LabeldruckErfolgreich(uint counter, byte[] functionData, uint location)
            : base(counter, 43U, location)
        {
            FunctionData = functionData;
        }

        public bool UseLabelPress1 => new BitMask(BitConverter.ToUInt32(TakeBytesFromFunctionData(0, 4), 0)).Get(0);

        public bool UseLabelPress2 => new BitMask(BitConverter.ToUInt32(TakeBytesFromFunctionData(0, 4), 0)).Get(1);

        public bool SecondLabelOnSamePrinter => new BitMask(BitConverter.ToUInt32(TakeBytesFromFunctionData(0, 4), 0)).Get(2);

        public bool OnlyReceiveCheckCode => new BitMask(BitConverter.ToUInt32(TakeBytesFromFunctionData(0, 4), 0)).Get(3);

        public byte[] Barcode => FunctionData.Skip(8).Take(32).ToArray();

        public byte[] ComparisonBarcode => FunctionData.Skip(40).Take(32).ToArray();
    }
}