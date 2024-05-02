// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.ConditionalMacro
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll


namespace Ephi.Core.UTC.ConditionalStatements;

public class ConditionalMacro : ConditionalContainer
{
    internal ConditionalMacro(byte index, RemoteUtc parent, RUN_MODE runMode, string name = "Anonymous")
        : base(index, parent, runMode, name)
    {
    }

    public override UTC_FUNCTION MakeFunction => UTC_FUNCTION.C_SET_MACRO;

    public ConditionalMacro AddStatement(Conditional conditional)
    {
        base.AddStatement(conditional);
        return this;
    }

    public virtual ConditionalMacro Repeat()
    {
        AddStatement(this);
        return this;
    }

    public ConditionalMacro AddGlobalTimeout(TIMEOUT_RANGE range, byte multiplier)
    {
        return AddGlobalTimeout<ConditionalMacro>(range, multiplier);
    }

    public ConditionalMacro AddGlobalTimeout(uint ms)
    {
        TIMEOUT_RANGE outFunction;
        byte outMultiplier;
        TimeoutHelpers.MsToTimeout(ms, out outFunction, out outMultiplier);
        return AddGlobalTimeout(outFunction, outMultiplier);
    }

    public ConditionalMacro MakePrecondition()
    {
        return MakePrecondition<ConditionalMacro>();
    }

    public ConditionalMacro DelayFinishedStateUpdMsg()
    {
        return DelayFinishedStateUpdMsg<ConditionalMacro>();
    }
}