// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.GuardBlock`1
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Linq;

namespace Ephi.Core.UTC.ConditionalStatements;

public class GuardBlock<Owner> : CompoundCondition where Owner : IBaseCondition
{
    public GuardBlock(
        Owner parent,
        TIMEOUT_RANGE range,
        byte multiplier,
        BOOL_INTERPRET interpretation)
        : base(parent, COMPOUND_FUNCTION.GUARD, interpretation)
    {
        AddTimeoutCondition<GuardBlock<Owner>>(range, multiplier, BOOL_INTERPRET.AS_IS);
    }

    private GuardBlock<Owner> AddCondition(StatePin pin)
    {
        return AddCondition<GuardBlock<Owner>>(pin, LEVEL.LOW);
    }

    public GuardBlock<Owner> AddGuardPin(PIN pin, PIN_DIRECTION dir = PIN_DIRECTION.IN)
    {
        return AddCondition(StatePin.Make(pin, dir));
    }

    public GuardBlock<Owner> AddGuardPin(BasePin pin)
    {
        return (pin != null ? pin.IsDummy ? 1 : 0 : 1) != 0 ? this : AddCondition(StatePin.Make(pin));
    }

    public Owner CloseBlock()
    {
        return (Owner)Parent;
    }

    protected override bool CheckValid()
    {
        if (compoundConditions.OfType<TimeoutCondition>().Count() != 1)
            throw new ArgumentException("GuardCondition must have exactely one timeout condition");
        if (!compoundConditions.OfType<InputCondition>().Any())
            throw new ArgumentException("GuardCondition must have at least one pin condition");
        return base.CheckValid();
    }
}