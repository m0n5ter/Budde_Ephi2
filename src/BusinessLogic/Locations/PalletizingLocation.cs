// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Locations.PalletizingLocation
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System.Net;
using System.Text;
using Ephi.Core.Helping;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Devices;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.Wms_Communication;
using PharmaProject.BusinessLogic.Wms_Communication.Messages;

namespace PharmaProject.BusinessLogic.Locations
{
    public class PalletizingLocation : BaseLocation
    {
        private readonly DESTINATION defaultDestination;
        private Conditional dispatchCSD1;
        private Conditional dispatchCSD2;
        private InPin fillSensor1;
        private InPin fillSensor2;
        private InPin inBtn;
        private Conditional loadDisp;
        private OutPin motorExit1;
        private OutPin motorExit2;
        private Conditional passthrough;

        public PalletizingLocation(
            string IP,
            string csd1BS1Ip,
            uint locationNumber,
            DESTINATION defDest = DESTINATION.DISPATCH_CSD2)
            : base(IP, locationNumber, 2U)
        {
            defaultDestination = defDest;
            AddBarcodeScanner(new BarcodeScanner(string.Format("BS1 Loc:{0}", locationNumber), IPAddress.Parse(csd1BS1Ip)));
        }

        protected override InPin[] ResetEmergencyPins => new InPin[1] { inBtn };

        public void Set1Barcode(string barcode)
        {
            if (csd1.Route == null)
            {
                Log("Rx barcode, but ROUTE IS NULL");
            }
            else
            {
                csd1.Route.Barcode = barcode;
                RequestWmsDirection(barcode);
            }
        }

        protected override void OnBarcodeScanned(string barcode)
        {
            base.OnBarcodeScanned(barcode);
            if (csd1.Route == null)
                csd1.Route = new Route(START.LOAD_CSD1);
            csd1.Route.Barcode = barcode;
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (scripts == null)
                return null;
            if (csdNum == 1U)
                return MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, prevSegDispatch: scripts.UpstreamStartDispatching);
            return csdNum == 2U
                ? MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, middleMotorRun: scripts.MiddleRollersRun)
                : null;
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (scripts == null)
                return null;
            if (csdNum == 1U)
                return MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, null, scripts.OccupiedRollers, csdNum,
                    middleMotorRun: scripts.MiddleRollersRun);
            return csdNum == 2U
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.DispatchNormalSegmentOccupied, scripts.OccupiedRollers, csdNum,
                    nextSegLoad: scripts.DownstreamStartLoading, middleMotorRun: scripts.MiddleRollersRun)
                : null;
        }

        protected override Conditional DispatchAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (scripts == null)
                return null;
            if (csdNum == 1U)
                return MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.BeltsRun, scripts.OccupiedRollers, csdNum, endDelay: 1000U,
                    middleMotorRun: motorExit1);
            if (csdNum != 2U)
                return null;
            var conditional = MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.BeltsRun, scripts.OccupiedRollers, csdNum, endDelay: 1000U,
                middleMotorRun: motorExit2);
            conditional.OnStateChanged += DispatchAlternative_OnStateChanged;
            return conditional;
        }

        protected override Conditional LoadAlternativeScript(uint csdNum)
        {
            return null;
        }

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._24);
            motorExit1 = MakeOut(PIN._22);
            motorExit2 = MakeOut(PIN._23);
            var scripts1 = GetScripts(2U);
            fillSensor2 = scripts1.OccupiedBelts.LowActive;
            scripts1.DispatchNormalSegmentOccupied = MakeIn(PIN._21).LowActive;
            scripts1.MiddleRollersRun = MakeOut(PIN._21);
            scripts1.UpstreamStartDispatching = null;
            scripts1.DownstreamStartLoading = MakeOut(PIN._20);
            scripts1.DispatchAlternativeSegmentOccupied = MakeIn(PIN._10).LowActive;
            scripts1.OccupiedBelts = null;
            var scripts2 = GetScripts(1U);
            fillSensor1 = scripts2.OccupiedBelts.LowActive;
            scripts2.UpstreamStartDispatching = MakeOut(PIN._19);
            scripts2.DownstreamStartLoading = null;
            scripts2.MiddleRollersRun = MakeOut(PIN._21);
            scripts2.DispatchAlternativeSegmentOccupied = MakeIn(PIN._5).LowActive;
            scripts2.OccupiedBelts = null;
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var str = string.Format(" Crossover 1=>2 (Loc:{0})", LocationNumber);
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            loadDisp = MakeConditionalBatch("Simultaneously load+dispatch" + str).AddStatement(scripts1.DispatchNormal).AddStatement(scripts2.LoadNormal);
            dispatchCSD1 = MakeConditionalMacro("Dispatch alternative CSD:1 " + str).AddStatement(scripts1.LoadNormal).AddStatement(scripts1.DispatchAlternative);
            dispatchCSD2 = MakeConditionalMacro("Dispatch alternative CSD:2 " + str).AddStatement(scripts1.LoadNormal).AddStatement(loadDisp).AddStatement(scripts2.DispatchAlternative);
            passthrough = MakeConditionalMacro("Passthrough " + str).AddStatement(scripts1.LoadNormal).AddStatement(loadDisp);
            Conditional conditional = MakeConditionalStatement(string.Format("Auto load precondition CSD:1, Loc:{0}", LocationNumber), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).MakePrecondition()
                .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts1.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts1.RollersRun, PIN_STATE.INACTIVE)
                .AddCondition(scripts1.LiftRun, PIN_STATE.INACTIVE).AddCondition(scripts1.OccupiedRollers, PIN_STATE.INACTIVE).AddCondition(scripts1.LoadTriggerNormal).CloseBlock();
            MakeConditionalMacro(string.Format("Auto load script CSD:1, Loc:{0}", LocationNumber), RUN_MODE.PERMANENTLY).AddStatement(conditional).AddStatement(scripts1.LoadNormal);
            MakeConditionalStatement(string.Format("Free exit 1 Loc:{0}", LocationNumber), OUTPUT_ENFORCEMENT.ENF_AT_CONDITION_TRUE, RUN_MODE.PERMANENTLY).AddLogicBlock(LOGIC_FUNCTION.OR)
                .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts1.DispatchAlternativeSegmentOccupied).AddCondition(fillSensor1, PIN_STATE.INACTIVE).CloseBlock().AddCondition(scripts1.BeltsRun)
                .CloseBlock().AddOutputState(motorExit1);
            MakeConditionalStatement(string.Format("Free exit 2 Loc:{0}", LocationNumber), OUTPUT_ENFORCEMENT.ENF_AT_CONDITION_TRUE, RUN_MODE.PERMANENTLY).AddLogicBlock(LOGIC_FUNCTION.OR)
                .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts2.DispatchAlternativeSegmentOccupied).AddCondition(fillSensor2, PIN_STATE.INACTIVE).CloseBlock().AddCondition(scripts2.BeltsRun)
                .CloseBlock().AddOutputState(motorExit2);
        }

        private void DispatchAlternative_OnStateChanged(Conditional s)
        {
            if (s.Status != CONDITIONAL_STATE.RUNNING)
                return;
            csd2.ForceDispatchPending();
        }

        public override void DoEvaluate()
        {
            switch (csd1.State)
            {
                case SEGMENT_STATE.IDLE:
                    csd1.Route = null;
                    break;
                case SEGMENT_STATE.LOADING:
                    if (csd1.Route == null)
                    {
                        csd1.Route = new Route(START.LOAD_CSD1);
                    }

                    break;
                case SEGMENT_STATE.OCCUPIED:
                    var route1 = csd1.Route;
                    var to = route1 != null ? route1.Destination : DESTINATION.TBD;
                    if (to == DESTINATION.TBD)
                        to = defaultDestination;
                    DispatchCsd1(to);
                    break;
            }

            switch (csd2.State)
            {
                case SEGMENT_STATE.IDLE:
                    csd2.Route = null;
                    break;
                case SEGMENT_STATE.OCCUPIED:
                    var route2 = csd2.Route;
                    if ((route2 != null ? (int)route2.Destination : (int)defaultDestination) == 4)
                    {
                        if (!csd2.Scripts.DispatchAlternativeSegmentOccupied.Inactive)
                            break;
                        csd2.DispatchAlternative();
                        break;
                    }

                    if (!csd2.Scripts.DispatchNormalSegmentOccupied.Inactive)
                        break;
                    csd2.DispatchNormal();
                    break;
            }
        }

        private void DispatchCsd1(DESTINATION to)
        {
            var route = csd1.Route;
            if (route != null)
                route.Destination = to;
            if (!Helpers.Contains(csd1.State, SEGMENT_STATE.LOADING_PENDING, SEGMENT_STATE.LOADING, SEGMENT_STATE.OCCUPIED))
                return;
            if (to == DESTINATION.DISPATCH_CSD1_ALT)
            {
                if (csd1.Scripts.DispatchAlternativeSegmentOccupied.Active || !dispatchCSD1.Run())
                    return;
                csd1.ForceDispatchPending();
            }
            else
            {
                if (!Helpers.Contains(csd2.State, SEGMENT_STATE.IDLE, SEGMENT_STATE.LOADING_PENDING))
                    return;
                if (csd1.State == SEGMENT_STATE.OCCUPIED)
                {
                    if (!loadDisp.Run())
                        return;
                }
                else if (!passthrough.Run())
                {
                    return;
                }

                csd2.ForceLoadPending();
                csd1.ForceDispatchPending();
                csd2.Route = csd1.Route;
            }

            if (csd1.Route == null)
                return;
            csd1.Route.Destination = to;
            DispatchWmsFeedback(csd1.Route);
            csd1.Route = null;
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            if (csd1.Route == null || !csd1.Route.Barcode.Equals(barcode) || csd1.Route.Destination != DESTINATION.TBD)
                return;
            switch (target)
            {
                case WMS_TOTE_DIRECTION.DIRECTION_2:
                    csd1.Route.Destination = DESTINATION.DISPATCH_CSD1_ALT;
                    return;
                case WMS_TOTE_DIRECTION.DIRECTION_3:
                    csd1.Route.Destination = DESTINATION.DISPATCH_CSD2_ALT;
                    break;
                default:
                    csd1.Route.Destination = defaultDestination;
                    break;
            }

            DispatchCsd1(csd1.Route.Destination);
        }

        protected override void DispatchWmsFeedback(Route route)
        {
            if (route == null || route.Barcode == null || route.Barcode.Equals(string.Empty))
                return;
            var direction = WMS_TOTE_DIRECTION.DIRECTION_1;
            switch (route.Destination)
            {
                case DESTINATION.DISPATCH_CSD1_ALT:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_2;
                    break;
                case DESTINATION.DISPATCH_CSD2_ALT:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_3;
                    break;
            }

            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(direction, Encoding.ASCII.GetBytes(route.Barcode), LocationNumber)));
            NdwConnectCommunicator.DirectionSentUpdate(LocationNumber, route.Barcode, direction);
        }
    }
}