// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.Log4Net.FileLayoutConverter
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System.IO;
using System.Linq;
using log4net.Core;
using log4net.Layout.Pattern;

namespace Ephi.Core.Helping.Log4Net;

public class FileLayoutConverter : PatternLayoutConverter
{
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
        var source = loggingEvent.RenderedMessage.Replace("\r", "").Split('\n');
        if (loggingEvent.Level.Value < Level.Fatal.Value)
        {
            var str1 = string.Format("  {0}, {1} ", loggingEvent.Level, loggingEvent.LoggerName);
            var length = source.Length;
            var str2 = string.Join("\r\n    ", source).Trim();
            if (length < 2)
                writer.Write("{0} - {1}\r\n", str1, str2);
            else
                writer.Write("{0}\r\n  - {1}\r\n", str1, str2);
        }
        else
        {
            var maxLength = source.Max(l => l.Length);
            var str3 = string.Empty.PadLeft(maxLength + 2, '─');
            var str4 = string.Join("", source.Select(ll => string.Format("  │ {0,-" + maxLength + "} │\r\n", ll, maxLength)));
            writer.Write("\r\n  ┌{0}┐\n{1}  └{2}┘\r\n\n", str3, str4, str3);
        }
    }
}