// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Network.EndPoint
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Ephi.Core.Helping;
using Ephi.Core.Helping.General;
using log4net;

namespace Ephi.Core.Network;

public abstract class EndPoint : BaseConnection
{
    public static bool TEST_NO_REQUIRE_ACK;
    private readonly Dictionary<int, AwaitingAckMessage> awaitingAckQueue = new();
    private readonly object workCycleLock = new();
    public Aging CommunicationAge = Aging.MakeInvalid;
    private DateTime lastWorkCycle;
    public Aging ReceivedAge = Aging.MakeInvalid;
    private Queue<SocketMessage> sendQueue = new();
    public Aging SentAge = Aging.MakeInvalid;
    private TimeSpan workCycleTimeout;

    public EndPoint(string logName = "EndPoint")
        : base(logName)
    {
        ConnectionState = CONNECTION_STATE.DISCONNECTED;
        HeartbeatTimeout = TimeSpan.MaxValue;
        WorkCycleInterval = TimeSpan.MaxValue;
    }

    public EndPoint(ILog log)
        : base(log)
    {
        ConnectionState = CONNECTION_STATE.DISCONNECTED;
        HeartbeatTimeout = TimeSpan.MaxValue;
    }

    public override CONNECTION_STATE ConnectionState
    {
        get => base.ConnectionState;
        protected set
        {
            if (base.ConnectionState == value)
                return;
            try
            {
                if (value != CONNECTION_STATE.CONNECTED)
                    return;
                if (WorkCycleInterval != TimeSpan.MaxValue)
                    LastWorkCycle = DateTime.Now.Subtract(WorkCycleInterval);
                ReceivedAge.Reset();
                SentAge.Reset();
                CommunicationAge.Reset();
            }
            finally
            {
                base.ConnectionState = value;
            }
        }
    }

    public bool SendQueueEmpty
    {
        get
        {
            lock (sendQueue)
            {
                return sendQueue.Count == 0;
            }
        }
    }

    public TimeSpan HeartbeatTimeout { get; set; }

    protected DateTime LastWorkCycle
    {
        get
        {
            lock (workCycleLock)
            {
                return lastWorkCycle;
            }
        }
        set
        {
            lock (workCycleLock)
            {
                lastWorkCycle = value;
            }
        }
    }

    protected TimeSpan WorkCycleInterval
    {
        get
        {
            lock (workCycleLock)
            {
                return workCycleTimeout;
            }
        }
        set
        {
            lock (workCycleLock)
            {
                workCycleTimeout = value;
            }
        }
    }

    ~EndPoint()
    {
        Disconnect();
    }

    public virtual void SendHeartbeat()
    {
        AsyncEvents.Raise(OnSendHeartbeat, this);
        SentAge.Reset();
        CommunicationAge.Reset();
    }

    protected virtual void MessageReceived(byte[] message)
    {
        ReceivedAge.Reset();
        CommunicationAge.Reset();
        AsyncEvents.Raise(OnMessageReceived, this, new SocketMessage(message));
    }

    protected virtual void MessageSent(SocketMessage message)
    {
        SentAge.Reset();
        CommunicationAge.Reset();
        AsyncEvents.Raise(OnMessageSent, this, message);
    }

    protected virtual void DoYourWork()
    {
        LastWorkCycle = DateTime.Now;
    }

    public void Send(byte[] message)
    {
        Send(message, int.MinValue);
    }

    public void Send(byte[] message, int ackId)
    {
        if (TEST_NO_REQUIRE_ACK)
            ackId = int.MinValue;
        lock (sendQueue)
        {
            sendQueue.Enqueue(ackId == int.MinValue ? new SocketMessage(message) : new AwaitingAckMessage(message, ackId));
        }
    }

    public void Acknowledged(int ackId)
    {
        lock (awaitingAckQueue)
        {
            if (awaitingAckQueue.ContainsKey(ackId))
                awaitingAckQueue.Remove(ackId);
        }

        lock (sendQueue)
        {
            if (!sendQueue.OfType<AwaitingAckMessage>().Any(aa => aa.AckId == ackId))
                return;
            var collection = new List<SocketMessage>(sendQueue);
            collection.RemoveAll(aa => (aa is AwaitingAckMessage awaitingAckMessage ? awaitingAckMessage.AckId : int.MinValue) == ackId);
            sendQueue = new Queue<SocketMessage>(collection);
        }
    }

    protected virtual void ClearResendQueue()
    {
        lock (awaitingAckQueue)
        {
            awaitingAckQueue.Clear();
        }
    }

    protected virtual void ClearSendQueue()
    {
        lock (sendQueue)
        {
            sendQueue.Clear();
        }
    }

    protected void AsyncListen()
    {
        ConnectionState = CONNECTION_STATE.CONNECTED;
        lock (awaitingAckQueue)
        {
            awaitingAckQueue.Values.ToList().ForEach(aa => aa.ResetSendCounter());
        }

        do
        {
            try
            {
                if (WorkCycleInterval != TimeSpan.MaxValue && DateTime.Now.Subtract(LastWorkCycle) > WorkCycleInterval)
                    DoYourWork();
                var socket1 = Socket;
                if ((socket1 != null ? socket1.Connected ? 1 : 0 : 0) == 0)
                    break;
                if (HeartbeatTimeout != TimeSpan.MaxValue && CommunicationAge.Age > HeartbeatTimeout && ConnectionState == CONNECTION_STATE.CONNECTED)
                    SendHeartbeat();
                var socket2 = Socket;
                var available = socket2?.Available ?? 0;
                bool flag;
                lock (sendQueue)
                {
                    flag = sendQueue.Count > 0;
                }

                if (available == 0 && !flag)
                {
                    if (useKeepAlive)
                        Socket.Send(new byte[0]);
                    Thread.Sleep(100);
                    lock (awaitingAckQueue)
                    {
                        if (awaitingAckQueue.Count == 0)
                            goto label_36;
                    }
                }

                if (available > 0)
                {
                    var numArray = new byte[available];
                    Socket?.Receive(numArray, available, SocketFlags.None);
                    MessageReceived(numArray);
                }

                if (flag)
                    ProcessSendQueue();
                if (!ProcessResendQueue())
                {
                    log.Error("Messages were not acknowledged after multiple resends. Disconnecting");
                    break;
                }
            }
            catch (ThreadAbortException ex)
            {
            }
            catch (Exception ex)
            {
                log.Error(nameof(AsyncListen), ex);
                break;
            }

            label_36: ;
        } while (!commitSuicide);
    }

    private void ProcessSendQueue()
    {
        lock (sendQueue)
        {
            while (sendQueue.Count > 0)
            {
                var message = sendQueue.Dequeue();
                if (message is AwaitingAckMessage)
                    lock (awaitingAckQueue)
                    {
                        var awaitingAckMessage = message as AwaitingAckMessage;
                        if (!awaitingAckQueue.ContainsKey(awaitingAckMessage.AckId))
                            awaitingAckQueue.Add(awaitingAckMessage.AckId, awaitingAckMessage);
                    }

                DoSendMessage(message);
            }
        }
    }

    private void DoSendMessage(SocketMessage message)
    {
        Socket?.Send(message.Payload);
        (message as AwaitingAckMessage)?.WasSent();
        MessageSent(message);
    }

    private bool ProcessResendQueue()
    {
        lock (awaitingAckQueue)
        {
            if (awaitingAckQueue.Count == 0)
                return true;
            var treshold = DateTime.Now.Subtract(TimeSpan.FromSeconds(2.0));
            foreach (var message in awaitingAckQueue.Values.ToList().FindAll(aa => aa.TimeSent < treshold))
            {
                if (message.SendCounter > 5)
                    return false;
                log.WarnFormat("Resending: {0}", message);
                DoSendMessage(message);
            }
        }

        return true;
    }

    public override void CleanUp()
    {
        base.CleanUp();
        OnMessageReceived = null;
        OnMessageSent = null;
    }

    public event MessageTransferDelegate OnMessageReceived;

    public event MessageTransferDelegate OnMessageSent;

    public event Action<EndPoint> OnSendHeartbeat;
}