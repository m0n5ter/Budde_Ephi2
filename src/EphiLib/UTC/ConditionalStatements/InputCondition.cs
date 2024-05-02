// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.InputCondition
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;

namespace Ephi.Core.UTC.ConditionalStatements;

public class InputCondition : PinState<InputCondition>, IBaseCondition
{
    private InputCondition()
    {
    }

    public void Invalidate()
    {
    }

    public byte[] Bytes
    {
        get
        {
            return new byte[2]
            {
                (byte)((byte)(192U | Convert.ToByte((int)Dir << 4)) | (uint)Convert.ToByte((int)State << 3)),
                (byte)Pin
            };
        }
    }

    public StatePin[] AllInPins => new StatePin[1] { this };
}