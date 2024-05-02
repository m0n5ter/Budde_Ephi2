// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.LogicBlock`1
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Linq;

namespace Ephi.Core.UTC.ConditionalStatements;

public class LogicBlock<Owner> : CompoundCondition where Owner : IBaseCondition
{
    internal LogicBlock(Owner parent, LOGIC_FUNCTION function, BOOL_INTERPRET interpretation)
        : base(parent, (COMPOUND_FUNCTION)function, interpretation)
    {
    }

    private LogicBlock<Owner> AddCondition(StatePin pin, LEVEL triggerState)
    {
        return AddCondition<LogicBlock<Owner>>(pin, triggerState);
    }

    public LogicBlock<Owner> AddCondition(PIN pin, LEVEL triggerState, PIN_DIRECTION dir = PIN_DIRECTION.IN)
    {
        return AddCondition(StatePin.Make(pin, dir), triggerState);
    }

    public LogicBlock<Owner> AddCondition(BasePin pin, PIN_STATE interpretation = PIN_STATE.ACTIVE)
    {
        return (pin != null ? pin.IsDummy ? 1 : 0 : 1) != 0 ? this : AddCondition(StatePin.Make(pin), interpretation == PIN_STATE.ACTIVE ? pin.ActiveState : pin.InactiveState);
    }

    public LogicBlock<Owner> AddTimeoutCondition(
        TIMEOUT_RANGE range,
        byte multiplier,
        BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        return AddTimeoutCondition<LogicBlock<Owner>>(range, multiplier, interpretation);
    }

    public LogicBlock<Owner> AddTimeoutCondition(uint ms, BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        TIMEOUT_RANGE outFunction;
        byte outMultiplier;
        TimeoutHelpers.MsToTimeout(ms, out outFunction, out outMultiplier);
        return AddTimeoutCondition(outFunction, outMultiplier, interpretation);
    }

    public LogicBlock<LogicBlock<Owner>> AddLogicBlock(
        LOGIC_FUNCTION function,
        BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        var condition = new LogicBlock<LogicBlock<Owner>>(this, function, interpretation);
        AddCondition(condition);
        return condition;
    }

    public GuardBlock<LogicBlock<Owner>> AddGuardBlock(
        TIMEOUT_RANGE guardFunction,
        byte guardMultiplier,
        BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        if (guardMultiplier == 0)
            throw new ArgumentException("Guards cannot have a time constant of 0");
        var condition = new GuardBlock<LogicBlock<Owner>>(this, guardFunction, guardMultiplier, interpretation);
        AddCondition(condition);
        return condition;
    }

    public GuardBlock<LogicBlock<Owner>> AddGuardBlock(uint ms, BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        TIMEOUT_RANGE outFunction;
        byte outMultiplier;
        TimeoutHelpers.MsToTimeout(ms, out outFunction, out outMultiplier);
        return AddGuardBlock(outFunction, outMultiplier, interpretation);
    }

    public Owner CloseBlock()
    {
        return (Owner)Parent;
    }

    protected override bool CheckValid()
    {
        if (compoundConditions.Count() == 0)
            throw new ArgumentException("This logic function has no conditions");
        return base.CheckValid();
    }
}