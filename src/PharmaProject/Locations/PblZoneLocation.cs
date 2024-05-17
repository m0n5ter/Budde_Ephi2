// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.PblZoneLocation
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
    public class PblZoneLocation : BaseLocation
    {
        private Conditional CrossOverAlt;
        private Conditional CrossOverNormal;
        private Conditional csd2AutoLoad;
        private LongStretch longSeg;

        public PblZoneLocation(string utcIp, uint locationNumber, string csd1BS1Ip, string csd1BS2Ip)
            : base(utcIp, locationNumber, 2U)
        {
            AddBarcodeScanner(new BarcodeScanner($"csd1BS1 Loc:{locationNumber}", IPAddress.Parse(csd1BS1Ip)));
            AddBarcodeScanner(new BarcodeScanner($"csd1BS2 Loc:{locationNumber}", IPAddress.Parse(csd1BS2Ip)));
        }

        public LongStretch LongSeg
        {
            get => longSeg;
            set
            {
                if (longSeg != null)
                    longSeg.OnEmptyChanged -= StretchEmpty_OnEmptyChanged;
                longSeg = value;
                if (longSeg == null)
                    return;
                longSeg.OnEmptyChanged += StretchEmpty_OnEmptyChanged;
            }
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
            var scripts = GetScripts(2U);
            scripts.DispatchNormalSegmentOccupied = null;
            scripts.LoadTriggerNormal = MakeIn(PIN._20).LowActive;
            scripts.DownstreamStartLoading = MakeOut(PIN._20);
            scripts.UpstreamStartDispatching = MakeOut(PIN._19);
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var str = $" Crossover 2=>1 (Loc:{LocationNumber})";
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            Conditional conditional1 = MakeConditionalBatch("Simultaneously load+dispatch" + str).AddStatement(scripts1.LoadAlternative).AddStatement(scripts2.DispatchAlternative);
            CrossOverAlt = MakeConditionalMacro("Dispatch alternative " + str).AddStatement(scripts2.LoadNormal).AddStatement(conditional1).AddStatement(scripts1.DispatchAlternative);
            CrossOverNormal = MakeConditionalMacro("Dispatch normal " + str).AddStatement(scripts2.LoadNormal).AddStatement(conditional1).AddStatement(scripts1.DispatchNormal);
            var scripts3 = GetScripts(2U);
            Conditional conditional2 = MakeConditionalStatement($"Auto load precondition CSD:2, Loc:{LocationNumber}", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).MakePrecondition()
                .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts3.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts3.RollersRun, PIN_STATE.INACTIVE)
                .AddCondition(scripts3.LiftRun, PIN_STATE.INACTIVE).AddCondition(scripts3.OccupiedBelts, PIN_STATE.INACTIVE).AddCondition(scripts3.OccupiedRollers, PIN_STATE.INACTIVE)
                .AddCondition(scripts3.LoadTriggerNormal).CloseBlock();
            csd2AutoLoad = MakeConditionalMacro($"Auto load script CSD:2, Loc:{LocationNumber}").AddStatement(conditional2).AddStatement(scripts3.LoadNormal);
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 2U
                ? MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CCW, scripts.OccupiedRollers, csdNum,
                    prevSegDispatch: GetScripts(2U).UpstreamStartDispatching)
                : null;
        }

        protected override Conditional LoadAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 1U
                ? MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedBelts, csdNum, endDelay: 500U, middleMotorRun: scripts.MiddleRollersRun,
                    middleMotorDir: scripts.MiddleRollersDir)
                : null;
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);

            if (csdNum == 1U)
                return MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.LiftDown, scripts.OccupiedBelts, csdNum, endDelay: 500U,
                    middleMotorRun: MakeOut(PIN._22));
            
            return csdNum == 2U
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CCW, scripts.LiftDown, scripts.OccupiedRollers, csdNum,
                    nextSegLoad: scripts.DownstreamStartLoading)
                : null;
        }

        protected override Conditional DispatchAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (csdNum == 1U)
                return MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CCW, scripts.LiftDown, scripts.OccupiedBelts, csdNum, endDelay: 3000U,
                    middleMotorRun: MakeOut(PIN._21));
            return csdNum == 2U
                ? MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, GetScripts(1U).OccupiedBelts, scripts.OccupiedRollers, csdNum,
                    middleMotorRun: scripts.MiddleRollersRun, middleMotorDir: scripts.MiddleRollersDir)
                : null;
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
        }

        public override void DoEvaluate()
        {
            switch (csd2.State)
            {
                case CSD_STATE.IDLE:
                    if (csd2.Route != null && !csd2.Route.Barcode.Equals(""))
                        DispatchWmsFeedback(csd2.Route);
                    csd2.Route = null;
                    csd2AutoLoad.Run();
                    break;
                case CSD_STATE.LOADING:
                    if (csd2.Route != null)
                        break;
                    csd2.Route = new Route(START.LOAD_CSD2);
                    break;
                case CSD_STATE.OCCUPIED:
                    var route = csd2.Route;
                    var to = route?.Destination ?? DESTINATION.TBD;
                    if (to == DESTINATION.TBD)
                        to = DESTINATION.DISPATCH_CSD2;
                    Dispatch(to);
                    break;
            }
        }

        protected override void DispatchWmsFeedback(Route route)
        {
            if (route.Barcode.Equals(string.Empty))
                return;
            WMS_TOTE_DIRECTION direction;
            switch (route.Destination)
            {
                case DESTINATION.DISPATCH_CSD1:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_2;
                    break;
                case DESTINATION.DISPATCH_CSD1_ALT:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_3;
                    break;
                default:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_1;
                    break;
            }

            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(direction, Encoding.ASCII.GetBytes(route.Barcode), LocationNumber)));
            NdwConnectCommunicator.DirectionSentUpdate(LocationNumber, route.Barcode, direction);
        }

        protected override void OnBarcodeScanned(string barcode)
        {
            if (!SendAllowed)
                return;
            base.OnBarcodeScanned(barcode);
            if (csd2.Route == null)
                csd2.Route = new Route(START.LOAD_CSD2);
            csd2.Route.Barcode = barcode;
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            if (csd2.Route == null || !csd2.Route.Barcode.Equals(barcode) || csd2.Route.Destination != DESTINATION.TBD)
                return;
            switch (target)
            {
                case WMS_TOTE_DIRECTION.DIRECTION_1:
                    Dispatch(DESTINATION.DISPATCH_CSD2);
                    break;
                case WMS_TOTE_DIRECTION.DIRECTION_2:
                    Dispatch(DESTINATION.DISPATCH_CSD1);
                    break;
                case WMS_TOTE_DIRECTION.DIRECTION_3:
                    Dispatch(DESTINATION.DISPATCH_CSD1_ALT);
                    break;
            }

            Evaluate();
        }

        private void Dispatch(DESTINATION to)
        {
            var csd2 = this.csd2;
            if (csd2.Route != null)
                csd2.Route.Destination = to;
            if (!Helpers.Contains(csd2.State, CSD_STATE.LOADING_PENDING, CSD_STATE.LOADING, CSD_STATE.OCCUPIED))
                return;
            switch (to)
            {
                case DESTINATION.DISPATCH_CSD1:
                    if (!csd1.IsIdle || !CrossOverNormal.Run())
                        break;
                    csd1.ForceLoadPending();
                    this.csd2.ForceDispatchPending();
                    break;
                case DESTINATION.DISPATCH_CSD1_ALT:
                    if (!csd1.IsIdle || !CrossOverAlt.Run())
                        break;
                    csd1.ForceLoadPending();
                    this.csd2.ForceDispatchPending();
                    break;
                case DESTINATION.DISPATCH_CSD2:
                    if (!LongSeg.Empty)
                        break;
                    csd2.PassThrough();
                    break;
                default:
                    throw new ArgumentException("Illegal dispatch destination");
            }
        }

        private void StretchEmpty_OnEmptyChanged(bool obj)
        {
            Evaluate();
        }
    }
}