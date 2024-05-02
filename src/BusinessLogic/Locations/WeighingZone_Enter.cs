// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Locations.WeighingZone_Enter
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using System.Net;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Devices;
using PharmaProject.BusinessLogic.Misc;

namespace PharmaProject.BusinessLogic.Locations
{
    public class WeighingZone_Enter : BaseLocation
    {
        private const int WEIGHING_FEEDBACK_TIMEOUT_ms = 5000;
        private string barcodeOnScale;
        private DateTime barcodeOnScaleSet = DateTime.Now;
        private string barcodeToScale;
        private Conditional csd2AutoLoad;
        private Conditional dispatchFromCsdToScale;
        private Conditional dispatchFromScale;
        private Conditional dispatchReboundCsd1;
        private Conditional dispatchReboundCsd3;
        private InPin inBtnReleaseInside;
        private InPin inBtnReleaseOutside;
        private InPin inCsd2LoadNormalOccupied;
        private InPin inCsd2OccupiedAltFromInside;
        private InPin inCsd2OccupiedAltFromOutside;
        private InPin inPostScaleAcmOccupied;
        private InPin inScaleOccupied;
        private bool insideReworkDispatchRequested;
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

        public string BarcodeToScale
        {
            get => barcodeToScale;
            set
            {
                if (barcodeToScale == value)
                    return;
                barcodeToScale = value;
                if (string.IsNullOrEmpty(value))
                    Log("To-scale barcode cleared");
                else
                    Log(string.Format("To-Scale barcode set to {0}", value));
            }
        }

        public TimeSpan BarcodeOnScaleAge => DateTime.Now.Subtract(barcodeOnScaleSet);

        public string BarcodeOnScale
        {
            get => barcodeOnScale;
            set
            {
                if (barcodeOnScale == value)
                    return;
                barcodeOnScale = value;
                barcodeOnScaleSet = DateTime.Now;
                if (string.IsNullOrEmpty(value))
                    Log("On-scale barcode cleared");
                else
                    Log(string.Format("On-Scale barcode set to {0}", value));
                Evaluate();
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
            var name = string.Format("Dispatch from CSD to scale (Loc:{0}) ", LocId);
            dispatchFromCsdToScale = MakeConditionalBatch(name).AddGlobalTimeout(TIMEOUT_RANGE.TR_SEC, 10)
                .AddStatement(MakeConditionalStatement(name + "a", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddOutputState(outScaleRun).AddCondition(inPostScaleAcmOccupied))
                .AddStatement(MakeConditionalStatement(name + "b", OUTPUT_ENFORCEMENT.ENF_AT_CONDITION_TRUE).AddOutputState(outPostScaleAcmLoad).AddCondition(inScaleOccupied));
            dispatchReboundCsd1 = MakeReboundDispatchScript(1U);
            dispatchReboundCsd3 = MakeReboundDispatchScript(3U);

            Conditional MakeReboundDispatchScript(uint csdNum)
            {
                return MakeConditionalStatement(string.Format("Rebound CSD:{0} Dispatch", csdNum), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddTimeoutCondition(TIMEOUT_RANGE.TR_100MS, 2)
                    .AddOutputState(GetScripts(csdNum).UpstreamStartDispatching);
            }
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            loadAltInside.OnStateChanged += csd2.Load_OnStateChanged;
            loadAltOutside.OnStateChanged += csd2.Load_OnStateChanged;
            dispatchFromCsdToScale.OnStateChanged += csd2.Dispatch_OnStateChanged;
            dispatchFromCsdToScale.OnStateChanged += Script_OnStateChanged;
            inScaleOccupied.OnStateChanged += TriggerEvaluate;
            inPostScaleAcmOccupied.OnStateChanged += TriggerEvaluate;
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (csdNum == 1U)
                return MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, endDelay: 300U);
            if (csdNum != 2U)
                return MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CCW, scripts.OccupiedRollers, csdNum, endDelay: 300U);
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
        }

        private void Handle_Csd1()
        {
            var scripts = GetScripts(1U);
            if (csd1.State == SEGMENT_STATE.IDLE)
            {
                if (!outsideReworkDispatchRequested)
                    return;
                if (!scripts.LoadTriggerNormal.Active)
                {
                    outsideReworkDispatchRequested = false;
                }
                else
                {
                    if (!csd1.LoadNormal())
                        return;
                    dispatchReboundCsd1.Run();
                    outsideReworkDispatchRequested = false;
                }
            }
        }

        private void Handle_Csd2()
        {
            var scripts = GetScripts(2U);
            switch (csd2.State)
            {
                case SEGMENT_STATE.IDLE:
                    BarcodeToScale = string.Empty;
                    break;
                case SEGMENT_STATE.LOADING_PENDING:
                    return;
                case SEGMENT_STATE.DISPATCHING_PENDING:
                    return;
                case SEGMENT_STATE.LOADING:
                    if (csd2.Scripts.LoadNormal.IsRunning && !string.IsNullOrEmpty(BarcodeToScale))
                    {
                        DispatchFromCSDToPostScale();
                        return;
                    }

                    break;
                case SEGMENT_STATE.OCCUPIED:
                    DispatchFromCSDToPostScale();
                    return;
                case SEGMENT_STATE.DISPATCHING:
                    SendAndClearBarcode();
                    return;
                default:
                    return;
            }

            if (!csd2.IsIdle)
                return;
            var flag = csd1.IsOccupied || csd3.IsOccupied;
            if (scripts.LoadTriggerNormal.Active && !flag)
            {
                csd2AutoLoad.Run();
            }
            else
            {
                csd2AutoLoad.Cancel();
                if (csd1.IsOccupied)
                {
                    if (!loadAltOutside.Run())
                        return;
                    csd2.ForceLoadPending();
                    csd1.DispatchAlternative();
                    BarcodeToScale = string.Empty;
                }
                else
                {
                    if (!csd3.IsOccupied || !loadAltInside.Run())
                        return;
                    csd2.ForceLoadPending();
                    csd3.DispatchAlternative();
                    BarcodeToScale = string.Empty;
                }
            }
        }

        private void Handle_Csd3()
        {
            var scripts = GetScripts(3U);
            if (csd3.State == SEGMENT_STATE.IDLE)
            {
                if (!insideReworkDispatchRequested)
                    return;
                if (!scripts.LoadTriggerNormal.Active)
                {
                    insideReworkDispatchRequested = false;
                }
                else
                {
                    if (!csd3.LoadNormal())
                        return;
                    dispatchReboundCsd3.Run();
                    insideReworkDispatchRequested = false;
                }
            }
        }

        private void HandleScale()
        {
            if (!inPostScaleAcmOccupied.Inactive || (!inScaleOccupied.Active && !csd2.Scripts.DispatchNormal.IsRunning))
                return;
            if (!string.IsNullOrEmpty(BarcodeOnScale))
            {
                if (BarcodeOnScaleAge < TimeSpan.FromMilliseconds(5000.0))
                {
                    ReEvaluate.Start();
                    return;
                }

                log.WarnFormat("WMS responded too late for weighing barcode {0}", BarcodeOnScale);
            }

            dispatchFromScale.Run();
            BarcodeOnScale = string.Empty;
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            log.InfoFormat("Received weighing feedback for barcode:{0}, on scale barcode:{1}", barcode, BarcodeToScale);
            if (!barcode.Equals(BarcodeOnScale))
                return;
            BarcodeOnScale = string.Empty;
        }

        protected override void OnNoRead()
        {
            if (!SendAllowed)
                return;
            NoReadSent();
            if (!string.IsNullOrEmpty(BarcodeToScale))
                return;
            BarcodeToScale = "NoRead";
        }

        protected override void RequestWmsDirection(string barcode)
        {
            BarcodeSent();
            BarcodeToScale = barcode;
            Log(string.Format("Weighing barcode preset: {0}", barcode));
        }

        private void DispatchFromCSDToPostScale()
        {
            if (!inPostScaleAcmOccupied.Inactive || dispatchFromCsdToScale.IsRunning)
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

            dispatchFromCsdToScale.Run();
            SendAndClearBarcode();
        }

        private void SendAndClearBarcode()
        {
            if (string.IsNullOrEmpty(BarcodeToScale))
                return;
            log.InfoFormat("Barcode: {0} sent to WMS", BarcodeToScale);
            base.RequestWmsDirection(BarcodeToScale);
            BarcodeOnScale = BarcodeToScale;
            BarcodeToScale = string.Empty;
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