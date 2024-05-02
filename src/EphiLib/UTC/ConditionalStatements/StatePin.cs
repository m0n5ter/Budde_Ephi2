// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.StatePin
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using Ephi.Core.Helping;

namespace Ephi.Core.UTC.ConditionalStatements;

public class StatePin
{
    protected StatePin()
    {
    }

    public PIN Pin { get; protected set; }

    public PIN_DIRECTION Dir { get; protected set; }

    public static StatePin Make(BasePin pin)
    {
        return new StatePin
        {
            Pin = pin.Pin,
            Dir = pin is OutPin ? PIN_DIRECTION.OUT : PIN_DIRECTION.IN
        };
    }

    public static StatePin Make(PIN pin, PIN_DIRECTION dir)
    {
        return new StatePin { Pin = pin, Dir = dir };
    }

    public override bool Equals(object obj)
    {
        return obj is StatePin statePin && statePin.Pin.Equals(Pin) && statePin.Dir.Equals(Dir);
    }

    public override int GetHashCode()
    {
        return ((int)(Dir + 1) * (int)(Pin + 1)).GetHashCode();
    }

    public override string ToString()
    {
        return string.Format("{0}-pin{1}", Formatting.TitleCase(Dir), Pin);
    }
}