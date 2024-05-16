using System;
using System.Net;
using System.Text;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Devices;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.Segments;
using PharmaProject.BusinessLogic.Wms_Communication;
using PharmaProject.BusinessLogic.Wms_Communication.Messages;

namespace PharmaProject.BusinessLogic.Locations
{
    public class WeighingZone_Leave : BaseLocation
    {
        private const uint SLOPE_TOTE_SPACING_ms = 3500;
        private readonly SharedSlopeControl SlopeControl;
        private Conditional CSD2_DispAltInside;
        private Conditional CSD2_DispAltOutside;
        private Conditional DispatchToSlope;
        private OutPin DrivePreSlopeSeg;
        private DateTime lastDispatchStraight = DateTime.Now;

        public WeighingZone_Leave(
            string utcIp,
            uint locationNumber,
            string bs1Ip,
            string bs2Ip,
            SharedSlopeControl slopeControl)
            : base(utcIp, locationNumber, 3U)
        {
            AddBarcodeScanner(new BarcodeScanner($"bs1 Loc:{locationNumber}", IPAddress.Parse(bs1Ip)));
            AddBarcodeScanner(new BarcodeScanner($"bs2 Loc:{locationNumber}", IPAddress.Parse(bs2Ip)));
            SlopeControl = slopeControl;
            slopeControl.CanTransferChanged = Evaluate;
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
            scripts1.MiddleRollersRun = MakeOut(PIN._22);
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

            DispatchToSlope = MakeConditionalStatement($"Dispatch to slope (Loc:{LocId}, CSD:{2})", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(5000U)
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
                    nextSegLoad: DrivePreSlopeSeg, minimumRunTimeMs: 1000);
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

        private void DispatchToSlope_OnStateChanged(Conditional c)
        {
            if (c.Status != CONDITIONAL_STATE.FINISHED)
                return;
            
            Evaluate();
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            CSD2_DispAltInside.OnStateChanged += csd2.Dispatch_OnStateChanged;
            CSD2_DispAltInside.OnStateChanged += csd2.Dispatch_OnStateChanged;
            DispatchToSlope.OnStateChanged += DispatchToSlope_OnStateChanged;
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
                case SEGMENT_STATE.IDLE:
                    if (!csd2.Scripts.LoadTriggerNormal.Active)
                        break;
                
                    csd2.LoadNormal();
                    break;
            
                case SEGMENT_STATE.LOADING:
                    if (csd2.Route != null)
                        break;
                    
                    csd2.Route = new Route(START.LOAD_CSD2);
                    break;
                
                case SEGMENT_STATE.OCCUPIED:
                    var def = DESTINATION.DISPATCH_CSD2;
                    
                    if (!GetCsdDispatchDestination(csd2, ref def))
                        break;
                    
                    Dispatch(def);
                    break;
            }
        }

        private void HandleSlope()
        {
            if (DateTime.Now.Subtract(lastDispatchStraight) < TimeSpan.FromMilliseconds(3500.0))
            {
                ReEvaluate.Start();
            }
            else
            {
                if (!GetScripts(2U).DispatchNormalSegmentOccupied.Active || DispatchToSlope.IsRunningOrAboutToBe)
                    return;
                
                var slopeControl = SlopeControl;
                
                if ((slopeControl != null ? slopeControl.CanTransfer ? 1 : 0 : 0) == 0)
                    return;
                
                lastDispatchStraight = DateTime.Now;
                DispatchToSlope.Run();
            }
        }

        private void HandleSideCsd(CSD csd)
        {
            switch (csd.State)
            {
                case SEGMENT_STATE.OCCUPIED:
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
            
            switch (target)
            {
                case WMS_TOTE_DIRECTION.DIRECTION_1:
                    route.Destination = DESTINATION.DISPATCH_CSD1;
                    break;
            
                case WMS_TOTE_DIRECTION.DIRECTION_2:
                    route.Destination = DESTINATION.DISPATCH_CSD2_ALT;
                    break;
                
                default:
                    route.Destination = DESTINATION.DISPATCH_CSD2;
                    break;
            }

            Evaluate();
        }

        protected override void DispatchWmsFeedback(Route route)
        {
            if (route == null || string.IsNullOrEmpty(route.Barcode))
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
            var flag1 = to == DESTINATION.DISPATCH_CSD2_ALT;
            var flag2 = (num | (flag1 ? 1 : 0)) != 0;
            var flag3 = !csd3.IsIdle;
            var flag4 = !csd1.IsIdle;
            
            if ((num & (flag3 ? 1 : 0)) != 0)
                to = DESTINATION.DISPATCH_CSD2_ALT;
            
            if (flag1 & flag4)
                to = DESTINATION.DISPATCH_CSD2;
            
            if (flag2 & flag3 & flag4)
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
                    if (DispatchToSlope.IsRunningOrAboutToBe || !GetScripts(2U).DispatchNormalSegmentOccupied.Inactive)
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