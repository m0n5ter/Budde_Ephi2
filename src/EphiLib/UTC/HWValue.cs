// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.HWValue
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;

namespace Ephi.Core.UTC;

public class HWValue
{
    public HWValue()
        : this(0U)
    {
    }

    public HWValue(uint initial)
    {
        Get = new byte[3];
        Set(initial);
    }

    public byte[] Get { get; }

    public uint AsNum
    {
        get
        {
            uint asNum = 0;
            for (var index = 0; index < 3; ++index)
                asNum |= (uint)Get[index] << ((2 - index) * 8);
            return asNum;
        }
    }

    public void Set(byte[] newVal)
    {
        Array.Copy(newVal, Get, 3);
    }

    public void Set(uint newVal)
    {
        for (var index = 0; index < 3; ++index)
            Get[index] = (byte)((newVal >> ((2 - index) * 8)) & byte.MaxValue);
    }

    public void Set(HWValue other)
    {
        Set(other.Get);
    }

    public bool CompareAndSet(HWValue other)
    {
        if (Equals(other))
            return false;
        Set(other);
        return true;
    }

    public LEVEL GetPin(PIN pinNumber)
    {
        if (pinNumber == PIN._NULL)
            return LEVEL.UNDETERMINED;
        return (AsNum & (1U << (int)(pinNumber & (PIN._16 | PIN._17)))) <= 0U ? LEVEL.LOW : LEVEL.HIGH;
    }

    public bool SetPin(PIN pinNumber, LEVEL pinState)
    {
        if (GetPin(pinNumber) == pinState)
            return false;
        var asNum = AsNum;
        var num = 1U << (int)(pinNumber & (PIN._16 | PIN._17));
        Set(pinState != LEVEL.HIGH ? asNum & ~num : asNum | num);
        return true;
    }

    public override bool Equals(object obj)
    {
        return (obj is HWValue hwValue ? hwValue.AsNum : uint.MaxValue).Equals(AsNum);
    }

    public override int GetHashCode()
    {
        return AsNum.GetHashCode();
    }
}