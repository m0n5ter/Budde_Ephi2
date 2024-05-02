// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.OutputState
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;

namespace Ephi.Core.UTC.ConditionalStatements;

public class OutputState : PinState<OutputState>
{
    private bool permanent;

    private OutputState()
    {
    }

    public byte[] Bytes
    {
        get
        {
            var num = (byte)((byte)(128U | Convert.ToByte((int)Dir << 4)) | (uint)Convert.ToByte((int)State << 3));
            if (permanent)
                num |= Convert.ToByte(4U);
            return new byte[2] { num, (byte)Pin };
        }
    }

    public OutputState MakePermanent(bool yes = true)
    {
        permanent = yes;
        return this;
    }
}