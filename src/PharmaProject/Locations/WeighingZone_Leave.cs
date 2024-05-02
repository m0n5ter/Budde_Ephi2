// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.WeighingZone_Leave
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using System.Net;
using System.Text;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.Segments;
using PharmaProject.Wms_Communication;
using PharmaProject.Wms_Communication.Messages;

namespace PharmaProject.Locations
{
    public class WeighingZone_Leave : BaseLocation
    {
        private readonly SharedSlopeControl SlopeControl;
        private Conditional CSD2_DispAltInside;
        private Conditional CSD2_DispAltOutside;
        private Conditional DispatchToSlope;
        private OutPin DrivePreSlopeSeg;
        private DateTime lastDispatchStraight = DateTime.Now;
        private readonly DelayedEvent ReEvaluate;

        public WeighingZone_Leave(
            string utcIp,
            uint locationNumber,
            string bs1Ip,
            string bs2Ip,
            SharedSlopeControl slopeControl)
            : base(utcIp, locationNumber, 3U)
        {
            AddBarcodeScanner(new BarcodeScanner(string.Format("bs1 Loc:{0}", locationNumber), IPAddress.Parse(bs1Ip)));
            AddBarcodeScanner(new BarcodeScanner(string.Format("bs2 Loc:{0}", locationNumber), IPAddress.Parse(bs2Ip)));
            SlopeControl = slopeControl;
            slopeControl.CanTransferChanged = Evaluate;
            ReEvaluate = new DelayedEvent(TimeSpan.FromMilliseconds(500.0), Evaluate);
        }

        public void Set1Barcode(string barcode)
        {
            if (csd2.Route == null)
            {
                Log("Rx barcode, but ROUTE IS NULL");
            }
            else
            {
                csd2.Route.Barcode = barcode;
                RequestWmsDirection(barcode);
            }
        }

        protected override void InitPins()
        {
            base.InitPins();
            DrivePreSlopeSeg = MakeOut(PIN._23);
            var scripts1 = GetScripts(2U);
            scripts1.LoadTriggerNormal = MakeIn(PIN._20);
            scripts1.UpstreamStartDispatching = MakeOut(PIN._19);
            scripts1.OccupiedBelts = MakeIn(PIN._10);
            scripts1.MiddleRollersRun = null;
            scripts1.DownstreamStartLoading = null;
            var scripts2 = GetScripts(1U);
            scripts2.LoadTriggerNormal = null;
            scripts2.UpstreamStartDispatching = null;
            scripts2.DispatchAlternativeSegmentOccupied = null;
            scripts2.DispatchNormalSegmentOccupied = MakeIn(PIN._21);
            scripts2.DownstreamStartLoading = MakeOut(PIN._20);
            scripts2.MiddleRollersRun = MakeOut(PIN._22);
            scripts2.OccupiedBelts = MakeIn(PIN._4);
            scripts2.OccupiedRollers = null;
            var scripts3 = GetScripts(3U);
            scripts3.LoadTriggerNormal = null;
            scripts3.UpstreamStartDispatching = null;
            scripts3.DispatchAlternativeSegmentOccupied = null;
            scripts3.DispatchNormalSegmentOccupied = MakeIn(PIN._22);
            scripts3.DownstreamStartLoading = MakeOut(PIN._21);
            scripts3.MiddleRollersRun = MakeOut(PIN._22);
            scripts3.OccupiedBelts = MakeIn(PIN._14);
            scripts3.OccupiedRollers = null;
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var scripts = GetScripts(2U);
            DispatchToSlope = MakeConditionalStatement(string.Format("Dispatch to slope (Loc:{0}, CSD:{1})", LocId, 2), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(5000U)
                .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts.DispatchNormalSegmentOccupied, PIN_STATE.INACTIVE).AddGuardBlock(1000U).AddGuardPin(scripts.DispatchNormalSegmentOccupied)
                .CloseBlock().CloseBlock().AddOutputState(DrivePreSlopeSeg);
            CSD2_DispAltInside = MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, GetScripts(3U).OccupiedBelts, scripts.OccupiedBelts, 2U,
                middleMotorRun: scripts.MiddleRollersRun);
            CSD2_DispAltOutside = MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, GetScripts(1U).OccupiedBelts, scripts.OccupiedBelts, 2U,
                middleMotorRun: scripts.MiddleRollersRun);
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 2U
                ? MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CCW, scripts.OccupiedRollers, csdNum, scripts.OccupiedBelts,
                    prevSegDispatch: scripts.UpstreamStartDispatching)
                : null;
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (csdNum == 1U)
                return MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.DispatchNormalSegmentOccupied, scripts.OccupiedBelts, csdNum,
                    nextSegLoad: scripts.DownstreamStartLoading);
            return csdNum == 3U
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CCW, scripts.DispatchNormalSegmentOccupied, scripts.OccupiedBelts, csdNum,
                    nextSegLoad: scripts.DownstreamStartLoading)
                : MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CCW, scripts.DispatchNormalSegmentOccupied, scripts.OccupiedBelts, csdNum,
                    nextSegLoad: DrivePreSlopeSeg);
        }

        protected override Conditional LoadAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 2U
                ? null
                : MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedBelts, csdNum, endDelay: 1000U, middleMotorRun: scripts.MiddleRollersRun);
        }

        protected override Conditional DispatchAlternativeScript(uint csdNum)
        {
            return null;
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            CSD2_DispAltInside.OnStateChanged += csd2.Dispatch_OnStateChanged;
            CSD2_DispAltInside.OnStateChanged += csd2.Dispatch_OnStateChanged;
        }

        public override void DoEvaluate()
        {
            HandleSideCsd(csd1);
            HandleSideCsd(csd3);
            Handle_Csd2();
            HandleSlope();
        }

        private void Handle_Csd2()
        {
            switch (csd2.State)
            {
                case CSD_STATE.IDLE:
                    if (!csd2.Scripts.LoadTriggerNormal.Active)
                        break;
                    csd2.LoadNormal();
                    break;
                case CSD_STATE.LOADING:
                    if (csd2.Route != null)
                        break;
                    csd2.Route = new Route(START.LOAD_CSD2);
                    break;
                case CSD_STATE.OCCUPIED:
                    var route = csd2.Route;
                    var destination = route != null ? route.Destination : DESTINATION.TBD;
                    Dispatch(destination == DESTINATION.TBD ? DESTINATION.DISPATCH_CSD2 : destination);
                    break;
            }
        }

        private void HandleSlope()
        {
            if (DateTime.Now.Subtract(lastDispatchStraight) < TimeSpan.FromMilliseconds(4500.0))
            {
                ReEvaluate.Start();
            }
            else
            {
                if (!GetScripts(2U).DispatchNormalSegmentOccupied.Active || DispatchToSlope.IsRunningOrAboutToBe)
                    return;
                lastDispatchStraight = DateTime.Now;
                var slopeControl = SlopeControl;
                if ((slopeControl != null ? slopeControl.CanTransfer ? 1 : 0 : 0) == 0)
                    return;
                DispatchToSlope.Run();
            }
        }

        private void HandleSideCsd(CSD csd)
        {
            switch (csd.State)
            {
                case CSD_STATE.OCCUPIED:
                    if (!csd.Scripts.DispatchNormalSegmentOccupied.Inactive)
                        break;
                    csd.DispatchNormal();
                    break;
            }
        }

        protected override void OnBarcodeScanned(string barcode)
        {
            base.OnBarcodeScanned(barcode);
            var route = csd2.Route;
            if (route == null)
                return;
            route.Barcode = barcode;
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            var route = csd2.Route;
            if (string.IsNullOrEmpty(route?.Barcode) || !route.Barcode.Equals(barcode))
                return;
            if (target != WMS_TOTE_DIRECTION.DIRECTION_1)
            {
                if (target == WMS_TOTE_DIRECTION.DIRECTION_2)
                    route.Destination = DESTINATION.DISPATCH_CSD2_ALT;
                else
                    route.Destination = DESTINATION.DISPATCH_CSD2;
            }
            else
            {
                route.Destination = DESTINATION.DISPATCH_CSD1;
            }
        }

        protected override void DispatchWmsFeedback(Route route)
        {
            if (route == null)
                return;
            WMS_TOTE_DIRECTION direction;
            switch (route.Destination)
            {
                case DESTINATION.DISPATCH_CSD1:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_1;
                    break;
                case DESTINATION.DISPATCH_CSD2_ALT:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_2;
                    break;
                default:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_3;
                    break;
            }

            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(direction, Encoding.ASCII.GetBytes(route.Barcode), LocationNumber)));
            NdwConnectCommunicator.DirectionSentUpdate(LocationNumber, route.Barcode, direction);
        }

        private void Dispatch(DESTINATION to)
        {
            var num = to == DESTINATION.DISPATCH_CSD2 ? 1 : 0;
            var flag1 = num != 0 || to == DESTINATION.DISPATCH_CSD2_ALT;
            var flag2 = csd3.State != CSD_STATE.IDLE;
            var flag3 = !csd1.IsIdle;
            if ((num & (flag2 ? 1 : 0)) != 0)
                to = DESTINATION.DISPATCH_CSD2_ALT;
            if ((to == DESTINATION.DISPATCH_CSD2_ALT) & flag3)
                to = DESTINATION.DISPATCH_CSD2;
            if (flag1 & flag2 & flag3)
                to = DESTINATION.DISPATCH_CSD1;
            switch (to)
            {
                case DESTINATION.DISPATCH_CSD2:
                    if (!csd3.LoadAlternative())
                        return;
                    csd2.ForceDispatchPending();
                    CSD2_DispAltInside.Run();
                    break;
                case DESTINATION.DISPATCH_CSD2_ALT:
                    if (!csd1.LoadAlternative())
                        return;
                    csd2.ForceDispatchPending();
                    CSD2_DispAltOutside.Run();
                    break;
                default:
                    if (!GetScripts(2U).DispatchNormalSegmentOccupied.Inactive)
                        return;
                    csd2.DispatchNormal();
                    break;
            }

            var route = csd2.Route ?? new Route(START.LOAD_CSD2);
            csd2.Route = null;
            route.Destination = to;
            DispatchWmsFeedback(route);
        }
    }
}