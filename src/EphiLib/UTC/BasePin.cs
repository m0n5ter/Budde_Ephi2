// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.BasePin
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using log4net;

namespace Ephi.Core.UTC;

public abstract class BasePin
{
    public static ILog log = LogManager.GetLogger("Pins");
    public readonly PIN Pin;
    protected readonly RemoteUtc utc;
    private LEVEL lastLevel = LEVEL.UNDETERMINED;
    private DateTime pinStateSet = DateTime.Now;

    protected BasePin()
        : this(null, PIN._NULL)
    {
    }

    protected BasePin(RemoteUtc utc, PIN pin)
    {
        Pin = pin;
        this.utc = utc;
        ActiveState = LEVEL.HIGH;
        lastLevel = Physical;
    }

    public RemoteUtc Utc => utc;

    public LEVEL ActiveState { get; protected set; }

    public LEVEL InactiveState => ActiveState != LEVEL.HIGH ? LEVEL.HIGH : LEVEL.LOW;

    public PIN_STATE State => !Active ? PIN_STATE.INACTIVE : PIN_STATE.ACTIVE;

    public TRUE_PIN_STATE FineState => !Undetermined ? (TRUE_PIN_STATE)State : TRUE_PIN_STATE.UNDETERMINED;

    public LEVEL Physical
    {
        get => !IsDummy ? utc.GetPin(this) : LEVEL.UNDETERMINED;
        set => utc?.SetPin(this, value);
    }

    public BasePin HighActive
    {
        get
        {
            ActiveState = LEVEL.HIGH;
            return this;
        }
    }

    public BasePin LowActive
    {
        get
        {
            ActiveState = LEVEL.LOW;
            return this;
        }
    }

    public bool Undetermined => Physical == LEVEL.UNDETERMINED;

    public bool Active => Physical == ActiveState;

    public bool Inactive => !Undetermined && !Active;

    public bool IsDummy => Pin == PIN._NULL;

    public TimeSpan PinStateAge => Physical != lastLevel ? TimeSpan.Zero : DateTime.Now.Subtract(pinStateSet);

    public void Toggle()
    {
        if (IsDummy)
            return;
        utc?.SetPin(this, Active ? InactiveState : ActiveState);
    }

    public BasePin Activate()
    {
        utc?.SetPin(this, ActiveState);
        return this;
    }

    public BasePin Deactivate()
    {
        utc?.SetPin(this, InactiveState);
        return this;
    }

    internal void CheckPinChanged()
    {
        if (Physical == lastLevel)
            return;
        lastLevel = Physical;
        pinStateSet = DateTime.Now;
        RaisePinChanged();
    }

    public bool Equals(BasePin other)
    {
        return other.Pin == Pin;
    }

    protected abstract void RaisePinChanged();

    public abstract void Dispose();
}