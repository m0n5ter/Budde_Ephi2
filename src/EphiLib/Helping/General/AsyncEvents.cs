// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.General.AsyncEvents
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

namespace Ephi.Core.Helping.General;

public class AsyncEvents
{
    public static void Raise<EVNT>(EVNT evnt, params object[] args) where EVNT : Delegate
    {
        Raise(evnt, null, args);
    }

    public static void Raise<EVNT>(EVNT evnt, Action<Exception> errorHandler, params object[] args) where EVNT : Delegate
    {
        foreach (var @delegate in evnt?.GetInvocationList() ?? new Delegate[0])
        {
            var dlg = @delegate;
            var name = dlg.Method.Name;
            ((Action)(() => dlg.DynamicInvoke(args))).BeginInvoke(iar =>
            {
                try
                {
                    ((Action)((AsyncResult)iar).AsyncDelegate).EndInvoke(iar);
                }
                catch (Exception ex)
                {
                    var exception = ex;
                    while (exception?.GetType() == typeof(TargetInvocationException) && exception.InnerException != null)
                        exception = exception.InnerException;
                    var instance = (Exception)Activator.CreateInstance(exception.GetType(),
                        string.Format("Exception in asynchronously called eventhandler '{0}'\r\n - {1}", iar.AsyncState, exception.Message), ex);
                    if (errorHandler == null)
                        throw instance;
                    errorHandler(instance);
                }
            }, dlg.Method.Name);
        }
    }
}