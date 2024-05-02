﻿// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Network.SocketMessage
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System.Linq;

namespace Ephi.Core.Network;

public class SocketMessage
{
    public SocketMessage(byte[] payload)
    {
        Payload = payload;
    }

    public byte[] Payload { get; }

    public override string ToString()
    {
        return string.Join(" ", Payload.Select(b => b.ToString("X2")));
    }
}