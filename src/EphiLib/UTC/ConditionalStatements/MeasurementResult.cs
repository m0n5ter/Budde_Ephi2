// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.MeasurementResult
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;

namespace Ephi.Core.UTC.ConditionalStatements;

public class MeasurementResult
{
    private readonly DateTime created = DateTime.Now;

    public MeasurementResult(int elapsedMs, bool flyingStart, bool flyingStop, int pauseCount)
    {
        Elapsed = TimeSpan.FromMilliseconds(elapsedMs);
        FlyingStart = flyingStart;
        FlyingStop = flyingStop;
        PauseCount = pauseCount;
    }

    public TimeSpan Age => DateTime.Now.Subtract(created);

    public TimeSpan Elapsed { get; }

    public bool FlyingStart { get; }

    public bool FlyingStop { get; }

    public int PauseCount { get; }

    public override string ToString()
    {
        return $"Measurement: Elapsed:{Elapsed}, fStart:{FlyingStart}, fStop:{FlyingStop}, Pauses:{PauseCount}";
    }
}