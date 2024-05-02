// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.HWValues
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System.Linq;

namespace Ephi.Core.UTC;

public class HWValues
{
    public bool hardEmergency;
    public HWValue inputData;
    public HWOverrides inputOverrides;
    public HWValue realOutputData;
    public HWValue requestedOutputData;
    public HWValue shortCircuitedOutputs;
    public HWValue softEmergencies;
    public bool softEmergency;

    public HWValues()
    {
        realOutputData = new HWValue();
        requestedOutputData = new HWValue();
        inputData = new HWValue();
        inputOverrides = new HWOverrides();
        shortCircuitedOutputs = new HWValue(16777215U);
        softEmergencies = new HWValue();
        hardEmergency = false;
        softEmergency = false;
    }

    public byte[] GetStatePayload
    {
        get
        {
            byte num = 0;
            if (hardEmergency)
                num |= 1;
            if (softEmergency)
                num |= 2;
            return new byte[1] { num }.Concat(requestedOutputData.Get).Concat(realOutputData.Get).Concat(inputData.Get).ToArray();
        }
    }

    public uint Compare(HWValues remote)
    {
        uint num1 = 0;
        if (!realOutputData.Equals(remote.realOutputData))
            num1 |= 2U;
        if (!requestedOutputData.Equals(remote.requestedOutputData))
            num1 |= 2U;
        if (!inputData.Equals(remote.inputData))
            num1 |= 4U;
        var num2 = num1 | inputOverrides.Compare(remote.inputOverrides);
        if (!shortCircuitedOutputs.Equals(remote.shortCircuitedOutputs))
            num2 |= 32U;
        if (!softEmergencies.Equals(remote.softEmergencies))
            num2 |= 64U;
        if (hardEmergency != remote.hardEmergency)
            num2 |= 16U;
        if (softEmergency != remote.softEmergency)
            num2 |= 128U;
        return num2;
    }

    public uint CompareAndEqualize(HWValues remote)
    {
        uint num1 = 0;
        if (realOutputData.CompareAndSet(remote.realOutputData))
            num1 |= 2U;
        if (inputData.CompareAndSet(remote.inputData))
            num1 |= 4U;
        if (!requestedOutputData.Equals(remote.requestedOutputData))
            num1 |= 2U;
        var num2 = num1 | inputOverrides.Compare(remote.inputOverrides);
        if (shortCircuitedOutputs.CompareAndSet(remote.shortCircuitedOutputs))
            num2 |= 32U;
        if (softEmergencies.CompareAndSet(remote.softEmergencies))
            num2 |= 64U;
        if (hardEmergency != remote.hardEmergency)
        {
            num2 |= 16U;
            hardEmergency = remote.hardEmergency;
        }

        if (softEmergency != remote.softEmergency)
            num2 |= 128U;
        return num2;
    }

    public bool SetStateFromPayload(ref byte[] payload)
    {
        if (payload.Length < 11)
            return false;
        var flag1 = (payload[0] & 1) > 0;
        var flag2 = (payload[0] & 2) > 0;
        var num = (false ? 1 : hardEmergency ^ flag1 ? 1 : 0) != 0 ? 1 : softEmergency ^ flag2 ? 1 : 0;
        hardEmergency = flag1;
        softEmergency = flag2;
        var asNum1 = requestedOutputData.AsNum;
        var asNum2 = realOutputData.AsNum;
        var asNum3 = inputData.AsNum;
        requestedOutputData.Set(payload.Skip(1).Take(3).ToArray());
        realOutputData.Set(payload.Skip(4).Take(3).ToArray());
        inputData.Set(payload.Skip(7).Take(3).ToArray());
        payload = payload.Skip(11).ToArray();
        return num != 0 || (int)asNum1 != (int)requestedOutputData.AsNum || (int)asNum2 != (int)realOutputData.AsNum || (int)asNum3 != (int)inputData.AsNum;
    }

    public override bool Equals(object obj)
    {
        var remote = obj as HWValues;
        return obj != null && Compare(remote) == 0U;
    }

    public override int GetHashCode()
    {
        return string.Format("{0}-{1}-{2}-{3}-{4}-{5}-{6}-{7}", realOutputData.GetHashCode(), requestedOutputData.GetHashCode(), inputData.GetHashCode(), inputOverrides.GetHashCode(),
            shortCircuitedOutputs.GetHashCode(), softEmergencies.GetHashCode(), hardEmergency.GetHashCode(), softEmergency.GetHashCode()).GetHashCode();
    }
}