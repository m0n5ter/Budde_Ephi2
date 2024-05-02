// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.WeighingZone_Enter
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System.Net;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;

namespace PharmaProject.Locations
{
    public class WeighingZone_Enter : BaseLocation
    {
        private Conditional csd2AutoLoad;
        private Conditional dispatchFromScale;
        private InPin inBtnReleaseInside;
        private InPin inBtnReleaseOutside;
        private InPin inCsd2LoadNormalOccupied;
        private InPin inCsd2OccupiedAltFromInside;
        private InPin inCsd2OccupiedAltFromOutside;
        private InPin inPostScaleAcmOccupied;
        private InPin inScaleOccupied;
        private bool insideReworkDispatchRequested;
        private string lastBarcode;
        private Conditional loadAltInside;
        private Conditional loadAltOutside;
        private Conditional LoadToScale;
        private OutPin outPostScaleAcmLoad;
        private OutPin outScaleRun;
        private bool outsideReworkDispatchRequested;

        public WeighingZone_Enter(string utcIp, uint locationNumber, string bs1Ip, string bs2Ip)
            : base(utcIp, locationNumber, 3U)
        {
            AddBarcodeScanner(new BarcodeScanner(string.Format("bs1 Loc:{0}", locationNumber), IPAddress.Parse(bs1Ip)));
            AddBarcodeScanner(new BarcodeScanner(string.Format("bs2 Loc:{0}", locationNumber), IPAddress.Parse(bs2Ip)));
        }

        public string LastBarcode
        {
            get => lastBarcode;
            set
            {
                if (lastBarcode == value)
                    return;
                lastBarcode = value;
                if (string.IsNullOrEmpty(value))
                    Log("Last barcode cleared");
                else
                    Log(string.Format("Last barcode set to {0}", value));
            }
        }

        protected override InPin[] ResetEmergencyPins
        {
            get
            {
                return new InPin[2]
                {
                    inBtnReleaseInside,
                    inBtnReleaseOutside
                };
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
            inBtnReleaseInside = MakeIn(PIN._14);
            inBtnReleaseOutside = MakeIn(PIN._24);
            inCsd2OccupiedAltFromOutside = MakeIn(PIN._10);
            inCsd2OccupiedAltFromInside = MakeIn(PIN._9);
            outScaleRun = MakeOut(PIN._24);
            outPostScaleAcmLoad = MakeOut(PIN._20);
            inPostScaleAcmOccupied = MakeIn(PIN._21).LowActive;
            inScaleOccupied = MakeIn(PIN._15);
            inCsd2LoadNormalOccupied = MakeIn(PIN._8);
            inBtnReleaseInside.OnStateChanged += InBtnReleaseInside_OnStateChanged;
            inBtnReleaseOutside.OnStateChanged += InBtnReleaseOutside_OnStateChanged;
            var scripts1 = GetScripts(2U);
            scripts1.LoadTriggerNormal = MakeIn(PIN._20);
            scripts1.UpstreamStartDispatching = MakeOut(PIN._19);
            scripts1.DispatchNormalSegmentOccupied = inScaleOccupied;
            scripts1.DownstreamStartLoading = outScaleRun;
            scripts1.OccupiedRollers = inCsd2LoadNormalOccupied;
            scripts1.OccupiedExtra = inCsd2OccupiedAltFromOutside;
            var scripts2 = GetScripts(1U);
            scripts2.LoadTriggerNormal = MakeIn(PIN._22).LowActive;
            scripts2.UpstreamStartDispatching = MakeOut(PIN._21);
            scripts2.DispatchAlternativeSegmentOccupied = GetScripts(2U).OccupiedBelts;
            scripts2.DownstreamStartLoading = OutPin.Dummy;
            scripts2.MiddleRollersRun = MakeOut(PIN._23);
            scripts2.OccupiedBelts = null;
            var scripts3 = GetScripts(3U);
            scripts3.LoadTriggerNormal = MakeIn(PIN._23).LowActive;
            scripts3.UpstreamStartDispatching = MakeOut(PIN._22);
            scripts3.DispatchAlternativeSegmentOccupied = GetScripts(2U).OccupiedRollers;
            scripts3.DownstreamStartLoading = OutPin.Dummy;
            scripts3.MiddleRollersRun = MakeOut(PIN._23);
            scripts3.OccupiedBelts = null;
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var scripts = GetScripts(2U);
            loadAltInside = base.LoadAlternativeScript(2U);
            loadAltOutside = MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, inCsd2OccupiedAltFromOutside, 2U, middleMotorRun: scripts.MiddleRollersRun);
            dispatchFromScale = MakeConditionalStatement(string.Format("Dispatch from scale (Loc:{0})", LocId), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(15000U)
                .AddOutputState(outScaleRun).AddOutputState(outPostScaleAcmLoad).AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(inScaleOccupied, PIN_STATE.INACTIVE).AddGuardBlock(1000U)
                .AddGuardPin(inScaleOccupied).CloseBlock().CloseBlock();
            LoadToScale = MakeConditionalStatement(string.Format("Load to scale (Loc:{0})", LocId), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(7000U).AddOutputState(outScaleRun)
                .AddCondition(inScaleOccupied);
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            loadAltInside.OnStateChanged += csd2.Load_OnStateChanged;
            loadAltOutside.OnStateChanged += csd2.Load_OnStateChanged;
            inScaleOccupied.OnStateChanged += TriggerEvaluate;
            inPostScaleAcmOccupied.OnStateChanged += TriggerEvaluate;
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (csdNum == 1U)
                return MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, prevSegDispatch: scripts.UpstreamStartDispatching,
                    endDelay: 300U);
            if (csdNum != 2U)
                return MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CCW, scripts.OccupiedRollers, csdNum, prevSegDispatch: scripts.UpstreamStartDispatching,
                    endDelay: 300U);
            Conditional conditional1 = MakeConditionalStatement(string.Format("Auto load precondition CSD:{0}, Loc:{1}", csdNum, LocationNumber), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                .MakePrecondition().AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts.RollersRun, PIN_STATE.INACTIVE)
                .AddCondition(scripts.LiftRun, PIN_STATE.INACTIVE).AddCondition(inCsd2LoadNormalOccupied, PIN_STATE.INACTIVE).AddCondition(inCsd2OccupiedAltFromInside, PIN_STATE.INACTIVE)
                .AddCondition(inCsd2OccupiedAltFromOutside, PIN_STATE.INACTIVE).AddCondition(scripts.LoadTriggerNormal).CloseBlock();
            var conditional2 = MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CCW, inCsd2LoadNormalOccupied, csdNum,
                prevSegDispatch: scripts.UpstreamStartDispatching);
            csd2AutoLoad = MakeConditionalMacro(string.Format("Auto load script CSD:{0}, Loc:{1}", csdNum, LocationNumber)).AddStatement(conditional1).AddStatement(conditional2);
            return conditional2;
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            if (csdNum != 2U)
                return null;
            var scripts = GetScripts(csdNum);
            return MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CCW, scripts.RollersRun, scripts.OccupiedRollers, csdNum);
        }

        protected override Conditional LoadAlternativeScript(uint csdNum)
        {
            return null;
        }

        protected override Conditional DispatchAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 1U || csdNum == 3U
                ? MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.DispatchAlternativeSegmentOccupied, scripts.OccupiedRollers, csdNum,
                    middleMotorRun: scripts.MiddleRollersRun)
                : null;
        }

        public override void DoEvaluate()
        {
            Handle_Csd1();
            Handle_Csd2();
            Handle_Csd3();
            HandleScale();
        }

        private void Handle_Csd1()
        {
            var scripts = GetScripts(1U);
            if (csd1.State == CSD_STATE.IDLE)
            {
                if (!outsideReworkDispatchRequested && !inBtnReleaseOutside.Active)
                    return;
                if (!scripts.LoadTriggerNormal.Active)
                {
                    outsideReworkDispatchRequested = false;
                }
                else
                {
                    if (!csd1.LoadNormal())
                        return;
                    outsideReworkDispatchRequested = false;
                }
            }
        }

        private void Handle_Csd2()
        {
            var scripts = GetScripts(2U);
            switch (csd2.State)
            {
                case CSD_STATE.IDLE:
                    LastBarcode = string.Empty;
                    if (!csd2.IsIdle)
                        break;
                    var flag = csd1.IsOccupied || csd3.IsOccupied;
                    if (scripts.LoadTriggerNormal.Active || !flag)
                    {
                        csd2AutoLoad.Run();
                        break;
                    }

                    csd2AutoLoad.Cancel();
                    if (csd1.IsOccupied)
                    {
                        if (!loadAltOutside.Run())
                            break;
                        csd2.ForceLoadPending();
                        csd1.DispatchAlternative();
                        LastBarcode = null;
                        break;
                    }

                    if (!csd3.IsOccupied || !loadAltInside.Run())
                        break;
                    csd2.ForceLoadPending();
                    csd3.DispatchAlternative();
                    LastBarcode = null;
                    break;
                case CSD_STATE.LOADING:
                    if (!csd2.Scripts.LoadNormal.IsRunning)
                        break;
                    DispatchToScale();
                    break;
                case CSD_STATE.OCCUPIED:
                    DispatchToScale();
                    break;
                case CSD_STATE.DISPATCHING:
                    if (!string.IsNullOrEmpty(LastBarcode))
                        base.RequestWmsDirection(LastBarcode);
                    LastBarcode = string.Empty;
                    break;
            }
        }

        private void Handle_Csd3()
        {
            var scripts = GetScripts(3U);
            if (csd3.State == CSD_STATE.IDLE)
            {
                if (!insideReworkDispatchRequested && !inBtnReleaseInside.Active)
                    return;
                if (!scripts.LoadTriggerNormal.Active)
                {
                    insideReworkDispatchRequested = false;
                }
                else
                {
                    if (!csd3.LoadNormal())
                        return;
                    insideReworkDispatchRequested = false;
                }
            }
        }

        private void HandleScale()
        {
            if (!inPostScaleAcmOccupied.Inactive || (!inScaleOccupied.Active && !csd2.Scripts.DispatchNormal.IsRunning))
                return;
            dispatchFromScale.Run();
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
        }

        protected override void RequestWmsDirection(string barcode)
        {
            if (LoadToScale.IsRunningOrAboutToBe)
            {
                base.RequestWmsDirection(barcode);
                LastBarcode = string.Empty;
            }
            else
            {
                switch (csd2.State)
                {
                    case CSD_STATE.LOADING_PENDING:
                    case CSD_STATE.LOADING:
                    case CSD_STATE.OCCUPIED:
                        LastBarcode = barcode;
                        MessageSent();
                        return;
                    case CSD_STATE.DISPATCHING_PENDING:
                    case CSD_STATE.DISPATCHING:
                        base.RequestWmsDirection(barcode);
                        break;
                }

                LastBarcode = string.Empty;
            }
        }

        private void DispatchToScale()
        {
            if (!inScaleOccupied.Inactive || LoadToScale.IsRunning)
                return;
            if (csd2.IsOccupied)
            {
                if (!csd2.DispatchNormal())
                    return;
            }
            else if (!csd2.PassThrough() && !csd2.DispatchNormal())
            {
                return;
            }

            LoadToScale.Run();
            if (!string.IsNullOrEmpty(LastBarcode))
                base.RequestWmsDirection(LastBarcode);
            LastBarcode = string.Empty;
        }

        private void InBtnReleaseOutside_OnStateChanged(InPin obj)
        {
            if (!obj.Active)
                return;
            outsideReworkDispatchRequested = true;
            Evaluate();
        }

        private void InBtnReleaseInside_OnStateChanged(InPin obj)
        {
            if (!obj.Active)
                return;
            insideReworkDispatchRequested = true;
            Evaluate();
        }
    }
}