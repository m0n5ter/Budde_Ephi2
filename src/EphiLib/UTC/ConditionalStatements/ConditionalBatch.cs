// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.ConditionalBatch
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;

namespace Ephi.Core.UTC.ConditionalStatements;

public class ConditionalBatch : ConditionalContainer
{
    internal ConditionalBatch(byte index, RemoteUtc parent, RUN_MODE runMode, string name = "Anonymous")
        : base(index, parent, runMode, name)
    {
    }

    public override UTC_FUNCTION MakeFunction => UTC_FUNCTION.C_SET_BATCH;

    public ConditionalBatch AddStatement(Conditional conditional)
    {
        if (conditional == null)
            return this;
        if (conditional == this)
            throw new ArgumentException("Batches cannot contain themselves");
        if (!Contains(conditional))
            base.AddStatement(conditional);
        return this;
    }

    public ConditionalBatch AddGlobalTimeout(TIMEOUT_RANGE range, byte multiplier)
    {
        return AddGlobalTimeout<ConditionalBatch>(range, multiplier);
    }

    public ConditionalBatch DelayFinishedStateUpdMsg()
    {
        return DelayFinishedStateUpdMsg<ConditionalBatch>();
    }
}