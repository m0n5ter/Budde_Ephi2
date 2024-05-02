// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.MainToPblLocation
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using System.Net;
using System.Text;
using Ephi.Core.Helping;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.Segments;
using PharmaProject.Wms_Communication;
using PharmaProject.Wms_Communication.Messages;

namespace PharmaProject.Locations
{
    public class MainToPblLocation : BaseLocation
    {
        private const uint DISP_START_DELAY_ms = 4000;
        private const uint SLOPE_SLEEP_AFTER_SEC = 30;
        private bool acmFull;
        private DateTime acmFullSet = DateTime.Now;
        private Conditional CrossOver;
        private bool csd2LoadNormalPending;
        private Conditional csd2QuickDispatch;
        private readonly DelayedEvent DispatchDelayedStart;
        private InPin inBtn;
        private InPin inMainOcc1;
        private InPin inMainOcc2;
        private OutPin outMainAcmLoad;
        private OutPin outSlopeRun1;
        private OutPin outSlopeRun2;
        private bool slopeRun;
        private readonly DelayedEvent SlopeSleep;
        private string weighingBarcode = string.Empty;

        public MainToPblLocation(
            string IP,
            uint locationNumber,
            string csd1BS1Ip,
            string csd1BS2Ip,
            SharedSlopeControl slopeControl)
            : base(IP, locationNumber, 2U)
        {
            AddBarcodeScanner(new BarcodeScanner(string.Format("csd2BS1 Loc:{0}", locationNumber), IPAddress.Parse(csd1BS1Ip)));
            AddBarcodeScanner(new BarcodeScanner(string.Format("csd2BS2 Loc:{0}", locationNumber), IPAddress.Parse(csd1BS2Ip)));
            slopeControl.CanTransferPtr = SlopeLoadAllowed;
            SlopeSleep = new DelayedEvent(TimeSpan.FromSeconds(30.0), () => SlopeRun = false);
            DispatchDelayedStart = new DelayedEvent(TimeSpan.FromMilliseconds(4000.0), slopeControl.CanTransferChanged);
        }

        public bool SlopeRun
        {
            get => slopeRun;
            set
            {
                if (slopeRun == value)
                    return;
                Log(string.Format("Slope {0}", value ? "start" : (object)"stop"));
                slopeRun = value;
                if (slopeRun)
                {
                    SlopeSleep.Start();
                    outSlopeRun1.Activate();
                    outSlopeRun2.Activate();
                    outMainAcmLoad.Activate();
                }
                else
                {
                    SlopeSleep.Stop();
                    outSlopeRun1.Deactivate();
                    outSlopeRun2.Deactivate();
                    outMainAcmLoad.Deactivate();
                }
            }
        }

        private TimeSpan AcmFullAge => DateTime.Now.Subtract(acmFullSet);

        public bool AcmFull
        {
            get => acmFull;
            set
            {
                if (acmFull == value)
                    return;
                acmFull = value;
                acmFullSet = DateTime.Now;
                if (acmFull)
                {
                    DispatchDelayedStart.Stop();
                    SlopeRun = false;
                }
                else
                {
                    DispatchDelayedStart.Start();
                    SlopeRun = true;
                }
            }
        }

        protected override InPin[] ResetEmergencyPins => new InPin[1] { inBtn };

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._24);
            inMainOcc1 = MakeIn(PIN._23).LowActive;
            inMainOcc2 = MakeIn(PIN._19).LowActive;
            outSlopeRun1 = MakeOut(PIN._22);
            outSlopeRun2 = MakeOut(PIN._24);
            outMainAcmLoad = MakeOut(PIN._17);
            var scripts = GetScripts(2U);
            scripts.DownstreamStartLoading = MakeOut(PIN._21);
            scripts.DispatchNormalSegmentOccupied = MakeIn(PIN._15).LowActive;
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            Conditional conditional = MakeConditionalStatement(string.Format("Auto load precondition CSD:{0}, Loc:{1}", 1, LocationNumber), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                .MakePrecondition().AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts1.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts1.RollersRun, PIN_STATE.INACTIVE)
                .AddCondition(scripts1.LiftRun, PIN_STATE.INACTIVE).AddCondition(scripts1.OccupiedBelts, PIN_STATE.INACTIVE).AddCondition(scripts1.OccupiedRollers, PIN_STATE.INACTIVE)
                .AddCondition(scripts1.LoadTriggerNormal).CloseBlock();
            CrossOver = MakeConditionalMacro("quick load from CSD1 to CSD2").AddStatement(scripts1.LoadNormal)
                .AddStatement(MakeConditionalBatch("load from CSD1 to CSD2").AddStatement(scripts1.DispatchAlternative).AddStatement(scripts2.LoadAlternative));
            MakeConditionalMacro("Test sequence for loop testing", RUN_MODE.PERMANENTLY).AddStatement(conditional).AddStatement(scripts1.LoadNormal);
            csd2QuickDispatch = MakeConditionalMacro("CSD 2 Quick Dispatch").AddStatement(scripts2.LoadAlternative).AddStatement(scripts2.DispatchNormal);
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            inMainOcc1.OnStateChanged += InMainOcc_OnStateChanged;
            inMainOcc2.OnStateChanged += InMainOcc_OnStateChanged;
            csd1.OnStateChanged += Csd1_OnStateChanged;
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (csdNum == 2U)
                return MakeConditionalBatch("Move rollers with dispatch")
                    .AddStatement(MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.RollersRun, scripts.OccupiedRollers, csdNum,
                        nextSegLoad: scripts.DownstreamStartLoading)).AddStatement(MakeConditionalStatement("Turn on rollers", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                        .AddCondition(scripts.OccupiedRollers, PIN_STATE.INACTIVE).AddGlobalTimeout(5000U).AddOutputState(scripts.RollersRun));
            return scripts != null
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.RollersRun, scripts.OccupiedRollers, csdNum,
                    nextSegLoad: scripts.DownstreamStartLoading)
                : null;
        }

        protected override Conditional DispatchAlternativeScript(uint csdNum)
        {
            if (csdNum != 1U)
                return null;
            var scripts = GetScripts(csdNum);
            return MakeConditionalBatch("Turn on belts same time as lift up")
                .AddStatement(MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.BeltsRun, scripts.OccupiedRollers, csdNum,
                    middleMotorRun: scripts.MiddleRollersRun)).AddStatement(MakeConditionalStatement("Turn on belts", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                    .AddCondition(scripts.OccupiedRollers, PIN_STATE.INACTIVE).AddGlobalTimeout(5000U).AddOutputState(scripts.BeltsRun).AddOutputState(scripts.BeltsDir)
                    .AddOutputState(scripts.MiddleRollersRun));
        }

        protected override Conditional LoadAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 2U
                ? MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedBelts, csdNum, endDelay: 300U, middleMotorRun: scripts.MiddleRollersRun)
                : null;
        }

        protected override void OnBarcodeScanned(string barcode)
        {
            if (!SendAllowed)
                return;
            base.OnBarcodeScanned(barcode);
            if (csd1.Route == null)
                return;
            csd1.Route.Barcode = barcode;
        }

        private void Csd1_OnStateChanged(CSD csd)
        {
            switch (csd1.State)
            {
                case CSD_STATE.IDLE:
                    if (csd1.Route != null && !csd1.Route.Barcode.Equals(""))
                        DispatchWmsFeedback(csd1.Route);
                    csd1.Route = null;
                    break;
                case CSD_STATE.LOADING:
                    if (csd1.Route != null)
                        break;
                    csd1.Route = new Route(START.LOAD_CSD1);
                    break;
            }
        }

        private bool SlopeLoadAllowed()
        {
            if (AcmFull || AcmFullAge < TimeSpan.FromMilliseconds(4000.0))
                return false;
            SlopeRun = true;
            SlopeSleep.Start();
            return true;
        }

        private void InMainOcc_OnStateChanged(InPin obj)
        {
            AcmFull = inMainOcc1.Active && inMainOcc2.Active;
        }

        public override void DoEvaluate()
        {
            if (csd1.State == CSD_STATE.OCCUPIED)
            {
                if (csd1.Route == null)
                    Log(">>>>>>>    csd1.Route == null   <<<<<<<<<<<");
                var route = csd1.Route;
                var to = route != null ? route.Destination : DESTINATION.DISPATCH_CSD1;
                if (to == DESTINATION.TBD)
                    to = DESTINATION.DISPATCH_CSD1;
                Dispatch(to);
            }

            switch (csd2.State)
            {
                case CSD_STATE.IDLE:
                    if (csd2LoadNormalPending && !weighingBarcode.Equals(string.Empty))
                    {
                        WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(WMS_TOTE_DIRECTION.DIRECTION_2, Encoding.ASCII.GetBytes(weighingBarcode), LocationNumber)));
                        NdwConnectCommunicator.DirectionSentUpdate(LocationNumber, weighingBarcode, WMS_TOTE_DIRECTION.DIRECTION_2);
                        weighingBarcode = string.Empty;
                    }

                    if (!csd2LoadNormalPending)
                        break;
                    if (csd2.PassThrough())
                    {
                        csd2LoadNormalPending = false;
                        break;
                    }

                    if (!csd2LoadNormalPending || !csd2.LoadNormal())
                        break;
                    csd2LoadNormalPending = false;
                    break;
                case CSD_STATE.LOADING:
                    if (GetScripts(2U).DispatchNormalSegmentOccupied.Active)
                        break;
                    csd2QuickDispatch.Run();
                    break;
                case CSD_STATE.OCCUPIED:
                    if (GetScripts(2U).DispatchNormalSegmentOccupied.Active)
                        break;
                    csd2.DispatchNormal();
                    break;
            }
        }

        private void Dispatch(DESTINATION to)
        {
            var csd1 = this.csd1;
            if (csd1.Route != null)
                csd1.Route.Destination = to;
            if (!Helpers.Contains(csd1.State, CSD_STATE.LOADING_PENDING, CSD_STATE.LOADING, CSD_STATE.OCCUPIED))
                return;
            if (to != DESTINATION.DISPATCH_CSD1)
            {
                if (to != DESTINATION.DISPATCH_CSD2)
                {
                    if (to == DESTINATION.TBD)
                        ;
                    throw new ArgumentException("Illegal dispatch destination");
                }

                if (!csd2.IsIdle || !CrossOver.Run())
                    return;
                csd2.ForceLoadPending();
                if (this.csd1.State != CSD_STATE.OCCUPIED)
                    return;
                this.csd1.ForceDispatchPending();
            }
            else
            {
                if (!csd1.Scripts.DispatchNormalSegmentOccupied.Inactive)
                    return;
                if (csd1.State == CSD_STATE.LOADING)
                {
                    csd1.PassThrough();
                }
                else
                {
                    if (this.csd1.State != CSD_STATE.OCCUPIED)
                        return;
                    csd1.DispatchNormal();
                }
            }
        }

        protected override void DispatchWmsFeedback(Route route)
        {
            if (route == null)
                return;
            var direction = WMS_TOTE_DIRECTION.DIRECTION_3;
            switch (route.Destination)
            {
                case DESTINATION.DISPATCH_CSD1:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_1;
                    break;
                case DESTINATION.DISPATCH_CSD2:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_2;
                    break;
            }

            if (direction == WMS_TOTE_DIRECTION.DIRECTION_3 || route.Barcode == null)
                return;
            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(direction, Encoding.ASCII.GetBytes(route.Barcode), LocationNumber)));
            NdwConnectCommunicator.DirectionSentUpdate(LocationNumber, route.Barcode, direction);
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            if (value1 == 0U && csd1.Route != null)
            {
                if (target == WMS_TOTE_DIRECTION.DIRECTION_1 || target != WMS_TOTE_DIRECTION.DIRECTION_2)
                {
                    csd1.Route.Destination = DESTINATION.DISPATCH_CSD1;
                    Dispatch(csd1.Route.Destination);
                }
                else
                {
                    csd1.Route.Destination = DESTINATION.DISPATCH_CSD2;
                    Dispatch(csd1.Route.Destination);
                }
            }
            else
            {
                if (value1 <= 0U)
                    return;
                weighingBarcode = barcode;
                csd2LoadNormalPending = true;
            }
        }
    }
}