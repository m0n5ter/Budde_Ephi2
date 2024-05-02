// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Network.SustainedClient
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Ephi.Core.Helping;
using Ephi.Core.Helping.General;

namespace Ephi.Core.Network;

public class SustainedClient : EndPoint
{
    private readonly Aging ageNoConnectLogged = Aging.MakeInvalid;
    private DateTime lastRaisedDisconnectedTooLong = DateTime.MinValue;

    public SustainedClient(string logName = "SustainedClient")
        : base(logName)
    {
        DisconnectedNotificationAfter = TimeSpan.Zero;
    }

    public int Port { get; private set; }

    public TimeSpan DisconnectedNotificationAfter { get; set; }

    public event Action OnDisconnectedTooLong;

    public void ResetDisconnectedTooLong()
    {
        lastRaisedDisconnectedTooLong = DateTime.MinValue;
    }

    public void SetIpPort(IPAddress ipAddress, int port)
    {
        IpAddress = ipAddress;
        Port = port;
    }

    public void Connect(IPAddress ipAddress, int port)
    {
        if (IpAddress != null && IpAddress.Equals(ipAddress) && Port.Equals(port) && ConnectionState == CONNECTION_STATE.CONNECTED)
            return;
        IpAddress = ipAddress;
        Port = port;
        Connect();
    }

    public void Connect()
    {
        if (IpAddress == null)
            throw new ArgumentNullException("IpAddress is NULL");
        Disconnect(true);
        socketThread = new Thread(AsycConnectionWatchdog);
        socketThread.Start();
    }

    private void AsycConnectionWatchdog()
    {
        commitSuicide = false;
        var remoteEP = new IPEndPoint(IpAddress, Port);
        while (!commitSuicide)
        {
            try
            {
                ConnectionState = CONNECTION_STATE.CONNECTING;
                if (Socket == null)
                    Socket = new Socket(remoteEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                var asyncResult = Socket.BeginConnect(remoteEP, null, null);
                while (!commitSuicide)
                {
                    if (asyncResult.AsyncWaitHandle.WaitOne(100, true))
                    {
                        Socket.EndConnect(asyncResult);
                        break;
                    }

                    if (Helpers.Due(ref lastRaisedDisconnectedTooLong, DisconnectedNotificationAfter))
                        AsyncEvents.Raise(OnDisconnectedTooLong);
                    ageNoConnectLogged.Invalidate();
                }

                if (commitSuicide)
                    break;
            }
            catch (Exception ex)
            {
                if ((ex is SocketException socketException ? socketException.ErrorCode : 0) != 10060 && ageNoConnectLogged.Exceeds_sec(60))
                {
                    log.Error("AsycConnect.1", ex);
                    ageNoConnectLogged.Reset();
                }

                ConnectionState = CONNECTION_STATE.ERROR;
                SoftDisconnect();
                Thread.Sleep(400);
                continue;
            }

            if (commitSuicide)
                break;
            lastRaisedDisconnectedTooLong = DateTime.MinValue;
            try
            {
                ConnectionState = CONNECTION_STATE.CONNECTED;
                AsyncListen();
            }
            catch (Exception ex)
            {
                log.Error("AsycConnect.2", ex);
            }
            finally
            {
                log.Info("(Watchdog) AsyncListen returned");
                SoftDisconnect();
            }
        }
    }
}