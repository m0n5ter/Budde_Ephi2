// Decompiled with JetBrains decompiler
// Type: PharmaProject.NdwConnectCommunicator
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using System.Net;
using System.Text;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;

namespace PharmaProject
{
    public static class NdwConnectCommunicator
    {
        private static MqttClient mqttClient;
        private static readonly string clientId = Guid.NewGuid().ToString();
        private static readonly string csdStatusUpdateTopic = "csdState/{0}-{1}";
        private static readonly string barcodeScannedTopic = "main/barcodeScanned/{0}";
        private static readonly string directionRequestedTopic = "main/directionRequested/{0}";
        private static readonly string directionSentTopic = "main/directionSent/{0}";
        private static string lastSentCsdUpdate;
        private static Thread connecting;

        public static MqttClient MqttClientSingleton()
        {
            if (mqttClient == null)
            {
                mqttClient = new MqttClient(IPAddress.Parse("172.16.0.150"));
                mqttClient.ConnectionClosed += MqttClient_ConnectionClosed;
                StartConnectingThread();
            }

            return mqttClient;
        }

        private static void MqttClient_ConnectionClosed(object sender, EventArgs e)
        {
            StartConnectingThread();
        }

        public static bool CsdStatusUpdate(
            uint locationNumber,
            uint csdNumber,
            CSD_STATE csdState,
            TABLE_POSITION tablePosition,
            string rollersMoving,
            string beltsMoving)
        {
            var message = string.Format("{{ \"csdState\": \"{0}\", \"tablePosition\": \"{1}\", \"rollersMoving\": \"{2}\", \"beltsMoving\": \"{3}\" }}", csdState, tablePosition, rollersMoving,
                beltsMoving);
            if (message == lastSentCsdUpdate)
                return false;
            lastSentCsdUpdate = message;
            return Send(string.Format(csdStatusUpdateTopic, locationNumber, csdNumber), message);
        }

        public static bool BarcodeScannedUpdate(uint locationNumber, string barcode)
        {
            return Send(string.Format(barcodeScannedTopic, locationNumber), $"{{ \"barcode\": \"{barcode}\", \"tag\": \"{locationNumber}\" }}");
        }

        public static bool DirectionRequestedUpdate(
            uint locationNumber,
            string barcode,
            WMS_TOTE_DIRECTION direction)
        {
            return Send(string.Format(directionRequestedTopic, locationNumber),
                $"{{ \"barcode\": \"{barcode}\", \"direction\": \"{(uint)direction}\", \"tag\": \"{locationNumber}\" }}");
        }

        public static bool DirectionSentUpdate(
            uint locationNumber,
            string barcode,
            WMS_TOTE_DIRECTION direction)
        {
            return Send(string.Format(directionSentTopic, locationNumber),
                $"{{ \"barcode\": \"{barcode}\", \"direction\": \"{(uint)direction}\", \"tag\": \"{locationNumber}\" }}");
        }

        private static bool Send(string topic, string message)
        {
            var mqttClient = NdwConnectCommunicator.mqttClient;
            if ((mqttClient != null ? mqttClient.IsConnected ? 1 : 0 : 0) != 0)
                return NdwConnectCommunicator.mqttClient.Publish(topic, Encoding.ASCII.GetBytes(message), 1, false) > 0;
            if (NdwConnectCommunicator.mqttClient != null)
            {
                StartConnectingThread();
                return false;
            }

            MqttClientSingleton();
            StartConnectingThread();
            return false;
        }

        public static void Stop()
        {
            StopConnectingThread();
            mqttClient.ConnectionClosed -= MqttClient_ConnectionClosed;
            mqttClient.Disconnect();
        }

        private static void StartConnectingThread()
        {
            StopConnectingThread();
            connecting = new Thread(AsyncConnect);
            connecting.Start();
        }

        private static void StopConnectingThread()
        {
            if (connecting == null)
                return;
            try
            {
                if (!connecting.IsAlive)
                    return;
                connecting?.Abort();
            }
            finally
            {
                connecting = null;
            }
        }

        private static void AsyncConnect()
        {
            try
            {
                int num = mqttClient.Connect(clientId);
            }
            catch (Exception ex)
            {
            }
        }
    }
}