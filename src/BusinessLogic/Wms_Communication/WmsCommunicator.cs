// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Wms_Communication.WmsCommunicator
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using System.Linq;
using System.Threading;
using Ephi.Core.Helping.Log4Net;
using log4net;
using PharmaProject.BusinessLogic.Locations;
using PharmaProject.BusinessLogic.Wms_Communication.Messages;
using Rfc1006LibNet.Advanced;
using Rfc1006LibNet.Advanced.EventArgs;

namespace PharmaProject.BusinessLogic.Wms_Communication
{
    public static class WmsCommunicator
    {
        private static readonly ILog Log;
        private static Rfc1006Server rfc1006Server;
        private static Thread awaitingConnection;
        private static bool DoListen;

        static WmsCommunicator()
        {
            Log = Log4NetHelpers.AddIsolatedLogger("WMS", "..\\Log\\WMS\\");
        }

        public static DateTime LastMessageReceivedTime { get; private set; } = DateTime.MinValue;

        public static event Action<BaseMessage> OnReceived;

        private static Rfc1006Server rfc1006ServerSingleton()
        {
            if (rfc1006Server == null)
            {
                Licenser.LicenseKey =
                    "AYHKXAFJQ4SCDBJVHOOGMVZGX3DLD6RGH65F3MY2UGRKXHBYPIXQDZKQI6IZOWXICLNPDJHZOFVABIMJK2R2463JMURV4PHQ3U7MCNP7QHLLOZVCG6DMMQVONFVGCP6LAFJR7MAXPGPJTH5A55C2L5HCWMIOGR77OYGWFG7SRED75YUIQCAE7JZJRGNBBN3ZLRQ6BCKB2NBYX5KS3VBQPWCSF2URLS2LXIUYHEXMHXEO3Z77AIKIIRWJKNHXPAEPLQO7UXSMZ43ZL5BZICNO3OL5UBCOMTBH4VZHR73H3HBKD3AUKZVXSLYAQGYOPGKVRVA6Z4VFZ6VYDHXQ6VIJMBOZHNLVIKBJOVO5FUHC3IO2FU2IHJRCPMWSDAAZJ5HCQJB3UJXFK3FN6I7E7J7TJZT6PGIBZ7XOC5UQUK2U7V6UGIW2Y7NNMWRM7L3TJV6CAGJRBZMAYFWMQWFVA4XL5HO7TBP4OQBTKU6LVPYICYLQHFD57TANNFHTZ4ATGTM33PYT32DDS2B7VNWEKI73A25RSCIPM22BINTFK7AK2QFJBUW7UQCFATQUMVD2QNVVTOUX5LSO3BC6VQKMOIQJLBAZYOOACKBY32YTVUE7MHATUYSVEWEUOUCUJ2PEVNNTJ26LAQLX2VXEXILSHKNT22LD53RI7D5VLAVJ5R6OTGX6B2LRN5JAREHA3JE3NNR343YHF2ZODMMOIRFIZTBXTQSR4NM5RGKOBSGPDBYZYULLYFDRM4PD7EPN6SLV7MUY4KW6W4ZUTCCAUQG2DGJ6MBK7WSZE4A4XKLWAZQDRCJOUP6S6JPF3IP67SBLXUUA3OFL5Y4F2GKIJZ2QW6DDTAW5JJ4CXCAWIHIKQDH4I5EPGRVDIKGILTODDKOONTJQC4WWA6K4O55XNMU2CBQU6ZTR7PCB3SFDFNCDMPMQSRSRFFEZ35KFJMT5ISOGGM7D46ATMP4XWOHWFO2JHOS3CJACLK4Q5WFMYZU5TJMJUPKXJVHJ6WI4HKYWNBFB6VWCRFBMZXFLABDQIIYVLWMI6W3XR365LVCEZJJRF2J2TBQTMDVNGBJE472TVNZSW3VPXKSY7QD7DTJN6VCDY72HIGUJHH3QX5WJRY2FFIK2JJCFCRYCWG3U6W3EGOJT6OLD2QUBDICEZRATULFCFFNLKKSAXGWEQMV3LKGXR5APAT2R2JMUPBVH6GXW55RDCLGN5HESYLHREFQCJAZXKEL27UPNQQMCKE";
                rfc1006Server = new Rfc1006Server(new Rfc1006IPEndPoint(102, "WMS", "PLC"));
                rfc1006Server.AutoReceive = true;
            }

            return rfc1006Server;
        }

        public static void Start()
        {
            rfc1006Server = rfc1006ServerSingleton();
            rfc1006Server.Received += Rfc1006Server_Received;
            rfc1006Server.Started += Rfc1006Server_Started;
            rfc1006Server.StatusChanged += Rfc1006Server_StatusChanged;
            rfc1006Server.Start();
        }

        public static void Stop()
        {
            StopConnectionThread();
            if (rfc1006Server == null)
                return;
            rfc1006Server.Stop();
            rfc1006Server.Received -= Rfc1006Server_Received;
            rfc1006Server.Started -= Rfc1006Server_Started;
            rfc1006Server.StatusChanged -= Rfc1006Server_StatusChanged;
        }

        private static void Rfc1006Server_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            Log.InfoFormat("{0}, WMS Connection {1}", DateTime.Now.TimeOfDay, rfc1006Server.Status);
            if (rfc1006Server.Status != Rfc1006Status.Disconnected || !DoListen)
                return;
            StartConnectionThread();
        }

        private static void StartConnectionThread()
        {
            StopConnectionThread();
            DoListen = true;
            awaitingConnection = new Thread(AsyncConnect);
            awaitingConnection.Start();
        }

        private static void StopConnectionThread()
        {
            if (awaitingConnection == null)
                return;
            try
            {
                DoListen = false;
                if (!awaitingConnection.IsAlive || awaitingConnection.Join(1000))
                    return;
                awaitingConnection.Abort();
            }
            finally
            {
                awaitingConnection = null;
            }
        }

        private static void AsyncConnect()
        {
            if (!DoListen)
                return;
            rfc1006Server.Connect();
        }

        public static event EventHandler<StatusChangedEventArgs> OnWmsStatusChanged
        {
            add => rfc1006Server.StatusChanged += value;
            remove => rfc1006Server.StatusChanged -= value;
        }

        private static void Rfc1006Server_Started(object sender, ConnectionEventArgs e)
        {
            StartConnectionThread();
        }

        private static void Rfc1006Server_Received(object sender, TransferEventArgs e)
        {
            var message = BaseMessage.ByteArrayToMessage(e.Buffer);
            LastMessageReceivedTime = DateTime.Now;
            if (message.functionCode == 0U)
                return;
            var onReceived = OnReceived;
            if (onReceived == null)
                return;
            onReceived(message);
        }

        public static bool Send(byte[] buffer)
        {
            if (rfc1006Server.Status != Rfc1006Status.Connected || rfc1006Server.Transmit(buffer) != buffer.Length)
                return false;
            var array1 = buffer.Skip(4).Take(4).ToArray();
            var array2 = buffer.Skip(8).Take(4).ToArray();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(array1);
                Array.Reverse(array2);
            }

            BaseLocation.FindLocation(BitConverter.ToUInt32(array2, 0))?.Log(string.Format("Sent function code {0}", BitConverter.ToUInt32(array1, 0)));
            return true;
        }
    }
}