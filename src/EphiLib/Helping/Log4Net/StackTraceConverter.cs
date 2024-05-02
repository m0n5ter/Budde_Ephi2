// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.Log4Net.StackTraceConverter
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System.Diagnostics;
using System.IO;
using log4net;
using log4net.Core;
using log4net.Layout.Pattern;

namespace Ephi.Core.Helping.Log4Net;

public class StackTraceConverter : PatternLayoutConverter
{
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
        foreach (var frame in new StackTrace().GetFrames())
            if (frame.GetMethod().DeclaringType.Assembly != typeof(LogManager).Assembly)
                writer.WriteLine("{0}.{1} line {2}", frame.GetMethod().DeclaringType.FullName, frame.GetMethod().Name, frame.GetFileLineNumber());
    }
}