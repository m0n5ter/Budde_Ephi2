// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Wms_Communication.Messages.Störvektor
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;

namespace PharmaProject.BusinessLogic.Wms_Communication.Messages
{
    public class Störvektor : BaseMessage
    {
        public Störvektor(byte[] störvektor)
            : base(GetAndIncrementMessageCounter(), 100U, 0U)
        {
            FunctionData = störvektor.Length <= 100 ? störvektor : throw new ArgumentOutOfRangeException(nameof(störvektor), "size is too large");
        }
    }
}