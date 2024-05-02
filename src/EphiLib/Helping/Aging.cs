// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.Aging
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;

namespace Ephi.Core.Helping;

public class Aging
{
    private DateTime time = DateTime.MinValue;

    private Aging()
    {
    }

    public TimeSpan Age => DateTime.Now.Subtract(time);

    public bool Valid => time != DateTime.MinValue;

    public static Aging MakeInvalid => new Aging().Invalidate();

    public static Aging MakeReset => new Aging().Reset();

    public Aging Reset()
    {
        time = DateTime.Now;
        return this;
    }

    public Aging Invalidate()
    {
        time = DateTime.MinValue;
        return this;
    }

    public bool Exceeds(TimeSpan ts)
    {
        return Age > ts;
    }

    public bool Exceeds_sec(int s)
    {
        return Age.TotalSeconds > s;
    }

    public bool Exceeds_sec(uint s)
    {
        return Age.TotalSeconds > s;
    }

    public bool Exceeds_ms(int ms)
    {
        return Age.TotalMilliseconds > ms;
    }

    public bool Exceeds_ms(uint ms)
    {
        return Age.TotalMilliseconds > ms;
    }
}