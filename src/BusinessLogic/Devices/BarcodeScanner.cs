using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Ephi.Core.Helping.General;
using Ephi.Core.Network;

namespace PharmaProject.BusinessLogic.Devices
{
    public class BarcodeScanner : SustainedClient
    {
        private const char STX = '\u0002';
        private const char ETX = '\u0003';
        public const string NO_READ_MSG = "NoRead";
        private const string HB_MSG = "HeartBeat";
        private const int HB_TIMEOUT = 2000;
        private static readonly List<BarcodeScanner> allBarcodeScanners = new List<BarcodeScanner>();
        private readonly IPAddress ip;
        private readonly int port;
        private readonly DelayedEvent hbWatchDog;

        public BarcodeScanner(string logName, IPAddress ip, int port = 2112)
            : base(logName)
        {
            this.ip = ip;
            this.port = port;
            hbWatchDog = new DelayedEvent(TimeSpan.FromMilliseconds(2000.0), ReConnect);
            allBarcodeScanners.Add(this);
        }

        public static void ConnectAllScanners()
        {
            foreach (var allBarcodeScanner in allBarcodeScanners)
                allBarcodeScanner.Connect(allBarcodeScanner.ip, allBarcodeScanner.port);
        }

        public static void DisconnectAllScanners()
        {
            foreach (var allBarcodeScanner in allBarcodeScanners)
                allBarcodeScanner.Disconnect();
        }

        public event Action<string> OnBarcodeScanned;

        public event Action OnNoRead;

        protected override void ConnectionStateChanged()
        {
            base.ConnectionStateChanged();

            if (ConnectionState != CONNECTION_STATE.CONNECTED)
                return;

            hbWatchDog.Start();
        }

        private void ReConnect()
        {
            SoftDisconnect();
            Connect();
        }

        protected override void MessageReceived(byte[] message)
        {
            hbWatchDog.Start();

            if (message.Length < 2)
                return;

            var str1 = Encoding.ASCII.GetString(message, 1, message.Length - 2);
            var separator = new char[2] { '\u0002', '\u0003' };

            foreach (var str2 in str1.Split(separator, StringSplitOptions.RemoveEmptyEntries))
            {
                switch (str2)
                {
                    case "HeartBeat":
                        continue;
                    case "NoRead":
                        var onNoRead = OnNoRead;
                        onNoRead?.Invoke();
                        continue;
                    default:
                        var onBarcodeScanned = OnBarcodeScanned;
                        onBarcodeScanned?.Invoke(str2);

                        continue;
                }
            }

            hbWatchDog.Start();
        }
    }
}