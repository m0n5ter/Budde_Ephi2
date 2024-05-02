// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.Lists.BitMask
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;

namespace Ephi.Core.Helping.Lists;

public class BitMask
{
    private readonly object MaskLock = new();
    private int holdUpdates;
    private bool stateDirty;

    public BitMask()
    {
    }

    public BitMask(ulong mask)
    {
        this.Mask = mask;
    }

    public ulong Mask { get; private set; }

    public bool HoldUpdates
    {
        get => holdUpdates > 0;
        set
        {
            holdUpdates += value ? 1 : -1;
            holdUpdates = Math.Max(0, holdUpdates);
            if (HoldUpdates || !stateDirty)
                return;
            RaiseOnMaskChanged();
        }
    }

    public bool Get(int bit)
    {
        return GetBool(bit);
    }

    public bool Set(int bit)
    {
        return SetBool(bit, true);
    }

    public bool Reset(int bit)
    {
        return SetBool(bit, false);
    }

    public void Toggle(int bit)
    {
        ToggleBool(bit);
    }

    public void Clear()
    {
        if (Mask == 0UL)
            return;
        lock (MaskLock)
        {
            Mask = 0UL;
        }

        RaiseOnMaskChanged();
    }

    protected bool GetBool(int bit)
    {
        return (Mask & (ulong)(1L << bit)) > 0UL;
    }

    protected void ToggleBool(int bit)
    {
        lock (MaskLock)
        {
            Mask ^= (ulong)(1L << bit);
        }

        RaiseOnMaskChanged();
    }

    public bool SetBool(int bit, bool set)
    {
        lock (MaskLock)
        {
            if (GetBool(bit) == set)
                return false;
            var num = 1UL << bit;
            if (set)
                Mask |= num;
            else
                Mask &= ~num;
        }

        RaiseOnMaskChanged();
        return true;
    }

    public bool Assign(BitMask source)
    {
        if ((long)source.Mask == (long)Mask)
            return false;
        lock (MaskLock)
        {
            Mask = source.Mask;
        }

        RaiseOnMaskChanged();
        return true;
    }

    public bool Assign(ulong sourceMask)
    {
        if ((long)sourceMask == (long)Mask)
            return false;
        lock (MaskLock)
        {
            Mask = sourceMask;
        }

        RaiseOnMaskChanged();
        return true;
    }

    public BitMask Diff(BitMask source)
    {
        return new BitMask
        {
            Mask = Mask ^ source.Mask
        };
    }

    public event Action<BitMask> OnMaskChanged;

    private void RaiseOnMaskChanged()
    {
        if (HoldUpdates)
        {
            stateDirty = true;
        }
        else
        {
            stateDirty = false;
            var onMaskChanged = OnMaskChanged;
            if (onMaskChanged == null)
                return;
            onMaskChanged(this);
        }
    }
}