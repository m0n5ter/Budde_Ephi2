// Decompiled with JetBrains decompiler
// Type: PharmaProject.Entry
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System.Threading;
using Ephi.Core.Helping.Log4Net;
using Ephi.Core.UTC;
using PharmaProject.BusinessLogic.Devices;
using PharmaProject.BusinessLogic.Locations;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.Segments;
using PharmaProject.BusinessLogic.UTC;
using PharmaProject.BusinessLogic.Wms_Communication;
using PharmaProject.BusinessLogic.Wms_Communication.Messages;

namespace PharmaProject
{
    public class Entry
    {
        private volatile bool commitSuicide;
        private EmptyBoxInfeed emptyBoxInfeed;
        private LabelPressUTC labelPressUTC;
        public AutostoreEnterSlope location1;
        public PblZoneLocation location10;
        public PblHalfwayLocation location11;
        public PblZoneLocation location12;
        public PblEndLocation location13;
        public Pbl_B_EnterMainLocation location14;
        public SmallStorageLocation location15;
        public WeighingZone_Enter location16;
        public WeighingZone_Leave location17;
        public PackingBelowLocation location18;
        public PackingEnterSlopeLocation location19;
        public AutostoreLocation location2;
        public PackingLeaveSlopeLocation location20;
        public LabelPrinterLocation location21;
        public LabelPrinterRebound location22;
        public StrapperLocation location23;
        public PalletizingLocation location24;
        public PalletizingLocation location25;
        public PalletizingLocation location26;
        public AutostoreLeaveSlope location3;
        public MainTrackCrossingLocation_PblA location4;
        public PblZoneLocation location5;
        public PblHalfwayLocation location6;
        public PblZoneLocation location7;
        public PblEndLocationWithExtension location8;
        public MainTrackCrossingLocation_PblB location9;
        private LongStrechAfterAcm longStrechBeginPblA;
        private LongStrechAfterAcm longStrechBeginPblB;
        public PostPackingSlopeLocation postPackingSlope;
        private readonly byte[] störvektor = new byte[100];
        private readonly Thread watchDog;

        public Entry()
        {
            Log4NetHelpers.Init();
            InitLocations();
            BaseLocation.AssignWmsTimeouts();
            WmsCommunicator.OnReceived += ParseMessage;
            watchDog = new Thread(AsyncRunWatchdog);
        }

        public void Start()
        {
            Stop();
            WmsCommunicator.Start();
            NdwConnectCommunicator.Start();
            CSD_UTC.UtcServerConnect(503, "172.16.40.2");
            BarcodeScanner.ConnectAllScanners();
            commitSuicide = false;
            watchDog.Start();
        }

        public void Stop()
        {
            commitSuicide = true;
            if (watchDog.IsAlive && !watchDog.Join(500))
                watchDog.Abort();
            WmsCommunicator.Stop();
            NdwConnectCommunicator.Stop();
            CSD_UTC.UtcServerDisconnect();
            BarcodeScanner.DisconnectAllScanners();
        }

        private void ParseMessage(BaseMessage message)
        {
            if (message == null)
                return;
            switch ((FUNCTION_CODES)message.functionCode)
            {
                case FUNCTION_CODES.ANFORDERUNG_PACKMITTEL:
                    if (message.location != 110U)
                        break;
                    emptyBoxInfeed.StartSequence((message as AnforderungPackmittel).PackageID);
                    break;
                case FUNCTION_CODES.IPUNKT_AUSSCHALTEN:
                    if (message.location != 110U)
                        break;
                    emptyBoxInfeed.StopSequence();
                    break;
                case FUNCTION_CODES.ANFORDERUNG_STÖRVEKTOR:
                    WmsCommunicator.Send(BaseMessage.MessageToByteArray(new Störvektor(störvektor)));
                    break;
                default:
                    if (message.functionCode == 2U && message.location == 111U)
                    {
                        emptyBoxInfeed.WeighingDone((message as AnweisungPackstück).Direction);
                        break;
                    }

                    var location = BaseLocation.FindLocation(message.location);
                    if (location == null)
                        break;
                    location.Log(string.Format("Received function code {0}", message.functionCode));
                    location.ProcessRxMessage(message);
                    break;
            }
        }

        private void InitLocations()
        {
            var sharedSlopeControl1 = new SharedSlopeControl();
            location1 = new AutostoreEnterSlope("172.16.40.40", 1U, sharedSlopeControl1);
            location2 = new AutostoreLocation("172.16.40.41", 2U);
            var slopeControl1 = new SharedSlopeControl();
            location3 = new AutostoreLeaveSlope("172.16.40.42", 3U, slopeControl1);
            location4 = new MainTrackCrossingLocation_PblA("172.16.40.43", 4U, "172.16.40.70", "172.16.40.71", "172.16.40.108", slopeControl1);
            emptyBoxInfeed = new EmptyBoxInfeed(location4.MakeIn(PIN._14).LowActive, location4.MakeIn(PIN._16), location4.MakeOut(PIN._15), location4.MakeOut(PIN._18), location4.MakeOut(PIN._13),
                "172.16.40.107");
            location5 = new PblZoneLocation("172.16.40.44", 5U, "172.16.40.72", "172.16.40.73");
            longStrechBeginPblA = new LongStrechAfterAcm(location5.MakeIn(PIN._24), location5.GetScripts(2U).LoadTriggerNormal, location4.MakeIn(PIN._22).LowActive, location5.MakeIn(PIN._23),
                location5.MakeOut(PIN._18), location4.MakeOut(PIN._16), "PBL A Infeed");
            location6 = new PblHalfwayLocation("172.16.40.45", 6U, "172.16.40.74", "172.16.40.75");
            location7 = new PblZoneLocation("172.16.40.46", 7U, "172.16.40.76", "172.16.40.77");
            location8 = new PblEndLocationWithExtension("172.16.40.47", "172.16.40.104", 8U);
            CSD_UTC.GroupSoftEmergency(location5, location6, location7, location8);
            location5.LongSeg = new LongStretch(location6.MakeIn(PIN._24), location6.GetScripts(1U).LoadTriggerNormal, location6.MakeIn(PIN._21), location5.GetScripts(2U).DownstreamStartLoading,
                "PBL A 1", 300U);
            location6.LongSeg = new LongStretch(location7.MakeIn(PIN._24), location7.GetScripts(2U).LoadTriggerNormal, location7.MakeIn(PIN._23), location6.GetScripts(1U).DownstreamStartLoading,
                "PBL A 2", 1000U);
            location7.LongSeg = new LongStretch(location8.MakeIn(PIN._24), location8.GetScripts(1U).LoadTriggerNormal, location8.MakeIn(PIN._23), location7.GetScripts(2U).DownstreamStartLoading,
                "PBL A 3", 300U);
            location9 = new MainTrackCrossingLocation_PblB("172.16.40.48", 9U, "172.16.40.80", "172.16.40.81", "172.16.40.78", "172.16.40.79");
            location10 = new PblZoneLocation("172.16.40.49", 10U, "172.16.40.82", "172.16.40.83");
            longStrechBeginPblB = new LongStrechAfterAcm(location10.MakeIn(PIN._24), location10.GetScripts(2U).LoadTriggerNormal, location9.MakeIn(PIN._24).LowActive, location10.MakeIn(PIN._23),
                location10.MakeOut(PIN._18), location9.MakeOut(PIN._18), "PBL A Infeed");
            location11 = new PblHalfwayLocation("172.16.40.50", 11U, "172.16.40.84", "172.16.40.85");
            location12 = new PblZoneLocation("172.16.40.51", 12U, "172.16.40.86", "172.16.40.87");
            location13 = new PblEndLocation("172.16.40.52", 13U);
            CSD_UTC.GroupSoftEmergency(location10, location11, location12, location13);
            location10.LongSeg = new LongStretch(location11.MakeIn(PIN._24), location11.GetScripts(2U).LoadTriggerNormal, location11.MakeIn(PIN._21), location10.GetScripts(2U).DownstreamStartLoading,
                "PBL B 1", 300U);
            location11.LongSeg = new LongStretch(location12.MakeIn(PIN._24), location12.GetScripts(2U).LoadTriggerNormal, location12.MakeIn(PIN._23), location11.GetScripts(1U).DownstreamStartLoading,
                "PBL B 2", 1000U);
            location12.LongSeg = new LongStretch(location13.MakeIn(PIN._24), location13.GetScripts(1U).LoadTriggerNormal, location13.MakeIn(PIN._23), location12.GetScripts(2U).DownstreamStartLoading,
                "PBL B 3", 300U);
            location14 = new Pbl_B_EnterMainLocation("172.16.40.53", 14U);
            location15 = new SmallStorageLocation("172.16.40.54", "172.16.40.55", 15U, "172.16.40.88", "172.16.40.89");
            var slopeControl2 = new SharedSlopeControl();
            location16 = new WeighingZone_Enter("172.16.40.56", 16U, "172.16.40.90", "172.16.40.91");
            location17 = new WeighingZone_Leave("172.16.40.57", 17U, "172.16.40.105", "172.16.40.106", slopeControl2);
            CSD_UTC.GroupSoftEmergency(location16, location17);
            location18 = new PackingBelowLocation("172.16.40.60", 18U);
            location19 = new PackingEnterSlopeLocation("172.16.40.58", 19U, "172.16.40.92", "172.16.40.93", slopeControl2);
            var sharedSlopeControl2 = new SharedSlopeControl();
            location20 = new PackingLeaveSlopeLocation("172.16.40.59", 20U, "172.16.40.94", "172.16.40.95", sharedSlopeControl2);
            labelPressUTC = new LabelPressUTC("172.16.40.66");
            location21 = new LabelPrinterLocation("172.16.40.61", 21U, "172.16.40.96", "172.16.40.97", labelPressUTC.labelPress1, labelPressUTC.labelPress2);
            location22 = new LabelPrinterRebound("172.16.40.62", 22U, "172.16.40.98", "172.16.40.99", "172.16.40.100");
            location21.HookupRebound(location22);
            location23 = new StrapperLocation("172.16.40.68", 23U, "172.16.40.101");
            CSD_UTC.GroupSoftEmergency(location18, location19, location20);
            postPackingSlope = new PostPackingSlopeLocation("172.16.40.67", sharedSlopeControl2, sharedSlopeControl1);
            CSD_UTC.GroupSoftEmergency(location21, location22);
            location24 = new PalletizingLocation("172.16.40.63", "172.16.40.102", 24U);
            location25 = new PalletizingLocation("172.16.40.64", "172.16.40.103", 25U);
            location26 = new PalletizingLocation("172.16.40.65", "172.16.40.109", 26U, DESTINATION.DISPATCH_CSD2_ALT);
        }

        private void AsyncRunWatchdog()
        {
            while (!commitSuicide)
            {
                for (var index = 0; index < 10; ++index)
                {
                    if (commitSuicide)
                        return;
                    Thread.Sleep(100);
                }

                CSD.RunWatchdog();
            }
        }
    }
}