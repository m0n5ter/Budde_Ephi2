// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.TimeoutHelpers
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;

namespace Ephi.Core.UTC.ConditionalStatements;

public static class TimeoutHelpers
{
    public static void HalfTime(ref TIMEOUT_RANGE function, ref byte multiplier)
    {
        HalfTime(function, multiplier, out function, out multiplier);
    }

    public static void HalfTime(
        TIMEOUT_RANGE function,
        byte multiplier,
        out TIMEOUT_RANGE outFunction,
        out byte outMultiplier)
    {
        MsToTimeout(GetMs(function, multiplier) / 2U, out outFunction, out outMultiplier);
    }

    public static void AddMs(uint ms, ref TIMEOUT_RANGE function, ref byte multiplier)
    {
        ms += GetMs(function, multiplier);
        MsToTimeout(ms, out function, out multiplier);
    }

    public static void SubtractMs(uint ms, ref TIMEOUT_RANGE function, ref byte multiplier)
    {
        var ms1 = GetMs(function, multiplier);
        ms = ms < ms1 ? ms1 - ms : Math.Min(ms, ms1);
        MsToTimeout(ms, out function, out multiplier);
    }

    public static void MsToTimeout(
        double ms,
        out TIMEOUT_RANGE outFunction,
        out byte outMultiplier)
    {
        MsToTimeout(Convert.ToUInt32(ms), out outFunction, out outMultiplier);
    }

    public static void MsToTimeout(uint ms, out TIMEOUT_RANGE outFunction, out byte outMultiplier)
    {
        if (ms < byte.MaxValue)
        {
            outFunction = TIMEOUT_RANGE.TR_MS;
            outMultiplier = (byte)ms;
        }
        else if (ms < 2550U)
        {
            outFunction = TIMEOUT_RANGE.TR_10MS;
            outMultiplier = (byte)(ms / 10U);
        }
        else if (ms < 25500U)
        {
            outFunction = TIMEOUT_RANGE.TR_100MS;
            outMultiplier = (byte)(ms / 100U);
        }
        else
        {
            outFunction = TIMEOUT_RANGE.TR_SEC;
            outMultiplier = (byte)Math.Min(ms / 1000U, byte.MaxValue);
        }
    }

    private static uint GetMs(TIMEOUT_RANGE function, byte multiplier)
    {
        uint ms = multiplier;
        switch (function)
        {
            case TIMEOUT_RANGE.TR_10MS:
                ms *= 10U;
                break;
            case TIMEOUT_RANGE.TR_100MS:
                ms *= 100U;
                break;
            case TIMEOUT_RANGE.TR_SEC:
                ms *= 1000U;
                break;
        }

        return ms;
    }
}