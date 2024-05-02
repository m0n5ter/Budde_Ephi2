// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.Protocol
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Linq;

namespace Ephi.Core.UTC;

public static class Protocol
{
    public const byte STX = 2;
    public const byte ETX = 3;

    private static byte[] TrimToStartIdx(byte[] payload)
    {
        var flag = true;
        var index = 0;
        var length = payload.Length;
        while (index < length - 2)
            if (!flag)
            {
                var num = Array.IndexOf<byte>(payload, 3, index);
                index = num + 1;
                if (num < 0)
                    return new byte[0];
                flag = true;
            }
            else
            {
                if (payload[index] == 2)
                    return payload.Skip(index).ToArray();
                flag = false;
            }

        return new byte[0];
    }

    public static FunctionMessage FindHeader(ref byte[] payload)
    {
        payload = TrimToStartIdx(payload);
        if (payload.Length == 0)
            return null;
        var count = 0;
        var length = payload.Length;
        FunctionMessage header = null;
        while (count + 3 < length)
            if (payload[count++] == 2)
            {
                var msgId = payload[count++];
                if (Enum.IsDefined(typeof(UTC_FUNCTION), payload[count]) && payload[count] != 0)
                {
                    header = new FunctionMessage((UTC_FUNCTION)payload[count++], msgId);
                    break;
                }
            }

        if (header == null)
            return header;
        payload = payload.Skip(count).ToArray();
        return header;
    }

    public static byte[] GetIndices(ref byte[] payload, int startIdx = 0)
    {
        if (payload.Length < startIdx + 1)
            return null;
        int count = payload[startIdx];
        if (count == 0)
            return new byte[0];
        if (payload.Length < count + startIdx + 1)
            return null;
        var array = payload.Skip(startIdx + 1).Take(count).ToArray();
        payload = payload.Skip(startIdx + 1 + count).ToArray();
        return array;
    }
}