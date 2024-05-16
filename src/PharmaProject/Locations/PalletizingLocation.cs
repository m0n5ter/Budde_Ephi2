// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.PalletizingLocation
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using System.Net;
using System.Text;
using Ephi.Core.Helping;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.Wms_Communication;
using PharmaProject.Wms_Communication.Messages;

namespace PharmaProject.Locations
{
    public class PalletizingLocation : BaseLocation
    {
        private Conditional csd1AutoLoad;
        private Conditional dispatchCSD1;
        private Conditional dispatchCSD2;
        private Conditional finishDispatchCSD2;
        private InPin inBtn;
        private Conditional loadDisp;
        private Conditional passthrough;

        public PalletizingLocation(string IP, string csd1BS1Ip, uint locationNumber)
            : base(IP, locationNumber, 2U)
        {
            AddBarcodeScanner(new BarcodeScanner($"BS1 Loc:{locationNumber}", IPAddress.Parse(csd1BS1Ip)));
            dispatchCSD1.OnStateChanged += csd1.Dispatch_OnStateChanged;
            dispatchCSD2.OnStateChanged += csd2.Dispatch_OnStateChanged;
            finishDispatchCSD2.OnStateChanged += csd2.Dispatch_OnStateChanged;
            GetScripts(1U).DispatchAlternative.OnStateChanged -= csd1.Dispatch_OnStateChanged;
            GetScripts(2U).DispatchAlternative.OnStateChanged -= csd2.Dispatch_OnStateChanged;
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
                : base.LoadNormalScript(csdNum);
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (scripts == null)
                return null;
            if (csdNum == 1U)
                return MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.LiftDown, scripts.OccupiedRollers, csdNum,
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
                return MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.DispatchAlternativeSegmentOccupied, scripts.OccupiedRollers, csdNum,
                    middleMotorRun: MakeOut(PIN._22));
            return csdNum == 2U
                ? MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.DispatchAlternativeSegmentOccupied, scripts.OccupiedRollers, csdNum,
                    middleMotorRun: MakeOut(PIN._23))
                : null;
        }

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._24);
            var scripts1 = GetScripts(2U);
            scripts1.DispatchNormalSegmentOccupied = MakeIn(PIN._21).LowActive;
            scripts1.MiddleRollersRun = MakeOut(PIN._21);
            scripts1.UpstreamStartDispatching = null;
            scripts1.DownstreamStartLoading = MakeOut(PIN._20);
            scripts1.DispatchAlternativeSegmentOccupied = MakeIn(PIN._10).LowActive;
            var scripts2 = GetScripts(1U);
            scripts2.UpstreamStartDispatching = MakeOut(PIN._19);
            scripts2.DownstreamStartLoading = null;
            scripts2.MiddleRollersRun = MakeOut(PIN._21);
            scripts2.DispatchAlternativeSegmentOccupied = MakeIn(PIN._5).LowActive;
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var str = $" Crossover 1=>2 (Loc:{LocationNumber})";
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            loadDisp = MakeConditionalBatch("Simultaneously load+dispatch" + str).AddStatement(scripts1.DispatchNormal).AddStatement(scripts2.LoadNormal);
            dispatchCSD1 = MakeConditionalMacro("Dispatch alternative CSD:1 " + str).AddStatement(scripts1.LoadNormal).AddStatement(scripts1.DispatchAlternative).AddStatement(
                MakeConditionalStatement("Move untill sensor low CSD:1" + str, OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddOutputState(MakeOut(PIN._22))
                    .AddCondition(scripts1.DispatchAlternativeSegmentOccupied, PIN_STATE.INACTIVE).AddGlobalTimeout(3000U));
            finishDispatchCSD2 = MakeConditionalMacro("Finish dispatch alternative CSD:2 " + str).AddStatement(scripts2.DispatchAlternative).AddStatement(
                MakeConditionalStatement("Move untill sensor low CSD:2" + str, OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddOutputState(MakeOut(PIN._23))
                    .AddCondition(scripts2.DispatchAlternativeSegmentOccupied, PIN_STATE.INACTIVE).AddGlobalTimeout(3000U));
            dispatchCSD2 = MakeConditionalMacro("Dispatch alternative CSD:2 " + str).AddStatement(scripts1.LoadNormal).AddStatement(loadDisp).AddStatement(scripts2.DispatchAlternative).AddStatement(
                MakeConditionalStatement("Move untill sensor low CSD:2" + str, OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddOutputState(MakeOut(PIN._23))
                    .AddCondition(scripts2.DispatchAlternativeSegmentOccupied, PIN_STATE.INACTIVE).AddGlobalTimeout(3000U));
            passthrough = MakeConditionalMacro("Passthrough " + str).AddStatement(scripts1.LoadNormal).AddStatement(loadDisp).AddStatement(scripts2.DispatchNormal);
            Conditional conditional = MakeConditionalStatement($"Auto load precondition CSD:1, Loc:{LocationNumber}", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).MakePrecondition()
                .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts1.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts1.RollersRun, PIN_STATE.INACTIVE)
                .AddCondition(scripts1.LiftRun, PIN_STATE.INACTIVE).AddCondition(scripts1.OccupiedBelts, PIN_STATE.INACTIVE).AddCondition(scripts1.OccupiedRollers, PIN_STATE.INACTIVE)
                .AddCondition(scripts1.LoadTriggerNormal).CloseBlock();
            csd1AutoLoad = MakeConditionalMacro($"Auto load script CSD:1, Loc:{LocationNumber}").AddStatement(conditional).AddStatement(scripts1.LoadNormal);
        }

        public override void DoEvaluate()
        {
            switch (csd1.State)
            {
                case CSD_STATE.IDLE:
                    csd1.Route = null;
                    csd1AutoLoad.Run();
                    break;
                case CSD_STATE.LOADING:
                    if (csd1.Route == null)
                    {
                        csd1.Route = new Route(START.LOAD_CSD1);
                    }

                    break;
                case CSD_STATE.OCCUPIED:
                    var route = csd1.Route;
                    var to = route?.Destination ?? DESTINATION.TBD;
                    if (to == DESTINATION.TBD)
                        to = DESTINATION.DISPATCH_CSD2;
                    Dispatch(to);
                    break;
            }

            switch (csd2.State)
            {
                case CSD_STATE.IDLE:
                    csd2.Route = null;
                    break;
                case CSD_STATE.OCCUPIED:
                    if (csd2.Route == null && !csd2.Scripts.DispatchNormalSegmentOccupied.Active && csd2.MeasureOccupied)
                    {
                        csd2.DispatchNormal();
                        break;
                    }

                    if (csd2.Route == null)
                        break;
                    if (csd2.Route.Destination == DESTINATION.TBD ||
                        (csd2.Route.Destination == DESTINATION.DISPATCH_CSD2 && !csd2.Scripts.DispatchNormalSegmentOccupied.Active && csd2.MeasureOccupied))
                    {
                        csd2.DispatchNormal();
                        break;
                    }

                    if (csd2.Route.Destination != DESTINATION.DISPATCH_CSD2_ALT || csd2.Scripts.DispatchAlternativeSegmentOccupied.Active || !csd2.MeasureOccupied)
                        break;
                    finishDispatchCSD2.Run();
                    break;
            }
        }

        private void Dispatch(DESTINATION to)
        {
            if (csd1.Route != null)
                csd1.Route.Destination = to;
            if (!Helpers.Contains(csd1.State, CSD_STATE.LOADING_PENDING, CSD_STATE.LOADING, CSD_STATE.OCCUPIED))
                return;
            switch (to)
            {
                case DESTINATION.DISPATCH_CSD1_ALT:
                    if (csd1.Scripts.DispatchAlternativeSegmentOccupied.Active || !dispatchCSD1.Run())
                        return;
                    csd2.ForceLoadPending();
                    csd1.ForceDispatchPending();
                    break;
                case DESTINATION.DISPATCH_CSD2:
                    if (!Helpers.Contains(csd2.State, CSD_STATE.IDLE, CSD_STATE.LOADING_PENDING))
                        return;
                    if (!csd2.Scripts.DispatchNormalSegmentOccupied.Active)
                    {
                        if (!passthrough.Run())
                            return;
                        csd2.ForceLoadPending();
                        csd1.ForceDispatchPending();
                        csd2.Route = csd1.Route;
                        break;
                    }

                    if (!loadDisp.Run())
                        return;
                    csd2.ForceLoadPending();
                    csd1.ForceDispatchPending();
                    csd2.Route = csd1.Route;
                    break;
                case DESTINATION.DISPATCH_CSD2_ALT:
                    if (!Helpers.Contains(csd2.State, CSD_STATE.IDLE, CSD_STATE.LOADING_PENDING))
                        return;
                    if (!csd2.Scripts.DispatchAlternativeSegmentOccupied.Active)
                    {
                        if (!dispatchCSD2.Run())
                            return;
                        csd2.ForceLoadPending();
                        csd1.ForceDispatchPending();
                        csd2.Route = csd1.Route;
                        break;
                    }

                    if (!loadDisp.Run())
                        return;
                    csd2.ForceLoadPending();
                    csd1.ForceDispatchPending();
                    csd2.Route = csd1.Route;
                    break;
                default:
                    throw new ArgumentException("Illegal dispatch destination");
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
            if (target != WMS_TOTE_DIRECTION.DIRECTION_2)
            {
                if (target == WMS_TOTE_DIRECTION.DIRECTION_3)
                    csd1.Route.Destination = DESTINATION.DISPATCH_CSD2_ALT;
                else
                    csd1.Route.Destination = DESTINATION.DISPATCH_CSD2;
            }
            else
            {
                csd1.Route.Destination = DESTINATION.DISPATCH_CSD1_ALT;
            }
        }

        protected override void DispatchWmsFeedback(Route route)
        {
            if (route?.Barcode == null || route.Barcode.Equals(string.Empty))
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