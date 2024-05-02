// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.BasePin`1
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Threading;
using Ephi.Core.Helping.General;

namespace Ephi.Core.UTC;

public abstract class BasePin<T> : BasePin where T : BasePin<T>
{
    public readonly DelayedEventContainer<T> DelayedEvents;
    protected object RaisePinChangedLock = new();

    protected BasePin()
        : this(null, PIN._NULL)
    {
    }

    protected BasePin(RemoteUtc utc, PIN pin)
        : base(utc, pin)
    {
        DelayedEvents = new DelayedEventContainer<T>(this as T);
    }

    protected abstract string DirectionName { get; }

    public T HighActive => base.HighActive as T;

    public T LowActive => base.LowActive as T;

    public T Activate()
    {
        return base.Activate() as T;
    }

    public T Deactivate()
    {
        return base.Deactivate() as T;
    }

    protected override void RaisePinChanged()
    {
        lock (RaisePinChangedLock)
        {
            DelayedEvents.ResetTimers();
        }
    }

    public bool AwaitState(TRUE_PIN_STATE state, TimeSpan timeout)
    {
        var dateTime = DateTime.Now.Add(timeout);
        var flag = false;
        while (dateTime > DateTime.Now && !(flag = state == FineState))
            Thread.Sleep(100);
        return flag;
    }

    public void RegisterDebouncedPinStateHandler(TimeSpan debounceTimeout, Action<T> handler)
    {
        if (debounceTimeout.TotalMilliseconds < 500.0)
            throw new ArgumentException("Debouce timeout for a debounced pin state event cannot be less than 500ms");
        DelayedEvents.RegisterDebouncedPinStateHandler(debounceTimeout, handler);
    }

    public void UnregisterDebouncedPinStateHandler(Action<T> handler)
    {
        DelayedEvents.UnregisterDebouncedPinStateHandler(handler);
    }

    public override void Dispose()
    {
        DelayedEvents.Dispose();
    }

    public override string ToString()
    {
        return string.Format("{0} Pin{1} {2}({3})", DirectionName, Pin, State, Physical);
    }
}