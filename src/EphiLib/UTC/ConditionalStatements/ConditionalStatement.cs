// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.ConditionalStatement
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Collections.Generic;
using System.Linq;

namespace Ephi.Core.UTC.ConditionalStatements;

public class ConditionalStatement : Conditional, IBaseCondition
{
    private readonly List<OutputState> outputStates = new();
    private IBaseCondition rootCondition;

    internal ConditionalStatement(
        byte index,
        RemoteUtc parent,
        OUTPUT_ENFORCEMENT enforcement,
        RUN_MODE runMode,
        string name = "Anonymous")
        : base(index, parent, runMode, name)
    {
        Enforcement = enforcement;
    }

    public OUTPUT_ENFORCEMENT Enforcement { get; }

    public override UTC_FUNCTION MakeFunction => UTC_FUNCTION.C_SET_STATEMENT;

    public StatePin[] AllOutPins => outputStates.ToArray<StatePin>();

    public override byte[] Bytes
    {
        get
        {
            if (outputStates.Count() == 0 && rootCondition == null)
                throw new ArgumentException("At least one statement must be provided");
            return new byte[2]
            {
                Convert.ToByte(ConditionalInterpretationByte | ((int)Enforcement << 4)),
                Index
            }.Concat(GlobalTimeoutBytes).Concat(outputStates.SelectMany(os => os.Bytes)).Concat(rootCondition?.Bytes ?? new byte[0]).ToArray();
        }
    }

    public StatePin[] AllInPins
    {
        get
        {
            var rootCondition = this.rootCondition;
            return rootCondition?.AllInPins.Distinct().ToArray() ?? new StatePin[0];
        }
    }

    private void SetRootCondition(IBaseCondition condition)
    {
        rootCondition = rootCondition == null
            ? condition
            : throw new ArgumentException("A conditional statement itself can contain only one condiftion. Add an (AND / OR) block instead if more conditions a re required");
        Invalidate();
    }

    private ConditionalStatement AddCondition(StatePin pin, LEVEL triggerState)
    {
        SetRootCondition(PinState<InputCondition>.Make(pin, triggerState));
        return this;
    }

    public ConditionalStatement AddCondition(PIN pin, LEVEL triggerState, PIN_DIRECTION dir = PIN_DIRECTION.IN)
    {
        return AddCondition(StatePin.Make(pin, dir), triggerState);
    }

    public ConditionalStatement AddCondition(BasePin pin, PIN_STATE triggerState = PIN_STATE.ACTIVE)
    {
        return (pin != null ? pin.IsDummy ? 1 : 0 : 1) != 0 ? this : AddCondition(StatePin.Make(pin), triggerState == PIN_STATE.INACTIVE ? pin.InactiveState : pin.ActiveState);
    }

    public ConditionalStatement AddTimeoutCondition(
        TIMEOUT_RANGE range,
        byte multiplier,
        BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        SetRootCondition(TimeoutCondition.Make(range, multiplier, interpretation));
        return this;
    }

    public ConditionalStatement AddTimeoutCondition(uint ms, BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        TimeoutHelpers.MsToTimeout(ms, out var outFunction, out var outMultiplier);
        return AddTimeoutCondition(outFunction, outMultiplier, interpretation);
    }

    public LogicBlock<ConditionalStatement> AddLogicBlock(
        LOGIC_FUNCTION function,
        BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        var condition = new LogicBlock<ConditionalStatement>(this, function, interpretation);
        SetRootCondition(condition);
        return condition;
    }

    public GuardBlock<ConditionalStatement> AddGuardBlock(
        TIMEOUT_RANGE range,
        byte guardMultiplier,
        BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        var condition = new GuardBlock<ConditionalStatement>(this, range, guardMultiplier, interpretation);
        SetRootCondition(condition);
        return condition;
    }

    public GuardBlock<ConditionalStatement> AddGuardBlock(uint ms, BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        TimeoutHelpers.MsToTimeout(ms, out var outFunction, out var outMultiplier);
        return AddGuardBlock(outFunction, outMultiplier, interpretation);
    }

    public ConditionalStatement AddOutputState(BasePin pin, PIN_STATE state = PIN_STATE.ACTIVE, bool persistent = false)
    {
        return (pin != null ? pin.IsDummy ? 1 : 0 : 1) != 0 ? this : AddOutputState(StatePin.Make(pin), state == PIN_STATE.ACTIVE ? pin.ActiveState : pin.InactiveState, persistent);
    }

    private ConditionalStatement AddOutputState(StatePin pin, LEVEL state, bool persistent = false)
    {
        if (outputStates.Any(os => os.Equals(pin)))
            throw new ArgumentException($"An output state for {pin} was already added in conditional {Name}");
        outputStates.Add(PinState<OutputState>.Make(pin, state).MakePermanent(persistent));
        Invalidate();
        return this;
    }

    public ConditionalStatement AddOutputState(
        PIN pin,
        LEVEL state,
        bool persistent = false,
        PIN_DIRECTION dir = PIN_DIRECTION.OUT)
    {
        return AddOutputState(StatePin.Make(pin, dir), state, persistent);
    }

    public ConditionalStatement AddOutputState(BasePin pin, bool persistent)
    {
        return AddOutputState(pin, PIN_STATE.ACTIVE, persistent);
    }

    public ConditionalStatement AddGlobalTimeout(TIMEOUT_RANGE range, byte multiplier)
    {
        return AddGlobalTimeout<ConditionalStatement>(range, multiplier);
    }

    public ConditionalStatement AddGlobalTimeout(uint ms)
    {
        TimeoutHelpers.MsToTimeout(ms, out var outFunction, out var outMultiplier);
        return AddGlobalTimeout(outFunction, outMultiplier);
    }

    public ConditionalStatement MakePrecondition()
    {
        return MakePrecondition<ConditionalStatement>();
    }

    public ConditionalStatement DelayFinishedStateUpdMsg()
    {
        return DelayFinishedStateUpdMsg<ConditionalStatement>();
    }
}