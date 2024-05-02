// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.General.DelayedEventContainer`1
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Collections.Generic;
using System.Linq;

namespace Ephi.Core.Helping.General;

public class DelayedEventContainer<T>
{
    private readonly Dictionary<Action<T>, DelayedEvent> debouncedEvents;
    private readonly T owner;

    public DelayedEventContainer(T owner)
    {
        this.owner = owner;
        debouncedEvents = new Dictionary<Action<T>, DelayedEvent>();
    }

    public void RegisterDebouncedPinStateHandler(TimeSpan debounceTimeout, Action<T> handler)
    {
        if (handler == null)
            return;
        lock (debouncedEvents)
        {
            debouncedEvents.Add(handler, new DelayedEvent(debounceTimeout, () => handler(owner)));
        }
    }

    public void UnregisterDebouncedPinStateHandler(Action<T> handler)
    {
        if (handler == null)
            return;
        lock (debouncedEvents)
        {
            if (!debouncedEvents.ContainsKey(handler))
                return;
            debouncedEvents[handler].Dispose();
            debouncedEvents.Remove(handler);
        }
    }

    public void ResetTimers()
    {
        lock (debouncedEvents)
        {
            debouncedEvents.Values.ToList().ForEach(e => e.Start());
        }
    }

    public bool ResetDebouncedPinStateHandler(Action<T> handler)
    {
        if (handler == null)
            return false;
        lock (debouncedEvents)
        {
            if (!debouncedEvents.ContainsKey(handler))
                return false;
            debouncedEvents[handler].Start();
            return true;
        }
    }

    public void Dispose()
    {
        lock (debouncedEvents)
        {
            debouncedEvents.Values.ToList().ForEach(de => de.Dispose());
            debouncedEvents.Clear();
        }
    }
}