// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.InPin
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using Ephi.Core.Helping.General;

namespace Ephi.Core.UTC;

public class InPin : BasePin<InPin>
{
    private InPin()
    {
    }

    private InPin(RemoteUtc utc, PIN pin)
        : base(utc, pin)
    {
    }

    public static InPin Dummy => new();

    protected override string DirectionName => "In";

    public static InPin Make(RemoteUtc utc, PIN pin, LEVEL activeState = LEVEL.HIGH)
    {
        var inPin = new InPin(utc, pin);
        inPin.ActiveState = activeState;
        return inPin;
    }

    public event Action<InPin> OnStateChangedAsync;

    public event Action<InPin> OnStateChanged;

    protected override void RaisePinChanged()
    {
        lock (RaisePinChangedLock)
        {
            base.RaisePinChanged();
            AsyncEvents.Raise(OnStateChangedAsync, ex => log.Error($"{this}_Async", ex.InnerException), this);
            try
            {
                var onStateChanged = OnStateChanged;
                if (onStateChanged == null)
                    return;
                onStateChanged(this);
            }
            catch (Exception ex)
            {
                log.Error($"{this}", ex);
            }
        }
    }
}