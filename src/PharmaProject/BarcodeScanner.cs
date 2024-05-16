// Decompiled with JetBrains decompiler
// Type: PharmaProject.BarcodeScanner
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using System.Net;
using System.Text;
using Ephi.Core.Network;

namespace PharmaProject
{
    public class BarcodeScanner : SustainedClient
    {
        private const char STX = '\u0002';
        private const char ETX = '\u0003';

        public BarcodeScanner(string logName, IPAddress ip, int port = 2112)
            : base(logName)
        {
            Connect(ip, port);
        }

        public event Action<string> OnBarcodeScanned;

        public event Action OnNoRead;

        protected override void MessageReceived(byte[] message)
        {
            if (message.Length < 2)
                return;
            var str1 = Encoding.ASCII.GetString(message, 1, message.Length - 2);
            var separator = new char[2] { '\u0002', '\u0003' };
            foreach (var str2 in str1.Split(separator, StringSplitOptions.RemoveEmptyEntries))
                if (str2 == "NoRead")
                {
                    var onNoRead = OnNoRead;
                    onNoRead?.Invoke();
                }
                else
                {
                    var onBarcodeScanned = OnBarcodeScanned;
                    onBarcodeScanned?.Invoke(str2);
                }
        }
    }
}