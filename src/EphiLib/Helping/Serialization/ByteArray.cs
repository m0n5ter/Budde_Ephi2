// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.Serialization.ByteArray
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;

namespace Ephi.Core.Helping.Serialization;

public static class ByteArray
{
    public enum ENDIAN
    {
        LITTLE,
        BIG
    }

    public static byte[] UInt16(ushort val)
    {
        return new byte[2]
        {
            (byte)((uint)val >> 8),
            (byte)(val & (uint)byte.MaxValue)
        };
    }

    public static ushort UInt16(byte[] arr, int idx)
    {
        return (ushort)UInt(arr, idx, 2);
    }

    public static uint UInt32(byte[] arr, int idx)
    {
        return UInt(arr, idx, 4);
    }

    private static uint UInt(byte[] arr, int idx, int bytes)
    {
        uint num1 = 0;
        for (var index = 0; index < bytes; ++index)
        {
            var num2 = (bytes - 1 - index) * 8;
            num1 |= (uint)arr[idx + index] << num2;
        }

        return num1;
    }

    public static byte[] Convert<T>(T num, ENDIAN end = ENDIAN.LITTLE) where T : struct
    {
        var numArray = new byte[0];
        if (typeof(T) == typeof(uint))
            numArray = BitConverter.GetBytes((uint)(ValueType)num);
        if (typeof(T) == typeof(ushort))
            numArray = BitConverter.GetBytes((ushort)(ValueType)num);
        if (end == ENDIAN.BIG)
            Array.Reverse(numArray);
        return numArray;
    }

    public static T DeepClone<T>(this T obj) where T : class
    {
        if (obj == null)
            return default;
        using (var serializationStream = new MemoryStream())
        {
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(serializationStream, obj);
            serializationStream.Position = 0L;
            return (T)binaryFormatter.Deserialize(serializationStream);
        }
    }

    public static bool DeepEqual<T>(T a, T b) where T : class
    {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;
        var empty1 = string.Empty;
        var empty2 = string.Empty;
        var xmlSerializer = new XmlSerializer(typeof(T));
        using (var output = new StringWriter())
        {
            using (var xmlWriter = XmlWriter.Create(output))
            {
                xmlSerializer.Serialize(xmlWriter, a);
                empty1 = output.ToString();
            }
        }

        using (var output = new StringWriter())
        {
            using (var xmlWriter = XmlWriter.Create(output))
            {
                xmlSerializer.Serialize(xmlWriter, b);
                empty2 = output.ToString();
            }
        }

        return empty1 == empty2;
    }
}