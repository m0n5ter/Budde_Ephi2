// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.Log4Net.Log4NetHelpers
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System.IO;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace Ephi.Core.Helping.Log4Net;

public static class Log4NetHelpers
{
    public static void Init()
    {
        XmlConfigurator.Configure();
    }

    public static ILog GetLogger(string name)
    {
        return LogManager.GetLogger(name);
    }

    public static ILog AddIsolatedLogger(string name, string path = ".\\")
    {
        var repository = (Hierarchy)LogManager.GetRepository();
        name = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
        path = string.Format("{0}.log", Path.Combine(path, name));
        name = "ISO." + name;
        var name1 = name;
        var logger = repository.GetLogger(name1) as Logger;
        logger.Level = Level.All;
        var patternLayout = new PatternLayout
        {
            ConversionPattern = "%date [%thread] %message %newline"
        };
        patternLayout.ActivateOptions();
        var rollingFileAppender = new RollingFileAppender();
        rollingFileAppender.Name = "Dynamic";
        rollingFileAppender.Layout = patternLayout;
        rollingFileAppender.AppendToFile = true;
        rollingFileAppender.RollingStyle = RollingFileAppender.RollingMode.Once;
        rollingFileAppender.MaxSizeRollBackups = 10;
        rollingFileAppender.MaximumFileSize = "5MB";
        rollingFileAppender.StaticLogFileName = false;
        rollingFileAppender.File = path;
        var newAppender = rollingFileAppender;
        newAppender.ActivateOptions();
        logger.AddAppender(newAppender);
        return LogManager.GetLogger(name);
    }
}