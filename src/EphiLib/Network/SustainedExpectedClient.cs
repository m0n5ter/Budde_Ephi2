// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Network.SustainedExpectedClient
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;

namespace Ephi.Core.Network;

public class SustainedExpectedClient : EndPoint
{
    public SustainedExpectedClient(string logName = "SustainedExpectedClient")
        : base(logName)
    {
    }

    public SustainedExpectedClient(ILog log)
        : base(log)
    {
    }

    public SustainedExpectedClient(IPAddress ipAddress, string logName = "SustainedExpectedClient")
        : base(logName)
    {
        ReplaceIp(ipAddress);
    }

    public SustainedExpectedClient(IPAddress ipAddress, ILog log)
        : base(log)
    {
        ReplaceIp(ipAddress);
    }

    public void ReplaceIp(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out var address))
            address = IPAddress.Parse("127.0.0.1");
        ReplaceIp(address);
    }

    public void ReplaceIp(IPAddress ipAddress)
    {
        if (ipAddress != null && ipAddress.Equals(IpAddress))
            return;
        IpAddress = ipAddress;
        Disconnect();
    }

    protected internal void ReplaceSocket(Socket socket)
    {
        Disconnect();
        ConnectionState = CONNECTION_STATE.CONNECTING;
        commitSuicide = false;
        socketThread = new Thread(AsyncReplaceSocket);
        log?.Info("Starts listening to incoming socket");
        socketThread.Start(socket);
    }

    internal void AsyncReplaceSocket(object oSocket)
    {
        try
        {
            Socket = oSocket as Socket;
            ConnectionState = CONNECTION_STATE.CONNECTED;
            AsyncListen();
        }
        catch (Exception ex)
        {
            log.Error(nameof(AsyncReplaceSocket), ex);
        }
        finally
        {
            log.Info("(ReplaceSocket) AsyncListen returned");
            SoftDisconnect();
            Socket = null;
        }
    }
}