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
    public abstract class MainTrackCrossingLocation_Base : BaseLocation
    {
        private readonly BarcodeScanner csd1BS1;
        private readonly BarcodeScanner csd1BS2;
        private readonly BarcodeScanner csd2BS1;
        protected InPin inBtn;

        protected MainTrackCrossingLocation_Base(
            string IP,
            uint locationNumber,
            string csd1BS1Ip,
            string csd1BS2Ip,
            string csd2BS1Ip)
            : base(IP, locationNumber, 2U)
        {
            csd1BS1 = new BarcodeScanner($"csd1BS1 Loc:{locationNumber}", IPAddress.Parse(csd1BS1Ip));
            csd1BS2 = new BarcodeScanner($"csd1BS2 Loc:{locationNumber}", IPAddress.Parse(csd1BS2Ip));
            csd2BS1 = new BarcodeScanner($"csd2BS1 Loc:{locationNumber}", IPAddress.Parse(csd2BS1Ip));
            csd1BS1.OnBarcodeScanned += Csd1_OnBarcodeScanned;
            csd1BS2.OnBarcodeScanned += Csd1_OnBarcodeScanned;
            csd2BS1.OnBarcodeScanned += Csd2_OnBarcodeScanned;
            csd1BS1.OnNoRead += OnNoRead;
            csd1BS2.OnNoRead += OnNoRead;
            csd2BS1.OnNoRead += OnNoRead;
        }

        protected override InPin[] ResetEmergencyPins => new [] { inBtn };

        public void Set1Barcode(string barcode)
        {
            Csd1_OnBarcodeScanned(barcode);
        }

        public void Set2Barcode(string barcode)
        {
            Csd2_OnBarcodeScanned(barcode);
        }

        private void Csd1_OnBarcodeScanned(string barcode)
        {
            OnBarcodeScanned(barcode, csd1);
        }

        protected void Csd2_OnBarcodeScanned(string barcode)
        {
            OnBarcodeScanned(barcode, csd2);
        }

        private void OnBarcodeScanned(string barcode, CSD csd)
        {
            var route = csd.Route;

            if (route == null)
            {
                Log($"Rx barcode (Loc:{LocationNumber} csd{csd.CsdNum}), but ROUTE IS NULL");
                csd.ForceLoadPending();
                route = new Route(csd == csd1 ? START.LOAD_CSD1 : START.LOAD_CSD2);
                csd.Route = route;
            }

            route.Barcode = barcode;
            RequestWmsDirection(barcode);
            NdwConnectCommunicator.BarcodeScannedUpdate(LocationNumber, barcode);
        }

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._18);
            var scripts = GetScripts(1U);
            scripts.RollersDir = scripts.RollersDir.LowActive;
        }

        protected Conditional LoadNormalPrecondition(uint csdNum)
        {
            var scripts1 = GetScripts(csdNum);
            var scripts2 = GetScripts(csdNum == 1U ? 2U : 1U);
            
            return MakeConditionalStatement($"Auto load precondition CSD:{csdNum}, Loc:{LocationNumber}", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).MakePrecondition()
                .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts1.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts1.RollersRun, PIN_STATE.INACTIVE)
                .AddCondition(scripts1.LiftRun, PIN_STATE.INACTIVE).AddCondition(scripts1.OccupiedBelts, PIN_STATE.INACTIVE).AddCondition(scripts1.OccupiedRollers, PIN_STATE.INACTIVE)
                .AddCondition(scripts1.LoadTriggerNormal).AddCondition(scripts2.OccupiedBelts, PIN_STATE.INACTIVE).AddCondition(scripts2.OccupiedRollers, PIN_STATE.INACTIVE).CloseBlock();
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var conditional = base.LoadNormalScript(csdNum);
            
            MakeConditionalMacro($"Auto load script CSD:{csdNum}, Loc:{LocationNumber}", RUN_MODE.PERMANENTLY)
                .AddStatement(LoadNormalPrecondition(csdNum))
                .AddStatement(conditional);
            
            return conditional;
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            switch (csdNum)
            {
                case 1:
                case 2:
                    return MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.RollersRun, scripts.OccupiedRollers, csdNum,
                        nextSegLoad: scripts.DownstreamStartLoading, endDelay: 300U);
            
                default:
                    return null;
            }
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            var route = csd1.Route;
            CSD csd;
            
            if (barcode.Equals(route?.Barcode))
            {
                csd = csd1;
            }
            else
            {
                route = csd2.Route;
            
                if (!barcode.Equals(route?.Barcode))
                    return;
                
                csd = csd2;
            }

            if (route.Destination != DESTINATION.TBD)
                return;
            
            route.Destination = target == WMS_TOTE_DIRECTION.DIRECTION_1 ? DESTINATION.DISPATCH_CSD1 : DESTINATION.DISPATCH_CSD2;
            Dispatch(csd, route.Destination);
        }

        protected override void DispatchWmsFeedback(Route route)
        {
            if (string.IsNullOrWhiteSpace(route?.Barcode))
                return;
            
            var direction = route.Destination == DESTINATION.DISPATCH_CSD1 ? WMS_TOTE_DIRECTION.DIRECTION_1 : WMS_TOTE_DIRECTION.DIRECTION_2;
            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(direction, Encoding.ASCII.GetBytes(route.Barcode), LocationNumber)));
            NdwConnectCommunicator.DirectionSentUpdate(LocationNumber, route.Barcode, direction);
        }

        public override void DoEvaluate()
        {
            if (Status != UTC_STATUS.OPERATIONAL)
                return;
            
            EvaluateCsds(csd1, csd2);
            EvaluateCsds(csd2, csd1);
            
            if (!csd1.IsOccupied || !csd2.IsOccupied)
                return;
            
            var route1 = csd1.Route;
            
            if ((route1 != null ? route1.IsCrossover ? 1 : 0 : 0) == 0)
                return;
            
            var route2 = csd2.Route;
            
            if ((route2 != null ? route2.IsCrossover ? 1 : 0 : 0) == 0)
                return;
            
            csd2.Route.Destination = DESTINATION.DISPATCH_CSD2;
            EvaluateCsds(csd2, csd1);
        }

        private void EvaluateCsds(CSD a, CSD b)
        {
            var route1 = b.Route;
            var flag = route1 != null && route1.IsCrossover;
            
            if (flag)
            {
                var route2 = b.Route;
            
                switch (route2?.Destination ?? DESTINATION.TBD)
                {
                    case DESTINATION.DISPATCH_CSD1:
                    case DESTINATION.DISPATCH_CSD1_ALT:
                    case DESTINATION.DISPATCH_CSD1_ALT2:
                        flag = a == csd1;
                        break;
                
                    case DESTINATION.DISPATCH_CSD2:
                    case DESTINATION.DISPATCH_CSD2_ALT:
                    case DESTINATION.DISPATCH_CSD2_ALT2:
                        flag = a == csd2;
                        break;
                }
            }

            switch (a.State)
            {
                case SEGMENT_STATE.IDLE:
                    a.Route = null;
                    
                    if (flag || !a.Scripts.LoadTriggerNormal.Active)
                        break;
                    
                    a.LoadNormal();
                    break;

                case SEGMENT_STATE.LOADING_PENDING:
                case SEGMENT_STATE.LOADING:
                    if (a.Route != null)
                        break;
                
                    a.Route = new Route(a == csd1 ? START.LOAD_CSD1 : START.LOAD_CSD2);
                    break;
                
                case SEGMENT_STATE.OCCUPIED:
                    var destination = a == csd1 ? DESTINATION.DISPATCH_CSD1 : DESTINATION.DISPATCH_CSD2;
                    var def = destination;
                
                    if (!GetCsdDispatchDestination(a, ref def))
                        break;
                    
                    var route3 = a.Route;
                    
                    if ((route3 != null ? route3.IsCrossover ? 1 : 0 : 0) != 0 && !b.IsIdle && a.StateAge > TimeSpan.FromMilliseconds(WaitForWmsFeedbackMs))
                        def = destination;
                    
                    Dispatch(a, def);
                    break;
            }
        }

        private void Dispatch(CSD csd, DESTINATION to)
        {
            var route = csd.Route;
            
            if (route != null)
                route.Destination = to;
            
            if (!Helpers.Contains(csd.State, SEGMENT_STATE.LOADING_PENDING, SEGMENT_STATE.LOADING, SEGMENT_STATE.OCCUPIED))
                return;
            
            switch (to)
            {
                case DESTINATION.DISPATCH_CSD1:
                case DESTINATION.DISPATCH_CSD1_ALT:
                    if (csd == csd1)
                    {
                        if (csd.Scripts.DispatchNormalSegmentOccupied.Active)
                            return;
                        if (csd.IsOccupied)
                        {
                            csd.DispatchNormal();
                            break;
                        }

                        csd.PassThrough();
                        break;
                    }

                    if (!csd1.IsIdle || !csd2.IsOccupied || !csd1.LoadAlternative() || !csd2.DispatchAlternative())
                        return;
                    csd1.Route = csd2.Route;
                    csd2.Route = null;
                    return;
            
                case DESTINATION.DISPATCH_CSD2:
                    if (csd == csd2)
                    {
                        if (csd.Scripts.DispatchNormalSegmentOccupied.Active)
                            return;
                        if (csd.IsOccupied)
                        {
                            csd.DispatchNormal();
                            break;
                        }

                        csd.PassThrough();
                        break;
                    }

                    if (!csd2.IsIdle || !csd1.IsOccupied || !csd2.LoadAlternative() || !csd1.DispatchAlternative())
                        return;
                    csd2.Route = csd1.Route;
                    csd1.Route = null;
                    return;
                
                default:
                    throw new ArgumentException("Illegal dispatch destination");
            }

            if (route != null)
                DispatchWmsFeedback(route);
            
            csd.Route = null;
        }
    }
}