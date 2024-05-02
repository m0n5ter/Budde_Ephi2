// Decompiled with JetBrains decompiler
// Type: PharmaProject.PBL
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using Ephi.Core.UTC;
using PharmaProject.Locations;
using PharmaProject.Segments;
using PharmaProject.UTC;

namespace PharmaProject
{
    public class PBL
    {
        private readonly LabelPressUTC labelPressUTC;
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
        public MainToPblLocation location4;
        public PblZoneLocation location5;
        public PblHalfwayLocation location6;
        public PblZoneLocation location7;
        public PblEndLocation location8;
        public MainTrackCrossingLocation location9;
        private LongStrechAfterAcm longStrechBeginPblA;
        private LongStrechAfterAcm longStrechBeginPblB;
        public PostPackingSlopeLocation postPackingSlope;

        public PBL()
        {
            var sharedSlopeControl1 = new SharedSlopeControl();
            location1 = new AutostoreEnterSlope("172.16.0.10", 1U, sharedSlopeControl1);
            location2 = new AutostoreLocation("172.16.0.11", 2U);
            var slopeControl1 = new SharedSlopeControl();
            location3 = new AutostoreLeaveSlope("172.16.0.12", 3U, slopeControl1);
            location4 = new MainToPblLocation("172.16.0.13", 4U, "172.16.0.50", "172.16.0.51", slopeControl1);
            location4.DoLog = true;
            location5 = new PblZoneLocation("172.16.0.14", 5U, "172.16.0.52", "172.16.0.53");
            longStrechBeginPblA = new LongStrechAfterAcm(location5.MakeIn(PIN._24), location5.GetScripts(2U).LoadTriggerNormal, location4.MakeIn(PIN._22).LowActive, location5.MakeIn(PIN._23),
                location5.MakeOut(PIN._18), location4.MakeOut(PIN._16));
            location6 = new PblHalfwayLocation("172.16.0.15", 6U, "172.16.0.54", "172.16.0.55");
            location7 = new PblZoneLocation("172.16.0.16", 7U, "172.16.0.56", "172.16.0.57");
            location8 = new PblEndLocation("172.16.0.17", 8U);
            CSD_UTC.GroupSoftEmergency(location5, location6, location7, location8);
            location5.LongSeg = new LongStretch(location6.MakeIn(PIN._24), location6.GetScripts(1U).LoadTriggerNormal, location6.MakeIn(PIN._21), location5.GetScripts(2U).DownstreamStartLoading,
                300U);
            location6.LongSeg = new LongStretch(location7.MakeIn(PIN._24), location7.GetScripts(2U).LoadTriggerNormal, location7.MakeIn(PIN._23), location6.GetScripts(1U).DownstreamStartLoading,
                1000U);
            location7.LongSeg = new LongStretch(location8.MakeIn(PIN._24), location8.GetScripts(1U).LoadTriggerNormal, location8.MakeIn(PIN._23), location7.GetScripts(2U).DownstreamStartLoading,
                300U);
            location9 = new MainTrackCrossingLocation("172.16.0.18", 9U, "172.16.0.60", "172.16.0.61", "172.16.0.58", "172.16.0.59");
            location10 = new PblZoneLocation("172.16.0.19", 10U, "172.16.0.62", "172.16.0.63");
            longStrechBeginPblB = new LongStrechAfterAcm(location10.MakeIn(PIN._24), location10.GetScripts(2U).LoadTriggerNormal, location9.MakeIn(PIN._24).LowActive, location10.MakeIn(PIN._23),
                location10.MakeOut(PIN._18), location9.MakeOut(PIN._18));
            location11 = new PblHalfwayLocation("172.16.0.20", 11U, "172.16.0.64", "172.16.0.65");
            location12 = new PblZoneLocation("172.16.0.21", 12U, "172.16.0.66", "172.16.0.67");
            location13 = new PblEndLocation("172.16.0.22", 13U);
            CSD_UTC.GroupSoftEmergency(location10, location11, location12, location13);
            location10.LongSeg = new LongStretch(location11.MakeIn(PIN._24), location11.GetScripts(2U).LoadTriggerNormal, location11.MakeIn(PIN._21), location10.GetScripts(2U).DownstreamStartLoading,
                300U);
            location11.LongSeg = new LongStretch(location12.MakeIn(PIN._24), location12.GetScripts(2U).LoadTriggerNormal, location12.MakeIn(PIN._23), location11.GetScripts(1U).DownstreamStartLoading,
                1000U);
            location12.LongSeg = new LongStretch(location13.MakeIn(PIN._24), location13.GetScripts(1U).LoadTriggerNormal, location13.MakeIn(PIN._23), location12.GetScripts(2U).DownstreamStartLoading,
                300U);
            location14 = new Pbl_B_EnterMainLocation("172.16.0.23", 14U);
            location15 = new SmallStorageLocation("172.16.0.24", "172.16.0.25", 15U, "172.16.0.68", "172.16.0.69");
            var slopeControl2 = new SharedSlopeControl();
            location16 = new WeighingZone_Enter("172.16.0.26", 16U, "172.16.0.70", "172.16.0.71");
            location17 = new WeighingZone_Leave("172.16.0.27", 17U, "172.16.0.85", "172.16.0.86", slopeControl2);
            CSD_UTC.GroupSoftEmergency(location16, location17);
            location18 = new PackingBelowLocation("172.16.0.30", 18U);
            location19 = new PackingEnterSlopeLocation("172.16.0.28", 19U, "172.16.0.72", "172.16.0.73", slopeControl2);
            var sharedSlopeControl2 = new SharedSlopeControl();
            location20 = new PackingLeaveSlopeLocation("172.16.0.29", 20U, "172.16.0.74", "172.16.0.75", sharedSlopeControl2);
            labelPressUTC = new LabelPressUTC("172.16.0.36");
            location21 = new LabelPrinterLocation("172.16.0.31", 21U, "172.16.0.76", "172.16.0.77", labelPressUTC.labelPress1, labelPressUTC.labelPress2);
            location22 = new LabelPrinterRebound("172.16.0.32", 22U, location21.MakeIn(PIN._23), location21.MakeOut(PIN._22), "172.16.0.78", "172.16.0.79", "172.16.0.80");
            location22.CsdEndRebound = location21.ReboundEndCsd;
            location21.ExitBarcodeTrigger = location22.MakeOut(PIN._24).HighActive;
            location23 = new StrapperLocation("172.16.0.38", 23U, "172.16.0.81");
            CSD_UTC.GroupSoftEmergency(location18, location19, location21);
            postPackingSlope = new PostPackingSlopeLocation("172.16.0.37", sharedSlopeControl2, sharedSlopeControl1);
            CSD_UTC.GroupSoftEmergency(location21, location22);
            location24 = new PalletizingLocation("172.16.0.33", "172.16.0.82", 24U);
            location25 = new PalletizingLocation("172.16.0.34", "172.16.0.83", 25U);
            location26 = new PalletizingLocation("172.16.0.35", "172.16.0.84", 26U);
        }
    }
}