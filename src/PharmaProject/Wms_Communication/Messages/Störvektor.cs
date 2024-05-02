// Decompiled with JetBrains decompiler
// Type: PharmaProject.Wms_Communication.Messages.Störvektor
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;

namespace PharmaProject.Wms_Communication.Messages
{
    internal class Störvektor : BaseMessage
    {
        public Störvektor(byte[] störvektor)
            : base(GetAndIncrementMessageCounter(), 100U, 0U)
        {
            FunctionData = störvektor.Length <= 100 ? störvektor : throw new ArgumentOutOfRangeException(nameof(störvektor), "size is too large");
        }
    }
}