// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.Serialization.Mqtt_Client
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Ephi.Core.Helping.Serialization;

public class Mqtt_Client
{
    private readonly Dictionary<string, MqttTx> cache = new();
    private Thread connectingThread;
    private string lastWillMessage = string.Empty;
    private QoS lastWillQos;
    private bool lastWillRetain;
    private string lastWillTopic = string.Empty;
    private MqttClient mqttClient;
    private string passWord = string.Empty;
    private readonly object sendLock = new();
    private MQTT_CLIENT_STATE state;
    private DateTime StateAge = DateTime.Now;
    private readonly List<MqttSubscriber> subscribers = new();
    private string userName = string.Empty;

    public string ClientId { get; private set; } = Guid.NewGuid().ToString();

    public MQTT_CLIENT_STATE State
    {
        get => state;
        set
        {
            if (state == value)
                return;
            StateAge = DateTime.Now;
            state = value;
            var onStateChanged = OnStateChanged;
            onStateChanged?.Invoke(this);
        }
    }

    private bool ValidateCertificate(
        object sender,
        X509Certificate certificate,
        X509Chain chain,
        SslPolicyErrors sslPolicyErrors)
    {
        foreach (var chainStatu in chain.ChainStatus)
            if (chainStatu.Status != X509ChainStatusFlags.NoError && chainStatu.Status != X509ChainStatusFlags.UntrustedRoot)
                return false;
        return true;
    }

    private X509Certificate SelectCertificate(
        object sender,
        string targetHost,
        X509CertificateCollection localCertificates,
        X509Certificate remoteCertificate,
        string[] acceptableIssuers)
    {
        if (acceptableIssuers != null && acceptableIssuers.Length != 0 && localCertificates != null && localCertificates.Count > 0)
            foreach (var localCertificate in localCertificates)
            {
                var issuer = localCertificate.Issuer;
                if (Array.IndexOf(acceptableIssuers, issuer) != -1)
                    return localCertificate;
            }

        return localCertificates != null && localCertificates.Count > 0 ? localCertificates[0] : null;
    }

    public event Action<Mqtt_Client> OnStateChanged;

    public void Start(string clientId, string ipAddress, int port = 1883)
    {
        Stop();
        if (!string.IsNullOrEmpty(clientId))
            ClientId = clientId;
        if (mqttClient != null)
        {
            mqttClient.MqttMsgPublishReceived -= MqttClient_MqttMsgPublishReceived;
            mqttClient.ConnectionClosed -= MqttClient_ConnectionClosed;
        }

        mqttClient = new MqttClient(ipAddress, port, false, null, null, MqttSslProtocols.None);
        if (mqttClient == null)
            throw new NullReferenceException("mqttClient has no- or an invalid Ip address");
        mqttClient.ConnectionClosed += MqttClient_ConnectionClosed;
        mqttClient.MqttMsgPublishReceived += MqttClient_MqttMsgPublishReceived;
        StartConnectingThread();
    }

    public void Start(
        string clientId,
        string hostName,
        string userName,
        string passWord,
        bool secure = false,
        X509Certificate caCertificate = null,
        X509Certificate clientCertificate = null,
        LocalCertificateSelectionCallback userCertificateSelectionCallback = null,
        RemoteCertificateValidationCallback userCertificateValidationCallback = null,
        int Port = 1883,
        MqttSslProtocols SslVersion = MqttSslProtocols.None)
    {
        Stop();
        if (!string.IsNullOrEmpty(clientId))
            ClientId = clientId;
        this.userName = userName;
        this.passWord = passWord;
        if (mqttClient != null)
        {
            mqttClient.ConnectionClosed -= MqttClient_ConnectionClosed;
            mqttClient.MqttMsgPublishReceived -= MqttClient_MqttMsgPublishReceived;
        }

        mqttClient = new MqttClient(hostName, Port, secure, caCertificate, clientCertificate, SslVersion, userCertificateValidationCallback ?? ValidateCertificate,
            userCertificateSelectionCallback ?? SelectCertificate);
        if (mqttClient == null)
            throw new NullReferenceException("mqttClient was not yet started");
        mqttClient.ConnectionClosed += MqttClient_ConnectionClosed;
        mqttClient.MqttMsgPublishReceived += MqttClient_MqttMsgPublishReceived;
        StartConnectingThread();
    }

    public void Stop()
    {
        if (mqttClient == null)
            return;
        StopConnectingThread();
        if (!mqttClient.IsConnected)
            return;
        mqttClient.Disconnect();
    }

    public bool Reconnect()
    {
        if (State != MQTT_CLIENT_STATE.STARTED || DateTime.Now.Subtract(StateAge) < TimeSpan.FromSeconds(10.0))
            return false;
        Stop();
        StartConnectingThread();
        return true;
    }

    public bool Publish<T>(string topic, T item, bool retain = true, QoS qoS = QoS.AtLeastOnce, bool lastWill = false)
    {
        if (item == null)
            return false;
        var input = JsonConvert.SerializeObject(item);
        if (string.IsNullOrWhiteSpace(input))
            return false;
        var message = Regex.Replace(input, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
        foreach (var keyValuePair in (Activator.CreateInstance(typeof(T)) is MqttBaseMessage instance ? instance.LocalToMessageFieldnameMapping : null) ?? new Dictionary<string, string>())
            message = message.Replace($"\"{keyValuePair.Key}\":", $"\"{keyValuePair.Value}\":");
        return Publish(topic, message, retain, qoS, lastWill);
    }

    public bool Publish(string topic, string message, bool retain = true, QoS qoS = QoS.AtLeastOnce, bool lastWill = false)
    {
        if (lastWill)
        {
            lastWillTopic = topic;
            lastWillMessage = message;
            lastWillRetain = retain;
            lastWillQos = qoS;
            return true;
        }

        lock (sendLock)
        {
            var msg = new MqttTx(topic, message, retain, qoS);
            var mqttClient = this.mqttClient;
            if ((mqttClient != null ? mqttClient.IsConnected ? 1 : 0 : 0) != 0)
            {
                FlushCache();
                return DoPublish(msg);
            }

            if (cache.ContainsKey(topic))
                cache[topic] = msg;
            else
                cache.Add(topic, msg);
        }

        return false;
    }

    private bool DoPublish(MqttTx msg)
    {
        try
        {
            return mqttClient.Publish(msg.Topic, Encoding.ASCII.GetBytes(msg.Payload), (byte)msg.QoS, msg.Retain) > 0;
        }
        catch
        {
            return false;
        }
    }

    private void FlushCache()
    {
        lock (sendLock)
        {
            foreach (var keyValuePair in cache)
                DoPublish(keyValuePair.Value);
            cache.Clear();
        }
    }

    public void Subscribe<T>(
        Action<string, T> callBack,
        QoS qos,
        bool raiseWithNullWhenUninterpretable,
        params string[] topics)
        where T : MqttBaseMessage
    {
        var s = new MqttSubscriber<T>(callBack, qos, raiseWithNullWhenUninterpretable);
        s.Topics.AddRange(topics);
        lock (subscribers)
        {
            subscribers.Add(s);
        }

        if (mqttClient == null)
            return;
        DoSubscribe(s);
    }

    public void ClearSubscriptions()
    {
        lock (subscribers)
        {
            foreach (var subscriber in subscribers)
            {
                int num = mqttClient.Unsubscribe(subscriber.Topics.ToArray());
            }

            subscribers.Clear();
        }
    }

    private void DoSubscribe(MqttSubscriber s)
    {
        var array = s.Topics.ToArray();
        int num1 = mqttClient.Unsubscribe(array);
        int num2 = mqttClient.Subscribe(array, Enumerable.Repeat((byte)s.QoS, s.Topics.Count).ToArray());
    }

    private void SubscribeAll()
    {
        lock (subscribers)
        {
            foreach (var subscriber in subscribers)
                DoSubscribe(subscriber);
        }
    }

    private bool Subscribed(string topic, MqttSubscriber s)
    {
        var flag = true;
        var strArray1 = topic.Split('/');
        foreach (var topic1 in s.Topics)
        {
            var chArray = new char[1] { '/' };
            var strArray2 = topic1.Split(chArray);
            if (strArray2.Length <= strArray1.Length)
            {
                for (var index = 0; index < strArray1.Length; ++index)
                {
                    flag = true;
                    if (strArray2[index] == "#")
                        return flag;
                    if (!(strArray2[index] == "+"))
                    {
                        flag = strArray2[index].Equals(strArray1[index]);
                        if (!flag)
                            break;
                    }
                }

                if (flag)
                    return true;
            }
        }

        return false;
    }

    private void MqttClient_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        var json = Encoding.ASCII.GetString(e.Message);
        lock (subscribers)
        {
            foreach (var subscriber in subscribers)
                if (Subscribed(e.Topic, subscriber))
                    subscriber.RaiseCallback(e.Topic, json);
        }
    }

    private void StartConnectingThread()
    {
        StopConnectingThread();
        connectingThread = new Thread(AsyncConnect);
        connectingThread.Start();
    }

    private void AsyncConnect()
    {
        State = MQTT_CLIENT_STATE.STARTING;
        try
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(passWord))
            {
                int num1 = mqttClient.Connect(ClientId);
            }
            else
            {
                int num2 = mqttClient.Connect(ClientId, userName, passWord);
            }
        }
        catch (Exception ex)
        {
        }
        finally
        {
            if (mqttClient.IsConnected)
            {
                SubscribeAll();
                FlushCache();
                State = MQTT_CLIENT_STATE.STARTED;
            }

            Thread.Sleep(1000);
            connectingThread = null;
            if (!mqttClient.IsConnected && State == MQTT_CLIENT_STATE.STARTING)
                StartConnectingThread();
        }
    }

    private void StopConnectingThread()
    {
        if (connectingThread == null)
            return;
        State = MQTT_CLIENT_STATE.STOPPING;
        try
        {
            if (!connectingThread.IsAlive || connectingThread.Join(500))
                return;
            connectingThread?.Abort();
        }
        catch
        {
        }
        finally
        {
            connectingThread = null;
            State = MQTT_CLIENT_STATE.STOPPED;
        }
    }

    private void MqttClient_ConnectionClosed(object sender, EventArgs e)
    {
        if (State == MQTT_CLIENT_STATE.STARTED)
            Stop();
        StartConnectingThread();
    }

    private class MqttTx
    {
        public MqttTx(string topic, string jsonPayload)
        {
            Topic = topic;
            Payload = jsonPayload;
        }

        public MqttTx(string topic, string jsonPayload, bool retain)
            : this(topic, jsonPayload)
        {
            Retain = retain;
        }

        public MqttTx(string topic, string jsonPayload, bool retain, QoS qualityOfService)
            : this(topic, jsonPayload, retain)
        {
            QoS = qualityOfService;
        }

        public string Topic { get; }

        public string Payload { get; }

        public QoS QoS { get; }

        public bool Retain { get; }
    }

    private abstract class MqttSubscriber
    {
        internal readonly QoS QoS;
        protected readonly bool RaiseWithNullWhenUninterpretable;
        internal readonly List<string> Topics;

        public MqttSubscriber(QoS qos, bool raiseWithNullWhenUninterpretable)
        {
            Topics = new List<string>();
            QoS = qos;
            RaiseWithNullWhenUninterpretable = raiseWithNullWhenUninterpretable;
        }

        internal abstract void RaiseCallback(string topic, string json);
    }

    private class MqttSubscriber<T> : MqttSubscriber where T : MqttBaseMessage
    {
        private readonly Action<string, T> Callback;

        public MqttSubscriber(
            Action<string, T> callback,
            QoS qos,
            bool raiseWithNullWhenUninterpretable)
            : base(qos, raiseWithNullWhenUninterpretable)
        {
            Callback = callback;
        }

        internal override void RaiseCallback(string topic, string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                if (!RaiseWithNullWhenUninterpretable)
                    return;
                Callback(topic, default);
            }
            else
            {
                try
                {
                    json = Regex.Replace(json, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                    foreach (var keyValuePair in (Activator.CreateInstance(typeof(T)) is MqttBaseMessage instance ? instance.LocalToMessageFieldnameMapping : null) ?? new Dictionary<string, string>())
                        json = json.Replace($"\"{keyValuePair.Value}\":", $"\"{keyValuePair.Key}\":");
                    var obj = JsonConvert.DeserializeObject<T>(json);
                    if (obj == null && !RaiseWithNullWhenUninterpretable)
                        return;
                    Callback(topic, obj);
                }
                catch (NullReferenceException ex)
                {
                    Console.WriteLine(ex.Message);
                    if (!RaiseWithNullWhenUninterpretable)
                        return;
                    Callback(topic, default);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    if (!RaiseWithNullWhenUninterpretable)
                        return;
                    Callback(topic, default);
                }
            }
        }
    }
}