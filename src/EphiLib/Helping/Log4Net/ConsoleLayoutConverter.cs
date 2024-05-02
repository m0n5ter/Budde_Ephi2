// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.Log4Net.ConsoleLayoutConverter
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using log4net.Core;
using log4net.Layout.Pattern;

namespace Ephi.Core.Helping.Log4Net;

public class ConsoleLayoutConverter : PatternLayoutConverter
{
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
        var source = loggingEvent.RenderedMessage.Replace("\r", "").Split('\n');
        if (loggingEvent.Level.Value < Level.Fatal.Value)
        {
            writer.Write("  {0}, {1}\r\n  - {2}\r\n\n", loggingEvent.Level, loggingEvent.LoggerName, string.Join("\r\n    ", source).Trim());
        }
        else
        {
            var array = source.Select(l => reworkTabs(l)).ToArray();
            var maxLength = array.Max(l => l.Length);
            var str1 = string.Empty.PadLeft(maxLength + 2, '─');
            var str2 = string.Join("", array.Select(ll => string.Format("  │ {0,-" + maxLength + "} │\r\n", ll, maxLength)));
            writer.Write("  ┌{0}┐\n\r{1}  └{2}┘\r\n\n", str1, str2, str1);
        }

        static string reworkTabs(string line)
        {
            var regex = new Regex(Regex.Escape("\t"));
            int num;
            while ((num = line.IndexOf("\t")) >= 0)
                line = regex.Replace(line, "".PadLeft(4 - num % 4), 1);
            return line;
        }
    }
}