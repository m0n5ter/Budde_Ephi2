// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Wms_Communication.Messages.AnforderungPackmittel
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;

namespace PharmaProject.BusinessLogic.Wms_Communication.Messages
{
    internal class AnforderungPackmittel : BaseMessage
    {
        public AnforderungPackmittel(uint counter, uint location, byte[] functionData)
            : base(counter, 60U, location)
        {
            FunctionData = functionData;
        }

        public uint PackagingID => BitConverter.ToUInt32(TakeBytesFromFunctionData(0, 4), 0);

        public uint PackageID => BitConverter.ToUInt32(TakeBytesFromFunctionData(4, 4), 0);
    }
}