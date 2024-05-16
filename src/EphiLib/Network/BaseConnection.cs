// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Network.BaseConnection
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Ephi.Core.Helping;
using Ephi.Core.Helping.General;
using log4net;

namespace Ephi.Core.Network;

public abstract class BaseConnection : IBaseConnection
{
    public readonly DelayedEventContainer<BaseConnection> DelayedEvents;
    protected bool commitSuicide = true;
    private volatile CONNECTION_STATE connectionState;
    protected DateTime connectionStateChangedAt = DateTime.Now;
    private volatile DelayedEvent delayedDisconnect;
    private readonly uint keepAliveInterval_ms = 1000;
    private readonly uint keepAliveStart_ms = 2000;
    protected ILog log;
    protected string logName;
    private volatile CONNECTION_STATE realConnectionState;
    private Socket socket;
    protected Thread socketThread;

    public BaseConnection(string logName = "BaseConnection")
        : this(LogManager.GetLogger(logName))
    {
    }

    public BaseConnection(ILog log)
    {
        logName = log.Logger.Name;
        this.log = log ?? LogManager.GetLogger(nameof(BaseConnection));
        DelayedEvents = new DelayedEventContainer<BaseConnection>(this);
    }

    protected Socket Socket
    {
        get => socket;
        set
        {
            socket = value;
            ApplyKeepAlive();
        }
    }

    public bool useKeepAlive { get; private set; }

    public uint DisconnectedStateDelay_ms
    {
        get
        {
            var delayedDisconnect = this.delayedDisconnect;
            return delayedDisconnect?.Delay_ms ?? 0U;
        }
        set
        {
            if ((int)DisconnectedStateDelay_ms == (int)value)
                return;
            if (DisconnectedStateDelay_ms > 0U)
            {
                delayedDisconnect.Dispose();
                delayedDisconnect = null;
            }

            if (value <= 0U)
                return;
            delayedDisconnect = new DelayedEvent(value, DelayedDisconnectElapsed);
        }
    }

    protected TimeSpan ConnectionStateAge => Helpers.Since(connectionStateChangedAt);

    public IPAddress IpAddress { get; protected set; }

    public event Action<IBaseConnection> OnConnectionStateChanged;

    public virtual CONNECTION_STATE ConnectionState
    {
        get => connectionState;
        protected set
        {
            if (realConnectionState == value)
                return;
            delayedDisconnect?.Start();
            realConnectionState = value;
            if (Helpers.Contains(value, CONNECTION_STATE.DISCONNECTING, CONNECTION_STATE.DISCONNECTED) && DisconnectedStateDelay_ms > 0U)
                return;
            SetConnectionState(value);
        }
    }

    public bool Connected => ConnectionState == CONNECTION_STATE.CONNECTED || ConnectionState == CONNECTION_STATE.LISTENING;

    public virtual void Disconnect(bool synchronous = false)
    {
        if (socketThread == null)
            return;
        try
        {
            commitSuicide = true;
            if (synchronous)
                AsyncDisconnect(500);
            else
                Task.Factory.StartNew(() => AsyncDisconnect(500)).Wait(600);
        }
        finally
        {
            SoftDisconnect();
            socketThread = null;
        }
    }

    public virtual void CleanUp()
    {
        OnConnectionStateChanged = null;
        Disconnect();
    }

    public void DumpBytes(string description, SocketMessage message)
    {
        DumpBytes(description, message?.Payload);
    }

    public void DumpBytes(string description, byte[] bytes)
    {
        if (bytes == null)
            log.InfoFormat("{0} => NULL", description);
        else
            log.InfoFormat("{0}\n{1}", description, Formatting.BytesToString(bytes));
    }

    public void KeepAlive(bool enable, uint keepAliveStart_ms = 1000, uint keepAliveInterval_ms = 10000)
    {
        useKeepAlive = enable;
        ApplyKeepAlive();
    }

    private void ApplyKeepAlive()
    {
        Socket socket;
        if ((socket = Socket) == null)
            return;
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, useKeepAlive);
        uint structure = 0;
        var optionInValue = new byte[Marshal.SizeOf(structure) * 3];
        BitConverter.GetBytes(useKeepAlive ? 1U : 0U).CopyTo(optionInValue, 0);
        BitConverter.GetBytes(keepAliveStart_ms).CopyTo(optionInValue, Marshal.SizeOf(structure));
        BitConverter.GetBytes(keepAliveInterval_ms).CopyTo(optionInValue, Marshal.SizeOf(structure) * 2);
        socket.IOControl(IOControlCode.KeepAliveValues, optionInValue, null);
    }

    private void SetConnectionState(CONNECTION_STATE value)
    {
        connectionState = value;
        connectionStateChangedAt = DateTime.Now;
        ConnectionStateChanged();
    }

    private void DelayedDisconnectElapsed()
    {
        if (!Helpers.Contains(realConnectionState, CONNECTION_STATE.DISCONNECTING, CONNECTION_STATE.DISCONNECTED))
            return;
        SetConnectionState(realConnectionState);
    }

    protected virtual void ConnectionStateChanged()
    {
        if (commitSuicide)
            return;
        DelayedEvents.ResetTimers();
        AsyncEvents.Raise(OnConnectionStateChanged, ex => log.Error(nameof(ConnectionStateChanged), ex), this);
    }

    protected virtual void SoftDisconnect()
    {
        try
        {
            if (ConnectionState != CONNECTION_STATE.DISCONNECTED)
                ConnectionState = CONNECTION_STATE.DISCONNECTING;
            try
            {
                var socket = Socket;
                if ((socket != null ? socket.Connected ? 1 : 0 : 0) != 0)
                    Socket?.Disconnect(true);
                Socket?.Close();
                Socket?.Dispose();
            }
            catch (Exception ex)
            {
                log?.Error("Error while reconnecting", ex);
            }
            finally
            {
                Socket = null;
                ConnectionState = CONNECTION_STATE.DISCONNECTED;
            }
        }
        catch
        {
        }
    }

    private void AsyncDisconnect(int timeOut)
    {
        if (socketThread == null)
            return;
        try
        {
            if (!socketThread.IsAlive || socketThread.Join(timeOut))
                return;
            log.Error("Socket thread could not be stopped gracefully and needed to be aborted.");
            socketThread.Abort();
        }
        catch (Exception ex)
        {
            log.Error("Error while Disconnecting", ex);
        }
        finally
        {
            socketThread = null;
        }
    }
}