// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.ConditionalMeasurement
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Linq;
using Ephi.Core.Helping;

namespace Ephi.Core.UTC.ConditionalStatements;

public class ConditionalMeasurement : Conditional, IBaseCondition
{
    private IBaseCondition pauseCondition;
    private IBaseCondition startCondition;
    private IBaseCondition stopCondition;

    internal ConditionalMeasurement(byte index, RemoteUtc parent, RUN_MODE runMode, string name = "Anonymous")
        : base(index, parent, runMode, name)
    {
    }

    public override UTC_FUNCTION MakeFunction => UTC_FUNCTION.C_SET_MEASUREMENT;

    public StatePin[] AllOutPins => new StatePin[0];

    public override byte[] Bytes
    {
        get
        {
            return new byte[2]
            {
                Convert.ToByte(ConditionalInterpretationByte),
                Index
            }.Concat(GlobalTimeoutBytes).Concat(startCondition?.Bytes ?? new byte[0]).Concat(stopCondition?.Bytes ?? new byte[0]).Concat(pauseCondition?.Bytes ?? new byte[0]).ToArray();
        }
    }

    public StatePin[] AllInPins => (startCondition?.AllInPins ?? new StatePin[0]).Concat(stopCondition?.AllInPins ?? new StatePin[0]).Concat(pauseCondition?.AllInPins ?? new StatePin[0]).Distinct()
        .ToArray();

    private void SetRootCondition(MEASURING_CONDITION conditionType, IBaseCondition condition)
    {
        if (GetRootCondition(conditionType) != null)
            throw new ArgumentException($"A condition for {Formatting.TitleCase(conditionType)} in '{Name}' was already given");
        switch (conditionType)
        {
            case MEASURING_CONDITION.MC_START:
                startCondition = condition;
                break;
            case MEASURING_CONDITION.MC_STOP:
                stopCondition = condition;
                break;
            case MEASURING_CONDITION.MC_PAUSE:
                pauseCondition = condition;
                break;
            default:
                return;
        }

        Invalidate();
    }

    private IBaseCondition GetRootCondition(MEASURING_CONDITION conditionType)
    {
        switch (conditionType)
        {
            case MEASURING_CONDITION.MC_START:
                return startCondition;
            case MEASURING_CONDITION.MC_STOP:
                return stopCondition;
            case MEASURING_CONDITION.MC_PAUSE:
                return pauseCondition;
            default:
                return null;
        }
    }

    private ConditionalMeasurement AddCondition(
        MEASURING_CONDITION conditionType,
        StatePin pin,
        LEVEL triggerState)
    {
        SetRootCondition(conditionType, PinState<InputCondition>.Make(pin, triggerState));
        return this;
    }

    public ConditionalMeasurement AddCondition(
        MEASURING_CONDITION conditionType,
        PIN pin,
        LEVEL triggerState,
        PIN_DIRECTION dir = PIN_DIRECTION.IN)
    {
        return AddCondition(conditionType, StatePin.Make(pin, dir), triggerState);
    }

    public ConditionalMeasurement AddCondition(
        MEASURING_CONDITION conditionType,
        BasePin pin,
        PIN_STATE triggerState = PIN_STATE.ACTIVE)
    {
        return (pin != null ? pin.IsDummy ? 1 : 0 : 1) != 0 ? this : AddCondition(conditionType, StatePin.Make(pin), triggerState == PIN_STATE.INACTIVE ? pin.InactiveState : pin.ActiveState);
    }

    public ConditionalMeasurement AddTimeoutCondition(
        MEASURING_CONDITION conditionType,
        TIMEOUT_RANGE range,
        byte multiplier,
        BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        SetRootCondition(conditionType, TimeoutCondition.Make(range, multiplier, interpretation));
        return this;
    }

    public ConditionalMeasurement AddTimeoutCondition(
        MEASURING_CONDITION conditionType,
        uint ms,
        BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        TimeoutHelpers.MsToTimeout(ms, out var outFunction, out var outMultiplier);
        return AddTimeoutCondition(conditionType, outFunction, outMultiplier, interpretation);
    }

    public LogicBlock<ConditionalMeasurement> AddLogicBlock(
        MEASURING_CONDITION conditionType,
        LOGIC_FUNCTION function,
        BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        var condition = new LogicBlock<ConditionalMeasurement>(this, function, interpretation);
        SetRootCondition(conditionType, condition);
        return condition;
    }

    public GuardBlock<ConditionalMeasurement> AddGuardBlock(
        MEASURING_CONDITION conditionType,
        TIMEOUT_RANGE range,
        byte guardMultiplier,
        BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        var condition = new GuardBlock<ConditionalMeasurement>(this, range, guardMultiplier, interpretation);
        SetRootCondition(conditionType, condition);
        return condition;
    }

    public GuardBlock<ConditionalMeasurement> AddGuardBlock(
        MEASURING_CONDITION conditionType,
        uint ms,
        BOOL_INTERPRET interpretation = BOOL_INTERPRET.AS_IS)
    {
        TimeoutHelpers.MsToTimeout(ms, out var outFunction, out var outMultiplier);
        return AddGuardBlock(conditionType, outFunction, outMultiplier, interpretation);
    }

    public ConditionalMeasurement AddGlobalTimeout(TIMEOUT_RANGE range, byte multiplier)
    {
        return AddGlobalTimeout<ConditionalMeasurement>(range, multiplier);
    }

    public ConditionalMeasurement AddGlobalTimeout(uint ms)
    {
        TimeoutHelpers.MsToTimeout(ms, out var outFunction, out var outMultiplier);
        return AddGlobalTimeout(outFunction, outMultiplier);
    }

    public ConditionalMeasurement MakePrecondition()
    {
        return MakePrecondition<ConditionalMeasurement>();
    }

    public ConditionalMeasurement DelayFinishedStateUpdMsg()
    {
        return DelayFinishedStateUpdMsg<ConditionalMeasurement>();
    }

    internal void RaiseOnMeasurementReceived(MeasurementResult result)
    {
        var measurementCompleted = OnMeasurementCompleted;
        measurementCompleted?.Invoke(this, result);
    }

    public event Action<ConditionalMeasurement, MeasurementResult> OnMeasurementCompleted;
}