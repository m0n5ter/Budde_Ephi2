// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.Formatting
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Globalization;
using System.Linq;

namespace Ephi.Core.Helping;

public static class Formatting
{
    public static string TitleCase(object obj, string nullResponse = "NULL")
    {
        return TitleCase(obj?.ToString() ?? nullResponse);
    }

    public static string TitleCase(string org, string nullResponse = "NULL")
    {
        if (org == null)
            return nullResponse;
        var empty = string.Empty;
        var flag = true;
        foreach (var c in org)
        {
            if (!flag)
            {
                if (flag = char.IsDigit(c))
                    empty += " ";
            }
            else
            {
                flag = char.IsDigit(c);
            }

            empty += c.ToString();
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(empty.Replace("_", " ").Replace("  ", " ").ToLower() ?? string.Empty);
    }

    public static string BytesToString(byte[] bytes)
    {
        return string.Format(" 0x {0}\n 0d {2}\n 0b {1}", BitConverter.ToString(bytes), string.Join(" ", bytes.ToList().ConvertAll(b => Convert.ToString(b, 2).PadLeft(8, '0')).ToArray()),
            string.Join(" ", bytes.ToList().ConvertAll(b => b.ToString().PadLeft(2, '0')).ToArray()));
    }
}