// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.HWOverrides
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System.Linq;

namespace Ephi.Core.UTC;

public class HWOverrides
{
    private readonly HWValue overriddeMask;
    private readonly HWValue overriddenStates;

    public HWOverrides()
    {
        overriddeMask = new HWValue();
        overriddenStates = new HWValue();
    }

    public byte[] GetStatePayload => overriddeMask.Get.Concat(overriddenStates.Get).ToArray();

    public bool SetOverride(PIN pinNumber, LEVEL pinState)
    {
        return overriddeMask.SetPin(pinNumber, LEVEL.HIGH) | overriddenStates.SetPin(pinNumber, pinState);
    }

    public bool UnSetOverride(PIN pinNumber)
    {
        return overriddeMask.SetPin(pinNumber, LEVEL.LOW);
    }

    public void UnSetOverrideAll()
    {
        overriddeMask.Set(0U);
        overriddenStates.Set(0U);
    }

    public LEVEL GetPinOverride(PIN pinNumber)
    {
        if (pinNumber == PIN._NULL)
            return LEVEL.UNDETERMINED;
        var num = 1U << (int)(pinNumber & (PIN._16 | PIN._17));
        if (((int)overriddeMask.AsNum & (int)num) == 0)
            return LEVEL.UNDETERMINED;
        return (overriddenStates.AsNum & num) <= 0U ? LEVEL.LOW : LEVEL.HIGH;
    }

    public uint Compare(HWOverrides other)
    {
        uint num = 0;
        if (!overriddeMask.Equals(other.overriddeMask))
            num |= 8U;
        if (!overriddenStates.Equals(other.overriddenStates))
            num |= 8U;
        return num;
    }

    public uint CompareAndEqualize(HWOverrides other)
    {
        uint num = 0;
        if (overriddeMask.CompareAndSet(other.overriddeMask))
            num |= 8U;
        if (overriddenStates.CompareAndSet(other.overriddenStates))
            num |= 8U;
        return num;
    }

    public bool CompareAndCombine(HWOverrides source)
    {
        var asNum1 = overriddeMask.AsNum;
        var num1 = asNum1;
        var asNum2 = overriddenStates.AsNum;
        var num2 = asNum2;
        var asNum3 = source.overriddeMask.AsNum;
        var asNum4 = source.overriddenStates.AsNum;
        var newVal1 = asNum1 | asNum3;
        var newVal2 = (asNum2 | (asNum3 & asNum4)) & (uint)~((int)asNum3 & ~(int)asNum4);
        if (newVal1.Equals(num1) && newVal2.Equals(num2))
            return false;
        overriddeMask.Set(newVal1);
        overriddenStates.Set(newVal2);
        return true;
    }

    public bool CompareAndAdd(HWValue target)
    {
        var asNum1 = target.AsNum;
        var num = asNum1;
        var asNum2 = overriddeMask.AsNum;
        var asNum3 = overriddenStates.AsNum;
        var newVal = (asNum1 | (asNum2 & asNum3)) & (uint)~((int)asNum2 & ~(int)asNum3);
        if (newVal.Equals(num))
            return false;
        target.Set(newVal);
        return true;
    }

    public uint CorrectAllPins(HWValue inPinStates)
    {
        var asNum1 = inPinStates.AsNum;
        var asNum2 = overriddeMask.AsNum;
        var asNum3 = inPinStates.AsNum;
        var newVal = (asNum1 & ~asNum2) | (asNum3 & asNum2);
        inPinStates.Set(newVal);
        return newVal;
    }

    public bool SetStateFromPayload(ref byte[] payload)
    {
        if (payload.Length < 6)
            return false;
        var asNum1 = (int)overriddeMask.AsNum;
        var asNum2 = overriddeMask.AsNum;
        overriddeMask.Set(payload.Take(3).ToArray());
        overriddenStates.Set(payload.Skip(3).Take(3).ToArray());
        payload = payload.Skip(6).ToArray();
        var asNum3 = (int)overriddeMask.AsNum;
        return asNum1 != asNum3 || (int)asNum2 != (int)overriddenStates.AsNum;
    }

    public override bool Equals(object obj)
    {
        return (obj is HWOverrides hwOverrides ? (int)hwOverrides.Compare(this) : 1) == 0;
    }

    public override int GetHashCode()
    {
        return $"{overriddeMask.GetHashCode()}-{overriddenStates.GetHashCode()}".GetHashCode();
    }
}