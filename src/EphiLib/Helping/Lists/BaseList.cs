// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.Lists.BaseList`1
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Collections.Generic;
using System.Linq;

namespace Ephi.Core.Helping.Lists;

public abstract class BaseList<T>
{
    protected List<T> list;

    public BaseList()
    {
        list = new List<T>();
    }

    public abstract T Peek { get; }

    public abstract T Pop { get; }

    public virtual bool Empty => Count == 0;

    public int Count
    {
        get
        {
            lock (list)
            {
                return list.Count();
            }
        }
    }

    public void PopBlind()
    {
        var pop = Pop;
    }

    public virtual T Add(T addition, bool unique = false)
    {
        lock (list)
        {
            if (unique && list.Contains(addition))
                return list[list.IndexOf(addition)];
            list.Add(addition);
            return addition;
        }
    }

    public T Find(Func<T, bool> predicate)
    {
        lock (list)
        {
            return list.FirstOrDefault(predicate);
        }
    }

    public List<T> FindAll(Predicate<T> match)
    {
        lock (list)
        {
            return list.FindAll(match);
        }
    }

    public bool Remove(T subject)
    {
        lock (list)
        {
            return list.Remove(subject);
        }
    }

    public bool Remove(Func<T, bool> predicate)
    {
        lock (list)
        {
            return list.Remove(Find(predicate));
        }
    }

    public virtual void Clear()
    {
        lock (list)
        {
            list.Clear();
        }
    }
}