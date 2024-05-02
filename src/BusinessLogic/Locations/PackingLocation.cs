// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Locations.PackingLocation
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using System.Net;
using System.Text;
using Ephi.Core.Helping;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Devices;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.Segments;
using PharmaProject.BusinessLogic.Wms_Communication;
using PharmaProject.BusinessLogic.Wms_Communication.Messages;

namespace PharmaProject.BusinessLogic.Locations
{
    public class PackingLocation : BaseLocation
    {
        private const uint CSD2_LOAD_DEBOUNCE = 2500;
        private Conditional csd1AutoLoad;
        private Conditional csd1DispatchLeft;
        private Conditional csd1DispatchRight;
        private Conditional csd2DispatchLeft;
        private Conditional csd2DispatchRight;
        private readonly PackingStationIoSegment ioSegment;
        private OutPin ioSegmentMotor;
        private InPin ioSegmentSensor;
        private readonly DelayedEvent reEvaluateCsd2Load;

        public PackingLocation(string IP, uint locationNumber, string bs1IP, string bs2IP)
            : base(IP, locationNumber, 2U)
        {
            AddBarcodeScanner(new BarcodeScanner(string.Format("Loc:{0} Barcodescanner:{1}", locationNumber, 1), IPAddress.Parse(bs1IP)));
            AddBarcodeScanner(new BarcodeScanner(string.Format("Loc:{0} Barcodescanner:{1}", locationNumber, 2), IPAddress.Parse(bs2IP)));
            ioSegment = new PackingStationIoSegment(ioSegmentSensor, ioSegmentMotor);
            ioSegment.SegmentChanged += IoSegment_SegmentChanged;
            reEvaluateCsd2Load = new DelayedEvent(2500U, Evaluate);
            csd2.OnStateChanged += Csd2_OnStateChanged;
        }

        private void Csd2_OnStateChanged(CSD csd)
        {
            if (csd.State != SEGMENT_STATE.LOADING)
                return;
            reEvaluateCsd2Load.Start();
        }

        protected override void InitPins()
        {
            base.InitPins();
            ioSegmentSensor = MakeIn(PIN._10).LowActive;
            ioSegmentMotor = MakeOut(PIN._23);
            var scripts1 = GetScripts(1U);
            scripts1.UpstreamStartDispatching = MakeOut(PIN._20);
            scripts1.LoadTriggerNormal = MakeIn(PIN._21).LowActive;
            scripts1.MiddleRollersDir = OutPin.Dummy;
            var scripts2 = GetScripts(2U);
            scripts2.DownstreamStartLoading = MakeOut(PIN._21);
            scripts2.DispatchNormalSegmentOccupied = MakeIn(PIN._22).LowActive;
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var scripts = GetScripts(1U);
            GetScripts(2U);
            Conditional conditional = MakeConditionalStatement(string.Format("Auto load precondition CSD:{0}, Loc:{1}", 1, LocationNumber), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                .MakePrecondition().AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts.RollersRun, PIN_STATE.INACTIVE)
                .AddCondition(scripts.LiftRun, PIN_STATE.INACTIVE).AddCondition(scripts.OccupiedBelts, PIN_STATE.INACTIVE).AddCondition(scripts.OccupiedRollers, PIN_STATE.INACTIVE)
                .AddCondition(scripts.LoadTriggerNormal).CloseBlock();
            csd1AutoLoad = MakeConditionalMacro(string.Format("Auto load CSD:1, Loc:{0}", LocationNumber)).AddStatement(conditional).AddStatement(scripts.LoadNormal);
        }

        protected override void InitCsdScrips(uint csdNum)
        {
            base.InitCsdScrips(csdNum);
            var scripts = GetScripts(csdNum);
            if (scripts == null)
                return;
            var conditional1 = MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, null, scripts.OccupiedBelts, csdNum,
                extraActiveNotFinished: scripts.OccupiedRollers, endDelay: 2000U, middleMotorRun: scripts.RollersRun);
            var conditional2 = MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, null, scripts.OccupiedBelts, csdNum,
                extraActiveNotFinished: scripts.OccupiedRollers, endDelay: 2000U, middleMotorRun: scripts.RollersRun);
            if (csdNum != 1U)
            {
                if (csdNum != 2U)
                    return;
                csd2DispatchRight = conditional1;
                csd2DispatchLeft = conditional2;
            }
            else
            {
                csd1DispatchRight = conditional1;
                csd1DispatchLeft = conditional2;
            }
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (scripts == null)
                return null;
            if (csdNum == 1U)
                return MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, scripts.OccupiedBelts,
                    prevSegDispatch: scripts.UpstreamStartDispatching);
            return csdNum == 2U
                ? MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, scripts.OccupiedBelts, 6500U,
                    middleMotorRun: scripts.MiddleRollersRun)
                : null;
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (csdNum == 1U)
                return MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.RollersRun, scripts.OccupiedRollers, csdNum,
                    extraActiveNotFinished: scripts.OccupiedBelts);
            return csdNum == 2U
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.RollersRun, scripts.OccupiedRollers, csdNum,
                    extraActiveNotFinished: scripts.OccupiedBelts, nextSegLoad: scripts.DownstreamStartLoading)
                : null;
        }

        protected override void AttachCsdEventHandlers(CSD csd)
        {
            base.AttachCsdEventHandlers(csd);
            csd1DispatchRight.OnStateChanged += csd1.Dispatch_OnStateChanged;
            csd1DispatchLeft.OnStateChanged += csd1.Dispatch_OnStateChanged;
            csd2DispatchRight.OnStateChanged += csd2.Dispatch_OnStateChanged;
            csd2DispatchLeft.OnStateChanged += csd2.Dispatch_OnStateChanged;
        }

        protected override void OnBarcodeScanned(string barcode)
        {
            base.OnBarcodeScanned(barcode);
            if (csd1.Route == null)
                csd1.Route = new Route(START.LOAD_CSD1);
            csd1.Route.Barcode = barcode;
        }

        protected override void DispatchWmsFeedback(Route route)
        {
            if (route == null || route.Barcode == null || route.Barcode.Equals(string.Empty))
                return;
            WMS_TOTE_DIRECTION direction;
            switch (route.Destination)
            {
                case DESTINATION.DISPATCH_CSD1_ALT:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_2;
                    break;
                case DESTINATION.DISPATCH_CSD1_ALT2:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_3;
                    break;
                case DESTINATION.DISPATCH_CSD2_ALT:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_4;
                    break;
                case DESTINATION.DISPATCH_CSD2_ALT2:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_5;
                    break;
                default:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_1;
                    break;
            }

            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(direction, Encoding.ASCII.GetBytes(route.Barcode), LocationNumber)));
            NdwConnectCommunicator.DirectionSentUpdate(LocationNumber, route.Barcode, direction);
        }

        private void IoSegment_SegmentChanged(PackingStationIoSegment obj)
        {
            Evaluate();
        }

        public override void DoEvaluate()
        {
            HandleCsd1();
            HandleIoSegment();
            HandleCsd2();
        }

        private void HandleIoSegment()
        {
            switch (ioSegment.State)
            {
                case SEGMENT_STATE.LOADING:
                case SEGMENT_STATE.OCCUPIED:
                    if (!csd2.IsIdle || !csd2.LoadNormal() || !ioSegment.Passthrough())
                        break;
                    csd2.Route = ioSegment.Route;
                    ioSegment.Route = null;
                    break;
            }
        }

        private void HandleCsd1()
        {
            var route = csd1.Route;
            switch (csd1.State)
            {
                case SEGMENT_STATE.IDLE:
                    csd1AutoLoad.Run();
                    if (route != null && (route.Destination == DESTINATION.DISPATCH_CSD1_ALT || route.Destination == DESTINATION.DISPATCH_CSD1_ALT2))
                        DispatchWmsFeedback(route);
                    csd1.Route = null;
                    break;
                case SEGMENT_STATE.LOADING_PENDING:
                case SEGMENT_STATE.LOADING:
                    if (route != null)
                        break;
                    csd1.Route = new Route(START.LOAD_CSD1);
                    break;
                case SEGMENT_STATE.OCCUPIED:
                    var def = DESTINATION.DISPATCH_CSD1;
                    if (!GetCsdDispatchDestination(csd1, ref def))
                        break;
                    DispatchCSD1(def);
                    break;
            }
        }

        private void HandleCsd2()
        {
            var route = csd2.Route;
            switch (csd2.State)
            {
                case SEGMENT_STATE.IDLE:
                    if (!string.IsNullOrEmpty(route?.Barcode))
                        DispatchWmsFeedback(route);
                    csd2.Route = null;
                    break;
                case SEGMENT_STATE.LOADING:
                    if (Helpers.Contains((DESTINATION)(route != null ? (int)route.Destination : 4), DESTINATION.DISPATCH_CSD2_ALT, DESTINATION.DISPATCH_CSD2_ALT2) ||
                        csd2.StateAge < TimeSpan.FromMilliseconds(2500.0))
                        break;
                    DispatchCSD2(route.Destination);
                    break;
                case SEGMENT_STATE.OCCUPIED:
                    DispatchCSD2(route != null ? route.Destination : DESTINATION.TBD);
                    break;
            }
        }

        private void DispatchCSD1(DESTINATION to)
        {
            switch (to)
            {
                case DESTINATION.DISPATCH_CSD1_ALT:
                    if (!csd1.IsOccupied || !csd1DispatchLeft.Run())
                        break;
                    csd1.ForceDispatchPending();
                    break;
                case DESTINATION.DISPATCH_CSD1_ALT2:
                    if (!csd1.IsOccupied || !csd1DispatchRight.Run())
                        break;
                    csd1.ForceDispatchPending();
                    break;
                default:
                    if (ioSegment.State != SEGMENT_STATE.IDLE || !ioSegment.Load() || (!csd1.DispatchNormal() && !csd1.PassThrough()))
                        break;
                    ioSegment.Route = csd1.Route;
                    csd1.Route = null;
                    break;
            }
        }

        private void DispatchCSD2(DESTINATION to)
        {
            switch (to)
            {
                case DESTINATION.DISPATCH_CSD2_ALT:
                    if (!csd2.IsOccupied || !csd2DispatchLeft.Run())
                        break;
                    csd2.ForceDispatchPending();
                    break;
                case DESTINATION.DISPATCH_CSD2_ALT2:
                    if (!csd2.IsOccupied || !csd2DispatchRight.Run())
                        break;
                    csd2.ForceDispatchPending();
                    break;
                default:
                    if (!csd2.Scripts.DispatchNormalSegmentOccupied.Inactive)
                        break;
                    if (csd2.Route != null)
                        csd2.Route.Destination = DESTINATION.DISPATCH_CSD2;
                    if (csd2.PassThrough())
                        break;
                    csd2.DispatchNormal();
                    break;
            }
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            if (string.IsNullOrEmpty(barcode) || !barcode.Equals(csd1.Route?.Barcode))
                return;
            switch (target)
            {
                case WMS_TOTE_DIRECTION.DIRECTION_1:
                    csd1.Route.Destination = DESTINATION.DISPATCH_CSD2;
                    break;
                case WMS_TOTE_DIRECTION.DIRECTION_2:
                    csd1.Route.Destination = DESTINATION.DISPATCH_CSD1_ALT;
                    break;
                case WMS_TOTE_DIRECTION.DIRECTION_3:
                    csd1.Route.Destination = DESTINATION.DISPATCH_CSD1_ALT2;
                    break;
                case WMS_TOTE_DIRECTION.DIRECTION_4:
                    csd1.Route.Destination = DESTINATION.DISPATCH_CSD2_ALT;
                    break;
                case WMS_TOTE_DIRECTION.DIRECTION_5:
                    csd1.Route.Destination = DESTINATION.DISPATCH_CSD2_ALT2;
                    break;
            }

            DispatchCSD1(csd1.Route.Destination);
        }
    }
}