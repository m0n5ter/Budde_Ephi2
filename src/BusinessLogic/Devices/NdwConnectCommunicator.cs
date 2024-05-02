// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Devices.NdwConnectCommunicator
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using System.Net;
using System.Text;
using System.Threading;
using PharmaProject.BusinessLogic.Misc;
using uPLibrary.Networking.M2Mqtt;

namespace PharmaProject.BusinessLogic.Devices
{
    public static class NdwConnectCommunicator
    {
        private static readonly string clientId = Guid.NewGuid().ToString();
        private static readonly string csdStatusUpdateTopic = "csdState/{0}-{1}";
        private static readonly string barcodeScannedTopic = "main/barcodeScanned/{0}";
        private static readonly string directionRequestedTopic = "main/directionRequested/{0}";
        private static readonly string directionSentTopic = "main/directionSent/{0}";
        private static string lastSentCsdUpdate;
        private static Thread connecting;

        static NdwConnectCommunicator()
        {
            MqttClientSingleton = new MqttClient(IPAddress.Parse("172.16.40.150"));
        }

        public static MqttClient MqttClientSingleton { get; }

        private static void MqttClient_ConnectionClosed(object sender, EventArgs e)
        {
            Start();
        }

        public static bool CsdStatusUpdate(
            uint locationNumber,
            uint csdNumber,
            SEGMENT_STATE csdState,
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
            return Send(string.Format(barcodeScannedTopic, locationNumber), string.Format("{{ \"barcode\": \"{0}\", \"tag\": \"{1}\" }}", barcode, locationNumber));
        }

        public static bool DirectionRequestedUpdate(
            uint locationNumber,
            string barcode,
            WMS_TOTE_DIRECTION direction)
        {
            return Send(string.Format(directionRequestedTopic, locationNumber),
                string.Format("{{ \"barcode\": \"{0}\", \"direction\": \"{1}\", \"tag\": \"{2}\" }}", barcode, (uint)direction, locationNumber));
        }

        public static bool DirectionSentUpdate(
            uint locationNumber,
            string barcode,
            WMS_TOTE_DIRECTION direction)
        {
            return Send(string.Format(directionSentTopic, locationNumber),
                string.Format("{{ \"barcode\": \"{0}\", \"direction\": \"{1}\", \"tag\": \"{2}\" }}", barcode, (uint)direction, locationNumber));
        }

        private static bool Send(string topic, string message)
        {
            if (MqttClientSingleton.IsConnected)
                return MqttClientSingleton.Publish(topic, Encoding.ASCII.GetBytes(message), 1, false) > 0;
            Start();
            return false;
        }

        public static void Start()
        {
            var connecting = NdwConnectCommunicator.connecting;
            if ((connecting != null ? connecting.IsAlive ? 1 : 0 : 0) != 0)
                return;
            Stop();
            MqttClientSingleton.ConnectionClosed += MqttClient_ConnectionClosed;
            NdwConnectCommunicator.connecting = new Thread(AsyncConnect);
            NdwConnectCommunicator.connecting.Start();
        }

        public static void Stop()
        {
            MqttClientSingleton.ConnectionClosed -= MqttClient_ConnectionClosed;
            try
            {
                var connecting = NdwConnectCommunicator.connecting;
                if ((connecting != null ? connecting.IsAlive ? 1 : 0 : 0) != 0)
                    NdwConnectCommunicator.connecting.Abort();
            }
            catch
            {
            }

            if (!MqttClientSingleton.IsConnected)
                return;
            MqttClientSingleton.Disconnect();
        }

        private static void AsyncConnect()
        {
            try
            {
                int num = MqttClientSingleton.Connect(clientId);
            }
            catch
            {
            }
        }
    }
}