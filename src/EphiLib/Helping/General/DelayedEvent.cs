// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.General.DelayedEvent
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Timers;

namespace Ephi.Core.Helping.General;

public class DelayedEvent
{
    private readonly Timer debounceTimer;
    public readonly uint Delay_ms;
    private readonly bool runOnce;
    private Action handler;

    public DelayedEvent(TimeSpan debounceTimeout, Action handler)
        : this(Convert.ToUInt32(debounceTimeout.TotalMilliseconds), handler)
    {
    }

    public DelayedEvent(uint ms, Action handler)
        : this(ms, handler, false)
    {
    }

    private DelayedEvent(uint ms, Action handler, bool runOnce)
    {
        this.runOnce = runOnce;
        Delay_ms = ms;
        debounceTimer = new Timer(ms)
        {
            AutoReset = false,
            Enabled = false
        };
        debounceTimer.Elapsed += RaiseEventTimer_Elapsed;
        this.handler = handler;
    }

    public bool Running => debounceTimer.Enabled;

    ~DelayedEvent()
    {
        Dispose();
    }

    private void RaiseEventTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        debounceTimer.Enabled = false;
        var handler = this.handler;
        handler?.Invoke();
        if (!runOnce)
            return;
        Dispose();
    }

    public void Stop()
    {
        debounceTimer.Stop();
    }

    public void Start()
    {
        Stop();
        debounceTimer.Start();
    }

    public void Dispose()
    {
        debounceTimer.Stop();
        handler = null;
    }

    public static void Once(TimeSpan debounceTimeout, Action handler)
    {
        Once(Convert.ToUInt32(debounceTimeout.TotalMilliseconds), handler);
    }

    public static void Once(uint ms, Action handler)
    {
        new DelayedEvent(ms, handler, true).Start();
    }
}