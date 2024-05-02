// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.UtcServer
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Linq;
using System.Net;
using Ephi.Core.Network;

namespace Ephi.Core.UTC;

public class UtcServer : SocketServer
{
    public RemoteUtc MakeUtc()
    {
        return MakeUtc(IPAddress.Any);
    }

    public RemoteUtc MakeUtc(string ip)
    {
        return MakeUtc(ip, "RemoteUtc");
    }

    public RemoteUtc MakeUtc(string ip, string logName)
    {
        return MakeUtc(IPAddress.Parse(ip), logName);
    }

    public RemoteUtc MakeUtc(IPAddress ip, string logName = "RemoteUtc")
    {
        var remoteUtc = new RemoteUtc(this, logName);
        remoteUtc.ReplaceIp(ip);
        return remoteUtc;
    }

    public T MakeUtc<T>(string logName = "RemoteUtc", params object[] args) where T : RemoteUtc
    {
        return (T)Activator.CreateInstance(typeof(T), new object[2]
        {
            this,
            logName
        }.Concat(args).ToArray());
    }

    private new void RegisterClient(SustainedExpectedClient client)
    {
    }

    private new void UnRegisterClient(SustainedExpectedClient client)
    {
    }
}