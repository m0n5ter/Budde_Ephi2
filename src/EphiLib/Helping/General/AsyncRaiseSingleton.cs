// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.General.AsyncRaiseSingleton
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;

namespace Ephi.Core.Helping.General;

public class AsyncRaiseSingleton
{
    private readonly Action asyncFire;
    protected volatile bool evalPending;
    protected volatile bool evalRunning;
    protected object EvaluateLock = new();
    private DateTime exceptionOccured;
    private Exception lastException;

    public AsyncRaiseSingleton(Action evnt)
    {
        var asyncRaiseSingleton = this;
        asyncFire = () =>
        {
            while (asyncRaiseSingleton.evalRunning)
            {
                lock (asyncRaiseSingleton.EvaluateLock)
                {
                    asyncRaiseSingleton.evalPending = false;
                }

                try
                {
                    var action = evnt;
                    if (action != null)
                        action();
                }
                catch (Exception ex)
                {
                    lock (asyncRaiseSingleton.EvaluateLock)
                    {
                        asyncRaiseSingleton.lastException = ex;
                        asyncRaiseSingleton.exceptionOccured = DateTime.Now;
                        asyncRaiseSingleton.evalRunning = false;
                        break;
                    }
                }

                lock (asyncRaiseSingleton.EvaluateLock)
                {
                    if (!asyncRaiseSingleton.evalPending)
                    {
                        asyncRaiseSingleton.evalRunning = false;
                        break;
                    }
                }
            }
        };
    }

    public bool Running => evalRunning;

    public void Raise()
    {
        lock (EvaluateLock)
        {
            if (lastException != null)
            {
                var applicationException = new ApplicationException(string.Format("Inner exception occured {0} ago", DateTime.Now.Subtract(exceptionOccured).ToString()), lastException);
                lastException = null;
                throw applicationException;
            }

            evalPending = true;
            if (evalRunning)
                return;
            evalRunning = true;
        }

        try
        {
            asyncFire.BeginInvoke(asyncFire.EndInvoke, null);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
}