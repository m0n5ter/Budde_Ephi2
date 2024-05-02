// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.UtcConfiguration
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using Ephi.Core.Helping.Lists;

namespace Ephi.Core.UTC;

public class UtcConfiguration : BitMask
{
    public UtcConfiguration()
    {
        WatchdogEnabled = true;
        DisableOutputsOnDisconnect = true;
        CancelConditionalsOnDisconnect = true;
        HeartbeatTimeoutMs = 500;
    }

    public bool WatchdogEnabled
    {
        get => GetBool(0);
        set => SetBool(0, value);
    }

    public bool DisableOutputsOnDisconnect
    {
        get => GetBool(1);
        set => SetBool(1, value);
    }

    public bool CancelConditionalsOnDisconnect
    {
        get => GetBool(2);
        set => SetBool(2, value);
    }

    public ushort HeartbeatTimeoutMs { get; set; }
}