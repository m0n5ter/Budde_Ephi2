// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.Timeout`1
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;

namespace Ephi.Core.UTC.ConditionalStatements;

public class Timeout<T> where T : Timeout<T>
{
    protected Timeout()
    {
    }

    public TIMEOUT_RANGE Range { get; private set; }

    public ushort Multiplier { get; private set; }

    public byte[] Bytes(INPUT_CONDITION_TYPE type, BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        if (Multiplier == 0)
            return new byte[0];
        if (type != INPUT_CONDITION_TYPE.GLOBAL_TIMEOUT)
            type = INPUT_CONDITION_TYPE.TIMEOUT_CONDITION;
        var num = Convert.ToByte((int)type << 5);
        if (interpretation == BOOL_INTERPRET.NEGATED)
            num |= 16;
        return new byte[3]
        {
            num,
            (byte)Range,
            Convert.ToByte(Math.Min(byte.MaxValue, Multiplier - 1))
        };
    }

    public static T Make(TIMEOUT_RANGE range, ushort multiplier)
    {
        if (multiplier > 256)
            throw new IndexOutOfRangeException("Multiplier cannot exceed 256");
        var instance = (T)Activator.CreateInstance(typeof(T), true);
        instance.Range = range;
        instance.Multiplier = multiplier;
        return instance;
    }
}