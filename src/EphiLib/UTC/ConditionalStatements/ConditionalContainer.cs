// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.ConditionalContainer
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Collections.Generic;
using System.Linq;

namespace Ephi.Core.UTC.ConditionalStatements;

public abstract class ConditionalContainer : Conditional
{
    protected List<Conditional> containedConditionals;

    protected ConditionalContainer(byte index, RemoteUtc parent, RUN_MODE runMode, string name = "Anonymous")
        : base(index, parent, runMode, name)
    {
        containedConditionals = new List<Conditional>();
    }

    protected Conditional[] ContainedConditionalsWithoutMe
    {
        get { return containedConditionals.Where(c => c != this).ToArray(); }
    }

    public byte[] Indices
    {
        get { return containedConditionals.Select(c => c.Index).ToArray(); }
    }

    public override byte[] Bytes
    {
        get
        {
            if (containedConditionals.Count() == 0)
                throw new ArgumentException("At least one statement must be provided");
            return new byte[2]
            {
                ConditionalInterpretationByte,
                Index
            }.Concat(new byte[1]
            {
                (byte)containedConditionals.Count()
            }).Concat(Indices).Concat(GlobalTimeoutBytes).ToArray();
        }
    }

    public int SortScore(Conditional containedChild)
    {
        int num1;
        lock (containedConditionals)
        {
            num1 = containedConditionals.IndexOf(containedChild);
        }

        if (num1 < 0)
            return int.MinValue;
        int num2;
        if (containedChild.IsRunning)
            return ushort.MaxValue - (num2 = num1 + 1);
        return containedChild.IsRunningOrAboutToBe ? byte.MaxValue - (num2 = num1 + 1) : num2 = num1 + 1;
    }

    public bool Contains(Conditional src)
    {
        lock (containedConditionals)
        {
            return containedConditionals.Contains(src);
        }
    }

    protected void AddStatement(Conditional conditional)
    {
        if (conditional == null)
            return;
        lock (containedConditionals)
        {
            containedConditionals.Add(conditional);
        }

        conditional.AddOwner(this);
        Invalidate();
    }
}