// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.PackingLocation
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System.Net;
using System.Text;
using Ephi.Core.Helping;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.Segments;
using PharmaProject.Wms_Communication;
using PharmaProject.Wms_Communication.Messages;

namespace PharmaProject.Locations
{
    public class PackingLocation : BaseLocation
    {
        private Conditional csd1DispatchLeft;
        private Conditional csd1DispatchRight;
        private Conditional csd2DispatchLeft;
        private Conditional csd2DispatchRight;

        public PackingLocation(string IP, uint locationNumber, string bs1IP, string bs2IP)
            : base(IP, locationNumber, 2U)
        {
            AddBarcodeScanner(new BarcodeScanner(string.Format("Loc:{0} Barcodescanner:{1}", locationNumber, 1), IPAddress.Parse(bs1IP)));
            AddBarcodeScanner(new BarcodeScanner(string.Format("Loc:{0} Barcodescanner:{1}", locationNumber, 2), IPAddress.Parse(bs2IP)));
        }

        protected override void InitPins()
        {
            base.InitPins();
            var scripts1 = GetScripts(1U);
            scripts1.UpstreamStartDispatching = MakeOut(PIN._20);
            scripts1.LoadTriggerNormal = MakeIn(PIN._21).LowActive;
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
            MakeConditionalMacro(string.Format("Auto load CSD:1, Loc:{0}", LocationNumber), RUN_MODE.PERMANENTLY).AddStatement(conditional).AddStatement(scripts.LoadNormal);
        }

        protected override void InitCsdScrips(uint csdNum)
        {
            base.InitCsdScrips(csdNum);
            var scripts = GetScripts(csdNum);
            if (scripts == null)
                return;
            var conditional1 = MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.LiftUp, scripts.OccupiedBelts, csdNum,
                extraActiveNotFinished: scripts.OccupiedRollers, endDelay: 1000U);
            var conditional2 = MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.LiftUp, scripts.OccupiedBelts, csdNum,
                extraActiveNotFinished: scripts.OccupiedRollers, endDelay: 1000U);
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

        protected override void AttachCsdEventHandlers(CSD csd)
        {
            base.AttachCsdEventHandlers(csd);
            csd1DispatchRight.OnStateChanged += csd1.Dispatch_OnStateChanged;
            csd1DispatchLeft.OnStateChanged += csd1.Dispatch_OnStateChanged;
            csd2DispatchRight.OnStateChanged += csd2.Dispatch_OnStateChanged;
            csd2DispatchLeft.OnStateChanged += csd2.Dispatch_OnStateChanged;
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (scripts == null)
                return null;
            if (csdNum == 1U)
                return MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.LiftUp, scripts.OccupiedRollers, csdNum,
                    extraActiveNotFinished: scripts.OccupiedBelts, middleMotorRun: scripts.MiddleRollersRun);
            return csdNum == 2U
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.LiftUp, scripts.OccupiedRollers, csdNum,
                    extraActiveNotFinished: scripts.OccupiedBelts, nextSegLoad: scripts.DownstreamStartLoading)
                : null;
        }

        public override void DoEvaluate()
        {
            switch (csd1.State)
            {
                case CSD_STATE.IDLE:
                    if (csd1.Route != null && (csd1.Route.Destination == DESTINATION.DISPATCH_CSD1_ALT || csd1.Route.Destination == DESTINATION.DISPATCH_CSD1_ALT2))
                        DispatchWmsFeedback(csd1.Route);
                    csd1.Route = null;
                    break;
                case CSD_STATE.LOADING_PENDING:
                case CSD_STATE.LOADING:
                    if (csd1.Route == null)
                    {
                        csd1.Route = new Route(START.LOAD_CSD1);
                    }

                    break;
                case CSD_STATE.OCCUPIED:
                    var route1 = csd1.Route;
                    DispatchCSD1(route1 != null ? route1.Destination : DESTINATION.TBD);
                    break;
            }

            switch (csd2.State)
            {
                case CSD_STATE.IDLE:
                    if (!string.IsNullOrEmpty(csd2.Route?.Barcode))
                        DispatchWmsFeedback(csd2.Route);
                    csd2.Route = null;
                    break;
                case CSD_STATE.LOADING:
                    var route2 = csd2.Route;
                    if (Helpers.Contains((DESTINATION)(route2 != null ? (int)route2.Destination : 4), DESTINATION.DISPATCH_CSD2_ALT, DESTINATION.DISPATCH_CSD2_ALT2))
                        break;
                    DispatchCSD2(csd2.Route.Destination);
                    break;
                case CSD_STATE.OCCUPIED:
                    var route3 = csd2.Route;
                    DispatchCSD2(route3 != null ? route3.Destination : DESTINATION.TBD);
                    break;
            }
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
                    if (!csd2.IsIdle || !csd2.LoadNormal() || (!csd1.PassThrough() && !csd1.DispatchNormal()))
                        break;
                    csd2.Route = csd1.Route;
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

            if (!Helpers.Contains(target, WMS_TOTE_DIRECTION.DIRECTION_1, WMS_TOTE_DIRECTION.DIRECTION_4, WMS_TOTE_DIRECTION.DIRECTION_5))
                return;
            DispatchCSD1(csd1.Route.Destination);
        }
    }
}