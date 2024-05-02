// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.PinState`1
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;

namespace Ephi.Core.UTC.ConditionalStatements;

public abstract class PinState<T> : StatePin where T : PinState<T>
{
    public LEVEL State { get; protected set; }

    public static T Make(BasePin pin, PIN_STATE state = PIN_STATE.ACTIVE)
    {
        return Make(StatePin.Make(pin), state == PIN_STATE.ACTIVE ? pin.ActiveState : pin.InactiveState);
    }

    public static T Make(StatePin pin, LEVEL state)
    {
        var instance = (T)Activator.CreateInstance(typeof(T), true);
        instance.Pin = pin.Pin;
        instance.Dir = pin.Dir;
        instance.State = state;
        return instance;
    }
}