// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.Helpers
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ephi.Core.Helping;

public static class Helpers
{
    public static string ExecutingPath
    {
        get
        {
            var executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            var num1 = executingPath.LastIndexOf(':');
            if (num1 > 1)
            {
                int num2;
                executingPath = executingPath.Substring(num2 = num1 - 1);
            }

            return executingPath;
        }
    }

    public static bool Contains<T>(T item, params T[] references) where T : struct, IConvertible
    {
        return references.ToList().Contains(item);
    }

    public static Tr MinMax<Tr, Tc>(Tr val, Tc min, Tc max)
        where Tr : struct, IConvertible
        where Tc : struct, IConvertible
    {
        var y = (Tr)Convert.ChangeType(min, typeof(Tr));
        var x = (Tr)Convert.ChangeType(max, typeof(Tr));
        if (Comparer<Tr>.Default.Compare(val, y) < 0)
            return y;
        return Comparer<Tr>.Default.Compare(x, val) < 0 ? x : val;
    }

    public static bool InRange<T>(T val, T min, T max) where T : struct, IConvertible
    {
        var b1 = EMin(min, max);
        var b2 = EMax(min, max);
        return EMin(val, b1).Equals(b1) && EMax(val, b2).Equals(b2);
    }

    public static T EMax<T>(T a, T b) where T : struct, IConvertible
    {
        return Comparer<T>.Default.Compare(a, b) <= 0 ? b : a;
    }

    public static T EMin<T>(T a, T b) where T : struct, IConvertible
    {
        return Comparer<T>.Default.Compare(a, b) >= 0 ? b : a;
    }

    public static void Swap<T>(ref T a, ref T b) where T : struct, IConvertible
    {
        var obj = a;
        a = b;
        b = obj;
    }

    public static TimeSpan Since(DateTime moment)
    {
        return moment.Equals(DateTime.MinValue) || moment.Equals(DateTime.MaxValue) ? TimeSpan.Zero : DateTime.Now.Subtract(moment);
    }

    public static bool Elapsed(DateTime moment, TimeSpan period)
    {
        if (period.Equals(TimeSpan.FromMilliseconds(0.0)) || period.Equals(TimeSpan.MinValue) || period.Equals(TimeSpan.MaxValue))
            return false;
        var timeSpan = Since(moment);
        return !timeSpan.Equals(TimeSpan.FromMilliseconds(0.0)) && timeSpan > period;
    }

    public static bool Due(ref DateTime last, TimeSpan interval)
    {
        if (interval.Equals(TimeSpan.Zero))
            return false;
        var now = DateTime.Now;
        if (Since(last).Equals(TimeSpan.Zero))
        {
            last = now;
            return false;
        }

        if (now < last.Add(interval))
            return false;
        last = now;
        return true;
    }

    public static bool IsNumericType(Type type)
    {
        var typeSet = new HashSet<Type>
        {
            typeof(byte),
            typeof(sbyte),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(decimal),
            typeof(double),
            typeof(float)
        };
        return typeSet.Contains(type) || typeSet.Contains(Nullable.GetUnderlyingType(type));
    }
}