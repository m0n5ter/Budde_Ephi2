// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.TimeoutCondition
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll


namespace Ephi.Core.UTC.ConditionalStatements;

public class TimeoutCondition : Timeout<TimeoutCondition>, IBaseCondition
{
    private TimeoutCondition()
    {
    }

    public BOOL_INTERPRET Interpretation { get; private set; }

    public void Invalidate()
    {
    }

    public byte[] Bytes => Bytes(INPUT_CONDITION_TYPE.TIMEOUT_CONDITION, Interpretation);

    public StatePin[] AllInPins => new StatePin[0];

    public static TimeoutCondition Make(
        TIMEOUT_RANGE range,
        ushort multiplier,
        BOOL_INTERPRET interpretation)
    {
        var timeoutCondition = Make(range, multiplier);
        timeoutCondition.Interpretation = interpretation;
        return timeoutCondition;
    }
}