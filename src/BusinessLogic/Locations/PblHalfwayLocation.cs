using System;
using System.Net;
using System.Text;
using Ephi.Core.Helping;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Devices;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.Segments;
using PharmaProject.BusinessLogic.Wms_Communication;
using PharmaProject.BusinessLogic.Wms_Communication.Messages;

namespace PharmaProject.BusinessLogic.Locations
{
    public class PblHalfwayLocation : BaseLocation
    {
        private Conditional carryOver;
        private Conditional csd1AutoLoad;
        public bool Csd1DispatchAllowed;
        private InPin inBtn;
        private LongStretch longSeg;

        public PblHalfwayLocation(
            string utcIp,
            uint locationNumber,
            string BSLeftIp,
            string BSRightIp)
            : base(utcIp, locationNumber, 2U)
        {
            AddBarcodeScanner(new BarcodeScanner($"csd1BS1 Loc:{locationNumber}", IPAddress.Parse(BSLeftIp)));
            AddBarcodeScanner(new BarcodeScanner($"csd1BS2 Loc:{locationNumber}", IPAddress.Parse(BSRightIp)));
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

        protected override InPin[] ResetEmergencyPins => new InPin[1] { inBtn };

        public void Set2Barcode(string barcode)
        {
            OnBarcodeScanned(barcode);
        }

        protected override void OnBarcodeScanned(string barcode)
        {
            if (csd2.Route == null)
                Log("Rx barcode, but ROUTE IS NULL");
            
            var route = csd2.Route ?? new Route(START.LOAD_CSD2);
            csd2.Route = route;
            route.Barcode = barcode;
            base.OnBarcodeScanned(barcode);
        }

        private void StretchEmpty_OnEmptyChanged(bool empty)
        {
            if (!empty)
                return;
            
            Evaluate();
        }

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._17);
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            GetScripts(1U).DispatchNormalSegmentOccupied = MakeIn(PIN._24);
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            var name = $" U-Turn 2=>1 (Loc:{LocationNumber})";
            Conditional conditional = MakeConditionalBatch("Simultaneously load+dispatch" + name).AddStatement(scripts1.LoadAlternative).AddStatement(scripts2.DispatchAlternative);
            carryOver = MakeConditionalMacro(name).AddStatement(scripts2.LoadNormal).AddStatement(conditional);
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            var conditional1 = base.LoadNormalScript(csdNum);
            
            Conditional conditional2 = MakeConditionalStatement($"Auto load precondition CSD:{csdNum}, Loc:{LocationNumber}", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                .MakePrecondition().AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts.RollersRun, PIN_STATE.INACTIVE)
                .AddCondition(scripts.LiftRun, PIN_STATE.INACTIVE).AddCondition(scripts.OccupiedBelts, PIN_STATE.INACTIVE).AddCondition(scripts.OccupiedRollers, PIN_STATE.INACTIVE)
                .AddCondition(scripts.LoadTriggerNormal).CloseBlock();
            
            Conditional conditional3 = MakeConditionalMacro($"Auto load script CSD:{csdNum}, Loc:{LocationNumber}", csdNum == 1U ? RUN_MODE.ON_DEMAND : RUN_MODE.PERMANENTLY)
                .AddStatement(conditional2).AddStatement(conditional1);
            
            if (csdNum == 1U)
                csd1AutoLoad = conditional3;
            
            return conditional1;
        }

        protected override Conditional LoadAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            return csdNum == 1U
                ? MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedBelts, csdNum, endDelay: 600U, middleMotorRun: scripts.MiddleRollersRun)
                : base.LoadAlternativeScript(csdNum);
        }

        protected override Conditional DispatchAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            return csdNum == 2U
                ? MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, GetScripts(1U).OccupiedBelts, scripts.OccupiedBelts, csdNum,
                    middleMotorRun: scripts.MiddleRollersRun)
                : base.DispatchAlternativeScript(csdNum);
        }

        public override void DoEvaluate()
        {
            if (Status != UTC_STATUS.OPERATIONAL)
                return;
            
            EvaluateCsds(csd1, csd2);
            EvaluateCsds(csd2, csd1);
        }

        private void EvaluateCsds(CSD a, CSD b)
        {
            switch (a.State)
            {
                case SEGMENT_STATE.IDLE:
                    a.Route = null;
                    
                    if (a == csd2)
                        break;
            
                    var route1 = b.Route;
                    
                    if ((route1 != null ? route1.IsCrossover ? 1 : 0 : 0) != 0)
                    {
                        Log("Load trigger 1 CANCELLED");
                        csd1AutoLoad.Cancel();
                        break;
                    }

                    Log("Load trigger 1 RUN");
                    
                    if (csd1AutoLoad.IsRunningOrAboutToBe)
                        break;
                    
                    csd1AutoLoad.Run();
                    break;
                
                case SEGMENT_STATE.LOADING:
                    if (a == csd1)
                    {
                        if (!csd1AutoLoad.IsRunning)
                            break;
                
                        Dispatch(csd1, DESTINATION.DISPATCH_CSD1);
                        break;
                    }

                    if (a.Route != null)
                        break;

                    a.Route = new Route(START.LOAD_CSD2);
                    break;

                case SEGMENT_STATE.OCCUPIED:
                    if (a == csd1)
                    {
                        Dispatch(csd1, DESTINATION.DISPATCH_CSD1);
                        break;
                    }

                    var route2 = csd2.Route;
                    var to = route2?.Destination ?? DESTINATION.TBD;
                    if (to == DESTINATION.TBD)
                        to = DESTINATION.DISPATCH_CSD2;
                    Dispatch(csd2, to);
                    break;
            }
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            if (csd2.Route == null || !csd2.Route.Barcode.Equals(barcode) || csd2.Route.Destination != DESTINATION.TBD)
            {
                log.WarnFormat("No route, barcode mismatch, or already assigned: {3} ({0}, {1}, {2})", barcode, target, value1, csd2.Route);
            }
            else
            {
                switch (target)
                {
                    case WMS_TOTE_DIRECTION.DIRECTION_1:
                        Dispatch(csd2, DESTINATION.DISPATCH_CSD2);
                        break;
                
                    case WMS_TOTE_DIRECTION.DIRECTION_2:
                        Dispatch(csd2, DESTINATION.DISPATCH_CSD1);
                        break;
                }

                Evaluate();
            }
        }

        protected override void DispatchWmsFeedback(Route route)
        {
            if (route?.Barcode == null || route.Barcode.Equals(string.Empty))
                return;
            
            var direction = route.Destination == DESTINATION.DISPATCH_CSD1 ? WMS_TOTE_DIRECTION.DIRECTION_2 : WMS_TOTE_DIRECTION.DIRECTION_1;
            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(direction, Encoding.ASCII.GetBytes(route.Barcode), LocationNumber)));
            NdwConnectCommunicator.DirectionSentUpdate(LocationNumber, route.Barcode, direction);
        }

        private void Dispatch(CSD csd, DESTINATION to)
        {
            if (csd.Route != null)
                csd.Route.Destination = to;
            
            if (!Helpers.Contains(csd.State, SEGMENT_STATE.LOADING_PENDING, SEGMENT_STATE.LOADING, SEGMENT_STATE.OCCUPIED))
                return;
            
            if (to != DESTINATION.DISPATCH_CSD1)
            {
                if (to != DESTINATION.DISPATCH_CSD2)
                {
                    if (to == DESTINATION.TBD)
                        ;
                    throw new ArgumentException("Illegal dispatch destination");
                }

                csd.PassThrough();
            }
            
            else if (csd == csd1)
            {
                if (!LongSeg.Empty)
                {
                    log.InfoFormat("NO DISPATCH: Longstretch not empty");
                    return;
                }

                if (csd1.IsOccupied)
                {
                    log.InfoFormat("DISPATCH: Starting dispatch");
                    LongSeg.PerformedUpstreamDispatch();
                    csd.DispatchNormal();
                }
                else
                {
                    log.InfoFormat("DISPATCH: Starting passthrough");
                    LongSeg.PerformedUpstreamDispatch();
                    csd.PassThrough();
                }
            }
            
            else
            {
                if (!csd1.IsIdle)
                    return;
            
                csd1AutoLoad.Cancel();
                
                if (!carryOver.Run())
                    return;
                
                csd1.ForceLoadPending();
                csd2.ForceDispatchPending();
            }

            if (csd.Route != null)
            {
                csd.Route.Destination = to;
                DispatchWmsFeedback(csd.Route);
            }

            csd.Route = null;
        }
    }
}