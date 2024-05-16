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
    public class SmallStorageLocation : BaseLocation
    {
        private const int PASSTHROUGH_DELAY_ms = 1000;
        private volatile bool buttonPressPending;
        private readonly DelayedEvent occupiedTimeout;
        private readonly DelayedEvent passThroughEval_csd2to3;
        private readonly DelayedEvent passThroughEval_csd3to4;
        private readonly DelayedEvent passThroughEval_csd3toAcm;
        private readonly DelayedEvent passThroughEval_csd6;
        private SmallStorageSublocation subLoc;

        public SmallStorageLocation(
            string utcIp_1,
            string utcIp_2,
            uint locationNumber,
            string BSLeftIp,
            string BSRightIp)
            : base(utcIp_1, locationNumber, 3U)
        {
            AddBarcodeScanner(new BarcodeScanner($"csd1BS1 Loc:{locationNumber}", IPAddress.Parse(BSLeftIp)));
            AddBarcodeScanner(new BarcodeScanner($"csd1BS2 Loc:{locationNumber}", IPAddress.Parse(BSRightIp)));
            subLoc.ReplaceIp(IPAddress.Parse(utcIp_2));
            subLoc.inReturn_1_Btn.OnStateChanged += InReturn_1_Btn_OnStateChanged;
            occupiedTimeout = new DelayedEvent(TimeSpan.FromMilliseconds(3100.0), () => HandleCSD1());
            var debounceTimeout = TimeSpan.FromMilliseconds(1000.0);
            passThroughEval_csd2to3 = new DelayedEvent(debounceTimeout, Evaluate);
            passThroughEval_csd3toAcm = new DelayedEvent(debounceTimeout, Evaluate);
            passThroughEval_csd3to4 = new DelayedEvent(debounceTimeout, Evaluate);
            passThroughEval_csd6 = new DelayedEvent(debounceTimeout, Evaluate);
            GroupSoftEmergency(this, subLoc);
        }

        protected override void PreInit()
        {
            base.PreInit();
            subLoc = new SmallStorageSublocation("0.0.0.0", this);
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            AttachCsdEventHandlers(subLoc.csd4);
            AttachCsdEventHandlers(subLoc.csd5);
            AttachCsdEventHandlers(subLoc.csd6);
            csd1.OnStateChanged += Csd1_OnStateChanged;
            csd2.OnStateChanged += Csd2_OnStateChanged;
            subLoc.csd4.OnStateChanged += Csd4_OnStateChanged;
            subLoc.csd6.OnStateChanged += Csd6_OnStateChanged;
        }

        private void Csd1_OnStateChanged(CSD csd)
        {
            if (csd.State == SEGMENT_STATE.OCCUPIED)
                occupiedTimeout.Start();

            if (csd.State != SEGMENT_STATE.DISPATCHING || !csd.Scripts.DispatchNormal.IsRunning)
                return;
            
            passThroughEval_csd2to3.Start();
        }

        private void Csd2_OnStateChanged(CSD csd)
        {
            if (csd.State != SEGMENT_STATE.DISPATCHING || !csd.Scripts.DispatchNormal.IsRunning)
                return;
            
            passThroughEval_csd3toAcm.Start();
        }

        private void Csd4_OnStateChanged(CSD csd)
        {
            if (csd.State != SEGMENT_STATE.LOADING)
                return;
            
            passThroughEval_csd3to4.Start();
        }

        private void Csd6_OnStateChanged(CSD csd)
        {
            if (csd.State != SEGMENT_STATE.LOADING)
                return;
            
            passThroughEval_csd6.Start();
        }

        protected override void InitPins()
        {
            base.InitPins();
            var scripts1 = GetScripts(1U);
            scripts1.DownstreamStartLoading = null;
            scripts1.DispatchNormalSegmentOccupied = GetScripts(2U).OccupiedRollers;
            var scripts2 = GetScripts(2U);
            scripts2.LoadTriggerNormal = null;
            scripts2.DispatchNormalSegmentOccupied = GetScripts(3U).OccupiedRollers;
            scripts2.DownstreamStartLoading = null;
            scripts2.MiddleRollersDir = null;
            scripts2.MiddleRollersRun = MakeOut(PIN._23);
            scripts2.LoadTriggerAlternative = MakeIn(PIN._22);
            scripts2.UpstreamStartDispatching = MakeOut(PIN._21);
            var scripts3 = GetScripts(3U);
            scripts3.DispatchNormalSegmentOccupied = MakeIn(PIN._21).LowActive;
            scripts3.LoadTriggerNormal = null;
            scripts3.DownstreamStartLoading = MakeOut(PIN._20);
            scripts3.MiddleRollersDir = null;
            scripts3.MiddleRollersRun = MakeOut(PIN._23);
            scripts3.LoadTriggerAlternative = null;
            scripts3.DispatchAlternativeSegmentOccupied = MakeIn(PIN._23);
            scripts3.UpstreamStartDispatching = MakeOut(PIN._22);
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            switch (csdNum)
            {
                case 1:
                    var conditional1 = base.LoadNormalScript(csdNum);
                    Conditional conditional2 = MakeConditionalStatement($"Auto load precondition CSD:{csdNum}, Loc:{LocationNumber}", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                        .MakePrecondition().AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts.RollersRun, PIN_STATE.INACTIVE)
                        .AddCondition(scripts.LiftRun, PIN_STATE.INACTIVE).AddCondition(scripts.OccupiedBelts, PIN_STATE.INACTIVE).AddCondition(scripts.OccupiedRollers, PIN_STATE.INACTIVE)
                        .AddCondition(scripts.LoadTriggerNormal).CloseBlock();
                    MakeConditionalMacro($"Auto load script CSD:{csdNum}, Loc:{LocationNumber}", RUN_MODE.PERMANENTLY).AddStatement(conditional2).AddStatement(conditional1);
                    return conditional1;

                case 2:
                case 3:
                    return MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, middleMotorRun: scripts.MiddleRollersRun);
                
                default:
                    return null;
            }
        }

        protected override Conditional DispatchAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            if (csdNum == 1U)
                return MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.BeltsRun, scripts.OccupiedRollers, csdNum, endDelay: 1000U,
                    middleMotorRun: scripts.MiddleRollersRun);
            
            if (csdNum != 3U)
                return null;
            
            var beltsRun1 = scripts.BeltsRun;
            var beltsDir = scripts.BeltsDir;
            var beltsRun2 = scripts.BeltsRun;
            var occupiedRollers = scripts.OccupiedRollers;
            var csdNum1 = (int)csdNum;
            var middleRollersRun = scripts.MiddleRollersRun;
            var startDispatching = scripts.UpstreamStartDispatching;
            var middleMotorRun = middleRollersRun;
            
            return MakeDispatchStatement(beltsRun1, TABLE_POSITION.UP, beltsDir, MOTOR_DIR.CCW, beltsRun2, occupiedRollers, (uint)csdNum1, nextSegLoad: startDispatching, endDelay: 1000U,
                middleMotorRun: middleMotorRun);
        }

        protected override Conditional LoadAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            return csdNum == 2U
                ? MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedBelts, csdNum, prevSegDispatch: scripts.UpstreamStartDispatching,
                    endDelay: 750U)
                : null;
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            if (csdNum != 3U)
                return base.DispatchNormalScript(csdNum);
            
            var scripts = GetScripts(csdNum);
            
            return MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.DispatchNormalSegmentOccupied, scripts.OccupiedRollers, csdNum,
                timeOut: 15000U, nextSegLoad: scripts.DownstreamStartLoading);
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            var route = csd1.Route;
            
            if (!barcode.Equals(route?.Barcode) || route.Destination != DESTINATION.TBD)
                return;
            
            switch (target)
            {
                case WMS_TOTE_DIRECTION.DIRECTION_1:
                    route.Destination = DESTINATION.DISPATCH_CSD1;
                    break;
            
                case WMS_TOTE_DIRECTION.DIRECTION_2:
                    route.Destination = DESTINATION.DISPATCH_CSD2;
                    break;
                
                case WMS_TOTE_DIRECTION.DIRECTION_3:
                    route.Destination = DESTINATION.DISPATCH_CSD2_ALT;
                    break;
                
                case WMS_TOTE_DIRECTION.DIRECTION_4:
                    route.Destination = DESTINATION.DISPATCH_CSD1_ALT;
                    break;
            }

            Evaluate();
        }

        protected override void DispatchWmsFeedback(Route route)
        {
            if (string.IsNullOrWhiteSpace(route?.Barcode))
                return;
            
            var direction = WMS_TOTE_DIRECTION.DIRECTION_1;
            
            switch (route.Destination)
            {
                case DESTINATION.DISPATCH_CSD2:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_2;
                    break;
            
                case DESTINATION.DISPATCH_CSD2_ALT:
                    direction = WMS_TOTE_DIRECTION.DIRECTION_3;
                    break;
            }

            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(direction, Encoding.ASCII.GetBytes(route.Barcode), LocationNumber)));
            NdwConnectCommunicator.DirectionSentUpdate(LocationNumber, route.Barcode, direction);
        }

        public override void DoEvaluate()
        {
            subLoc.csd4.CheckValid();
            subLoc.csd5.CheckValid();
            subLoc.csd6.CheckValid();
            subLoc.csd4.InvalidateRoute();
            subLoc.csd5.InvalidateRoute();
            subLoc.csd6.InvalidateRoute();
            HandleCSD1();
            HandleCSD2();
            HandleCSD3();
            HandleCSD4();
            HandleCSD5();
            HandleCSD6();
        }

        private void HandleCSD1()
        {
            GetScripts(1U);
            var route = csd1.Route;
            var to = route?.Destination ?? DESTINATION.TBD;
            
            switch (csd1.State)
            {
                case SEGMENT_STATE.LOADING_PENDING:
                case SEGMENT_STATE.LOADING:
                    if (csd1.Route == null)
                        csd1.Route = new Route(START.LOAD_CSD1);
            
                    if (to == DESTINATION.TBD)
                        break;
                    
                    Dispatch_CSD1(to);
                    break;
                
                case SEGMENT_STATE.OCCUPIED:
                    if (csd1.StateAge > TimeSpan.FromMilliseconds(WaitForWmsFeedbackMs) && (uint)to > 1U)
                        to = DESTINATION.DISPATCH_CSD1;
                
                    if (to == DESTINATION.TBD)
                        break;
                    
                    Dispatch_CSD1(to);
                    break;
            }
        }

        private void HandleCSD2()
        {
            var scripts = GetScripts(2U);
            
            switch (csd2.State)
            {
                case SEGMENT_STATE.IDLE:
                    csd2.Route = null;
            
                    if (!scripts.LoadTriggerAlternative.Active)
                        break;
                    
                    csd2.LoadAlternative();
                    break;
                
                case SEGMENT_STATE.LOADING:
                    if (csd2.Scripts.LoadAlternative.IsRunning)
                    {
                        csd2.Route = new Route(START.LOAD_CSD2, DESTINATION.DISPATCH_CSD1);
                        break;
                    }

                    if (!csd3.IsIdle || csd1.State != SEGMENT_STATE.DISPATCHING || csd1.StateAge < TimeSpan.FromMilliseconds(1000.0) || !csd3.LoadNormal())
                        break;
                
                    csd2.PassThrough();
                    csd3.Route = csd2.Route;
                    break;

                case SEGMENT_STATE.OCCUPIED:
                    if (csd3.State != SEGMENT_STATE.IDLE || !csd3.LoadNormal())
                        break;
                
                    csd2.DispatchNormal();
                    csd3.Route = csd2.Route;
                    break;
            }
        }

        private void HandleCSD3()
        {
            var scripts = GetScripts(3U);
            
            switch (csd3.State)
            {
                case SEGMENT_STATE.IDLE:
                    csd3.Route = null;
                    break;
            
                case SEGMENT_STATE.LOADING:
                    if (!csd3.Scripts.LoadNormal.IsRunning || !scripts.DispatchNormalSegmentOccupied.Inactive || csd2.State != SEGMENT_STATE.DISPATCHING ||
                        csd2.StateAge < TimeSpan.FromMilliseconds(1000.0))
                        break;
                
                    var route = csd3.Route;
                    
                    if ((route != null ? route.Destination != DESTINATION.DISPATCH_CSD1_ALT ? 1 : 0 : 1) == 0)
                        break;
                    
                    csd3.PassThrough();
                    break;
                
                case SEGMENT_STATE.OCCUPIED:
                    var destination1 = csd3.Route?.Destination;
                    var destination2 = DESTINATION.DISPATCH_CSD1_ALT;
                
                    if ((destination1.GetValueOrDefault() == destination2) & destination1.HasValue)
                    {
                        if (scripts.DispatchAlternativeSegmentOccupied.Inactive)
                        {
                            csd3.DispatchAlternative();
                            break;
                        }

                        if (csd3.StateAge < TimeSpan.FromMilliseconds(WaitForWmsFeedbackMs))
                        {
                            ReEvaluate.Start();
                            break;
                        }
                    }

                    if (scripts.DispatchNormalSegmentOccupied.Inactive && csd3.DispatchNormal())
                        break;
                    
                    ReEvaluate.Start();
                    break;
            }
        }

        private void HandleCSD4()
        {
            var csd4 = subLoc.csd4;
            var scripts = csd4.Scripts;
            var route = csd4.Route;
            var flag = (route != null ? (int)route.Destination : 3) == 4;
            
            switch (subLoc.csd4.State)
            {
                case SEGMENT_STATE.LOADING:
                    if (!flag || csd4.StateAge < TimeSpan.FromMilliseconds(1000.0) || !subLoc.csd5.IsIdle || !subLoc.csd5.LoadAlternative())
                        break;
                    subLoc.csd4.PassThrough();
                    break;
            
                case SEGMENT_STATE.OCCUPIED:
                    if (flag)
                    {
                        if (!subLoc.csd5.IsIdle || !subLoc.csd5.LoadAlternative() || !subLoc.csd4.DispatchAlternative())
                            break;
                        subLoc.csd4.Route = null;
                        break;
                    }

                    if (!scripts.DispatchNormalSegmentOccupied.Inactive || !subLoc.csd4.DispatchNormal())
                        break;
                
                    subLoc.csd4.Route = null;
                    break;
            }
        }

        private void HandleCSD5()
        {
            if (subLoc.csd5.State != SEGMENT_STATE.OCCUPIED || !subLoc.GetScripts(2U).DispatchNormalSegmentOccupied.Inactive)
                return;

            subLoc.csd5.DispatchNormal();
        }

        private void HandleCSD6()
        {
            var csd6 = subLoc.csd6;
            var scripts = csd6.Scripts;
            
            switch (csd6.State)
            {
                case SEGMENT_STATE.IDLE:
                    if (!subLoc.inReturn_1_Btn.Inactive || buttonPressPending)
                    {
                        buttonPressPending = false;
                        csd6.LoadNormal();
                        break;
                    }

                    if (scripts.LoadTriggerAlternative.Inactive)
                        break;
            
                    if (!scripts.DispatchAlternativeSegmentOccupied.Inactive)
                    {
                        csd6.LoadAlternative();
                        break;
                    }

                    csd6.PassThrough();
                    break;
                
                case SEGMENT_STATE.LOADING:
                    if (!scripts.LoadAlternative.IsRunning || !scripts.DispatchAlternativeSegmentOccupied.Inactive)
                        break;
                    csd6.PassThrough();
                    break;
                
                case SEGMENT_STATE.OCCUPIED:
                    if (!scripts.DispatchAlternativeSegmentOccupied.Inactive)
                        break;
                    csd6.DispatchAlternative();
                    break;
            }
        }

        private void Dispatch_CSD1(DESTINATION to)
        {
            var route = csd1.Route ?? new Route(START.LOAD_CSD1, to);
            route.Destination = to;
            
            switch (to)
            {
                case DESTINATION.DISPATCH_CSD1:
                case DESTINATION.DISPATCH_CSD1_ALT:
                    if (!csd2.IsIdle)
                        return;
            
                    if (csd1.IsOccupied)
                    {
                        csd2.Route = route;
                    
                        if (!csd2.LoadNormal() || !csd1.DispatchNormal())
                            return;
                        
                        break;
                    }

                    if (Helpers.Contains(csd1.State, SEGMENT_STATE.LOADING_PENDING, SEGMENT_STATE.LOADING))
                    {
                        csd2.Route = route;
                        
                        if (!csd2.LoadNormal() || !csd1.PassThrough())
                            return;
                    }

                    break;

                case DESTINATION.DISPATCH_CSD2:
                case DESTINATION.DISPATCH_CSD2_ALT:
                    if (!subLoc.csd4.IsIdle || (route.Destination == DESTINATION.DISPATCH_CSD2_ALT && !subLoc.csd5.IsIdle) || !csd1.IsOccupied)
                        return;
                
                    subLoc.csd4.Route = route;
                    
                    if (!subLoc.csd4.LoadAlternative() || !csd1.DispatchAlternative())
                        return;
                    
                    break;
                
                default:
                    throw new ArgumentException("Illegal dispatch destination");
            }

            if (csd1.Route != null)
                DispatchWmsFeedback(csd1.Route);

            if (occupiedTimeout.Running)
                occupiedTimeout.Stop();
            
            csd1.Route = null;
        }

        protected override void OnBarcodeScanned(string barcode)
        {
            if (csd1.Route == null)
                Log("Rx barcode, but ROUTE IS NULL");
            else
                base.OnBarcodeScanned(barcode);
        }

        protected override void RequestWmsDirection(string barcode)
        {
            base.RequestWmsDirection(barcode);
            csd1.Route.Barcode = barcode;
        }

        private void InReturn_1_Btn_OnStateChanged(InPin pin)
        {
            if (!pin.Active)
                return;
            
            buttonPressPending = true;
            TriggerEvaluate(pin);
        }
    }
}