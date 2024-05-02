// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Network.IBaseConnection
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Net;

namespace Ephi.Core.Network;

public interface IBaseConnection
{
    CONNECTION_STATE ConnectionState { get; }

    IPAddress IpAddress { get; }

    bool Connected { get; }

    event Action<IBaseConnection> OnConnectionStateChanged;

    void Disconnect(bool synchronous);

    void CleanUp();
}