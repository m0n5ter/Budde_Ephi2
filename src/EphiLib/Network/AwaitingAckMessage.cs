// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Network.AwaitingAckMessage
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;

namespace Ephi.Core.Network;

internal class AwaitingAckMessage : SocketMessage
{
    public AwaitingAckMessage(byte[] payload, int ackId)
        : base(payload)
    {
        WasSent();
        AckId = ackId;
    }

    public DateTime TimeSent { get; private set; }

    public int AckId { get; private set; }

    public int SendCounter { get; private set; }

    public void WasSent()
    {
        TimeSent = DateTime.Now;
        ++SendCounter;
    }

    public void ResetSendCounter()
    {
        SendCounter = 0;
    }
}