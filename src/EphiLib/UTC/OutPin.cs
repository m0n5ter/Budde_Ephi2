// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.OutPin
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using Ephi.Core.Helping.General;

namespace Ephi.Core.UTC;

public class OutPin : BasePin<OutPin>
{
    private OutPin()
    {
    }

    private OutPin(RemoteUtc utc, PIN pin)
        : base(utc, pin)
    {
    }

    public static OutPin Dummy => new();

    protected override string DirectionName => "Out";

    public static OutPin Make(RemoteUtc utc, PIN pin, LEVEL activeState = LEVEL.HIGH)
    {
        var outPin = new OutPin(utc, pin);
        outPin.ActiveState = activeState;
        return outPin;
    }

    public event Action<OutPin> OnStateChangedAsync;

    public event Action<OutPin> OnStateChanged;

    protected override void RaisePinChanged()
    {
        lock (RaisePinChangedLock)
        {
            base.RaisePinChanged();
            AsyncEvents.Raise(OnStateChangedAsync, ex => log.Error(string.Format("{0}_Async", this), ex.InnerException), this);
            try
            {
                var onStateChanged = OnStateChanged;
                if (onStateChanged == null)
                    return;
                onStateChanged(this);
            }
            catch (Exception ex)
            {
                log.Error(string.Format("{0}", this), ex);
            }
        }
    }
}