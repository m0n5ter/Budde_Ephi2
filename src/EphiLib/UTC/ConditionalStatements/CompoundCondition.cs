// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.CompoundCondition
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Collections.Generic;
using System.Linq;

namespace Ephi.Core.UTC.ConditionalStatements;

public class CompoundCondition : IBaseCondition
{
    public readonly COMPOUND_FUNCTION Function;
    public readonly BOOL_INTERPRET Interpretation;
    protected readonly IBaseCondition Parent;
    protected List<IBaseCondition> compoundConditions = new();

    internal CompoundCondition(
        IBaseCondition parent,
        COMPOUND_FUNCTION function,
        BOOL_INTERPRET interpretation)
    {
        Function = function;
        Interpretation = interpretation;
        Parent = parent;
    }

    public void Invalidate()
    {
        Parent.Invalidate();
    }

    public byte[] Bytes
    {
        get
        {
            if (!CheckValid())
                throw new ArgumentException("CompoundCondition invalid");
            byte num1 = 64;
            byte num2 = 96;
            return new byte[2]
            {
                (byte)(num1 | (uint)Convert.ToByte((int)Interpretation << 4)),
                (byte)Function
            }.Concat(compoundConditions.SelectMany(cc => cc.Bytes).ToArray()).Concat(new byte[1]
            {
                num2
            }).ToArray();
        }
    }

    public StatePin[] AllInPins
    {
        get { return compoundConditions.SelectMany(cc => cc.AllInPins).ToArray(); }
    }

    protected void AddCondition(IBaseCondition condition)
    {
        lock (compoundConditions)
        {
            compoundConditions.Add(condition);
        }

        Invalidate();
    }

    protected T AddCondition<T>(StatePin pin, LEVEL triggerState) where T : CompoundCondition
    {
        if (compoundConditions.OfType<InputCondition>().Any(ic => ic.Equals(pin)))
            throw new ArgumentException("An input condition for this pin was already added");
        AddCondition(PinState<InputCondition>.Make(pin, triggerState));
        return (T)this;
    }

    protected T AddTimeoutCondition<T>(
        TIMEOUT_RANGE range,
        byte multiplier,
        BOOL_INTERPRET interpretation)
        where T : CompoundCondition
    {
        AddCondition(TimeoutCondition.Make(range, multiplier, interpretation));
        Invalidate();
        return (T)this;
    }

    protected virtual bool CheckValid()
    {
        return true;
    }
}