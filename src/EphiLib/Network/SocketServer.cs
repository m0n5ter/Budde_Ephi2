// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Network.SocketServer
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Ephi.Core.Helping.General;
using log4net;

namespace Ephi.Core.Network;

public class SocketServer
{
    private readonly List<SustainedExpectedClient> expected = new();
    private readonly ILog log;
    private bool commitSuicide;
    private TcpListener listener;
    private Thread listenThread;
    private bool softRestart;
    private volatile CONNECTION_STATE state;

    public SocketServer(string logName = "SocketServer")
    {
        log = LogManager.GetLogger(logName);
    }

    public IPAddress IpAddress { get; set; } = IPAddress.Any;

    public virtual CONNECTION_STATE State
    {
        get => state;
        protected set
        {
            var state = this.state;
            this.state = value;
            StateChanged(state);
        }
    }

    public int Port { get; private set; }

    public void RegisterClient(SustainedExpectedClient client)
    {
        lock (expected)
        {
            expected.Add(client);
        }
    }

    public SustainedExpectedClient RegisterClient(Socket socket)
    {
        var sustainedExpectedClient = new SustainedExpectedClient((socket.RemoteEndPoint as IPEndPoint).Address);
        sustainedExpectedClient.ReplaceSocket(socket);
        lock (expected)
        {
            expected.Add(sustainedExpectedClient);
        }

        return sustainedExpectedClient;
    }

    public void UnRegisterClient(SustainedExpectedClient client)
    {
        lock (expected)
        {
            expected.Remove(client);
        }

        client.Disconnect();
    }

    public void ClearRegistered()
    {
        lock (expected)
        {
            expected.Clear();
        }
    }

    public event Action<SocketServer> OnStateChanged;

    protected virtual void StateChanged(CONNECTION_STATE oldState)
    {
        AsyncEvents.Raise(OnStateChanged, this);
    }

    public virtual void Disconnect(bool includingRegisteredClients = false)
    {
        commitSuicide = true;
        SoftDisconnect(includingRegisteredClients);
        if (listenThread == null)
            return;
        if (!listenThread.Join(1000))
        {
            listenThread.Abort();
            log.Error("Thread could not be stopped gracefully and needed to be aborted.");
        }

        listenThread = null;
    }

    protected virtual void SoftDisconnect(bool includingRegisteredClients = false)
    {
        State = CONNECTION_STATE.DISCONNECTING;
        try
        {
            softRestart = true;
            lock (expected)
            {
                foreach (BaseConnection baseConnection in expected)
                    baseConnection.Disconnect();
            }
        }
        finally
        {
            State = CONNECTION_STATE.DISCONNECTED;
        }
    }

    public event Func<Socket, bool> OnSocketAccepted;

    protected virtual bool SocketAccepted(Socket socket)
    {
        return OnSocketAccepted != null && OnSocketAccepted(socket);
    }

    public void SetIpPort(int port, IPAddress ipAddress = null)
    {
        IpAddress = ipAddress ?? IPAddress.Any;
        Port = port;
    }

    public void Connect(int port, string ip)
    {
        Connect(port, IPAddress.Parse(ip));
    }

    public void Connect(int port, IPAddress ipAddress = null)
    {
        ipAddress = ipAddress ?? IPAddress.Any;
        if (IpAddress.Equals(ipAddress) && Port.Equals(port) && State == CONNECTION_STATE.LISTENING)
            return;
        IpAddress = ipAddress;
        Port = port;
        Connect();
    }

    public void Connect()
    {
        IpAddress = IpAddress ?? IPAddress.Any;
        Disconnect();
        listenThread = new Thread(AsycListen);
        listenThread.Start();
    }

    private void AsycListen()
    {
        commitSuicide = false;
        Socket incoming = null;
        var localEP = new IPEndPoint(IpAddress, Port);
        while (!commitSuicide)
            try
            {
                State = CONNECTION_STATE.CONNECTING;
                softRestart = false;
                listener = new TcpListener(localEP);
                listener.Start();
                State = CONNECTION_STATE.LISTENING;
                while (!softRestart && !commitSuicide)
                {
                    if (listener.Pending())
                    {
                        incoming = listener.AcceptSocket();
                        if (incoming != null)
                            ProcessIncomingSocket(incoming);
                    }

                    if (incoming != null)
                        incoming = null;
                    else
                        Thread.Sleep(400);
                }

                listener.Stop();
                listener = null;
                lock (expected)
                {
                    expected.AsParallel().ForAll(s => s.Disconnect());
                }
            }
            catch (Exception ex)
            {
                log.Error(nameof(AsycListen), ex);
                SoftDisconnect();
                State = CONNECTION_STATE.ERROR;
                listener?.Stop();
                listener = null;
                Thread.Sleep(500);
            }
    }

    private void ProcessIncomingSocket(Socket incoming)
    {
        lock (expected)
        {
            if (expected.Count > 0)
            {
                var remoteIp = (incoming.RemoteEndPoint as IPEndPoint).Address;
                var sustainedExpectedClient = expected.FirstOrDefault(ec => ec.IpAddress != null && ec.IpAddress.Equals(remoteIp));
                if (sustainedExpectedClient != null)
                {
                    sustainedExpectedClient.ReplaceSocket(incoming);
                    return;
                }
            }
        }

        if (SocketAccepted(incoming))
            return;
        incoming.Disconnect(false);
        incoming.Dispose();
    }
}