﻿// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Network.CONNECTION_STATE
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll


namespace Ephi.Core.Network;

public enum CONNECTION_STATE
{
    DISCONNECTED,
    CONNECTING,
    CONNECTED,
    DISCONNECTING,
    LISTENING,
    ERROR
}