using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Misc;

namespace PharmaProject.BusinessLogic.Locations
{
    public class PackingBelowLocation : BaseLocation
    {
        private OutPin csd1_leftDispatch;
        private InPin csd1_leftTrigger;
        private OutPin csd1_rightDispatch;
        private InPin csd1_rightTrigger;
        private LOAD_DIRECTION csd1LastLoaded;
        private OutPin csd2_leftDispatch;
        private InPin csd2_leftTrigger;
        private OutPin csd2_rightDispatch;
        private InPin csd2_rightTrigger;
        private LOAD_DIRECTION csd2LastLoaded;
        private InPin inBtn_LA;
        private InPin inBtn_LV;
        private InPin inBtn_RA;
        private InPin inBtn_RV;
        private Conditional loadCsd1Left;
        private Conditional loadCsd1Right;
        private Conditional loadCsd2Left;
        private Conditional loadCsd2Right;
        private OutPin outLoad_LA;
        private OutPin outLoad_LV;
        private OutPin outLoad_RA;
        private OutPin outLoad_RV;

        public PackingBelowLocation(string IP, uint locationNumber)
            : base(IP, locationNumber, 2U)
        {
        }

        protected override InPin[] ResetEmergencyPins
        {
            get
            {
                return new[]
                {
                    inBtn_LV,
                    inBtn_RV,
                    inBtn_LA,
                    inBtn_RA
                };
            }
        }

        protected override void InitPins()
        {
            base.InitPins();
            inBtn_LV = MakeIn(PIN._24);
            inBtn_RV = MakeIn(PIN._19);
            inBtn_LA = MakeIn(PIN._15);
            inBtn_RA = MakeIn(PIN._23).HighActive;
            outLoad_LV = MakeOut(PIN._24);
            outLoad_RV = MakeOut(PIN._22);
            outLoad_LA = MakeOut(PIN._18);
            outLoad_RA = MakeOut(PIN._23);
            csd1_leftTrigger = MakeIn(PIN._13);
            csd1_rightTrigger = MakeIn(PIN._14);
            csd2_leftTrigger = MakeIn(PIN._11);
            csd2_rightTrigger = MakeIn(PIN._12);
            csd1_leftDispatch = MakeOut(PIN._15);
            csd1_rightDispatch = MakeOut(PIN._16);
            csd2_leftDispatch = MakeOut(PIN._13);
            csd2_rightDispatch = MakeOut(PIN._14);
            var scripts1 = GetScripts(1U);
            scripts1.DownstreamStartLoading = MakeOut(PIN._19);
            scripts1.DispatchNormalSegmentOccupied = MakeIn(PIN._20);
            scripts1.BeltsDir = scripts1.BeltsDir.LowActive;
            var scripts2 = GetScripts(2U);
            scripts2.UpstreamStartDispatching = MakeOut(PIN._20);
            scripts2.DownstreamStartLoading = MakeOut(PIN._21);
            scripts2.LoadTriggerNormal = MakeIn(PIN._21).LowActive;
            scripts2.DispatchNormalSegmentOccupied = MakeIn(PIN._22).LowActive;
            scripts2.BeltsDir = scripts2.BeltsDir.LowActive;
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            MakeEnterScript(inBtn_LV, outLoad_LV, "LV");
            MakeEnterScript(inBtn_RV, outLoad_RV, "RV");
            MakeEnterScript(inBtn_LA, outLoad_LA, "LA");
            MakeEnterScript(inBtn_RA, outLoad_RA, "RA");
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            loadCsd1Left = MakeLoadStatement(scripts1.BeltsRun, TABLE_POSITION.UP, scripts1.BeltsDir, MOTOR_DIR.CCW, scripts1.OccupiedRollers, 1U, prevSegDispatch: csd1_leftDispatch);
            loadCsd1Right = MakeLoadStatement(scripts1.BeltsRun, TABLE_POSITION.UP, scripts1.BeltsDir, MOTOR_DIR.CW, scripts1.OccupiedBelts, 1U, prevSegDispatch: csd1_rightDispatch);
            loadCsd2Left = MakeLoadStatement(scripts2.BeltsRun, TABLE_POSITION.UP, scripts2.BeltsDir, MOTOR_DIR.CCW, scripts2.OccupiedRollers, 2U, prevSegDispatch: csd2_leftDispatch);
            loadCsd2Right = MakeLoadStatement(scripts2.BeltsRun, TABLE_POSITION.UP, scripts2.BeltsDir, MOTOR_DIR.CW, scripts2.OccupiedBelts, 2U, prevSegDispatch: csd2_rightDispatch);
        }

        private Conditional MakeEnterScript(InPin btn, OutPin load, string id)
        {
            return MakeConditionalStatement($"Load ACM section {id} ", OUTPUT_ENFORCEMENT.ENF_NEGATE_WHEN_TRUE, RUN_MODE.PERMANENTLY).AddLogicBlock(LOGIC_FUNCTION.AND)
                .AddCondition(btn, PIN_STATE.INACTIVE).AddGuardBlock(100U).AddGuardPin(btn).CloseBlock().CloseBlock().AddOutputState(load);
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            if (csdNum != 2U)
                return null;

            var scripts = GetScripts(csdNum);
            
            return scripts == null
                ? null
                : MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.DispatchNormalSegmentOccupied, csdNum,
                    prevSegDispatch: scripts.UpstreamStartDispatching, middleMotorRun: scripts.DownstreamStartLoading);
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            return scripts != null
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CCW, scripts.DispatchNormalSegmentOccupied, scripts.OccupiedRollers, csdNum,
                    nextSegLoad: scripts.DownstreamStartLoading)
                : null;
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            csd1_leftTrigger.OnStateChanged += TriggerEvaluate;
            csd1_rightTrigger.OnStateChanged += TriggerEvaluate;
            csd2_leftTrigger.OnStateChanged += TriggerEvaluate;
            csd2_rightTrigger.OnStateChanged += TriggerEvaluate;
            loadCsd1Left.OnStateChanged += csd1.Load_OnStateChanged;
            loadCsd1Right.OnStateChanged += csd1.Load_OnStateChanged;
            loadCsd2Left.OnStateChanged += csd2.Load_OnStateChanged;
            loadCsd2Right.OnStateChanged += csd2.Load_OnStateChanged;
        }

        public override void DoEvaluate()
        {
            EvaluateCsd1();
            EvaluateCsd2();
        }

        private void EvaluateCsd1()
        {
            switch (csd1.State)
            {
                case SEGMENT_STATE.IDLE:
                    var num1 = 2;
                    var num2 = (int)(csd1LastLoaded + 1);
            
                    for (var index = num2; index < num2 + num1; ++index)
                        switch ((LOAD_DIRECTION)(index % num1))
                        {
                            case LOAD_DIRECTION.RIGHT:
                                if (csd1_rightTrigger.Active)
                                    goto default;
                                break;
                    
                            case LOAD_DIRECTION.LEFT:
                                if (!csd1_leftTrigger.Active)
                                    break;
                                goto default;
                            
                            default:
                                LoadCsd1((LOAD_DIRECTION)(index % num1));
                                return;
                        }

                    break;

                case SEGMENT_STATE.OCCUPIED:
                    if (!csd1.Scripts.DispatchNormalSegmentOccupied.Inactive)
                        break;
                
                    csd1.DispatchNormal();
                    break;
            }
        }

        private void EvaluateCsd2()
        {
            switch (csd2.State)
            {
                case SEGMENT_STATE.IDLE:
                    var num1 = 3;
                    var num2 = (int)(csd2LastLoaded + 1);
                    
                    for (var index = num2; index < num2 + num1; ++index)
                        switch (index % num1)
                        {
                            case 0:
                                if (csd2_rightTrigger.Active)
                                    goto default;
                                break;
                    
                            case 1:
                                if (csd2_leftTrigger.Active)
                                    goto default;
                                break;
                            
                            case 2:
                                if (!csd2.Scripts.LoadTriggerNormal.Active || !csd2.Scripts.DispatchNormalSegmentOccupied.Inactive)
                                    break;
                                goto default;
                            
                            default:
                                LoadCsd2((LOAD_DIRECTION)(index % num1));
                                return;
                        }

                    break;

                case SEGMENT_STATE.OCCUPIED:
                    if (!csd2.Scripts.DispatchNormalSegmentOccupied.Inactive)
                        break;
                
                    csd2.DispatchNormal();
                    break;
            }
        }

        private void LoadCsd1(LOAD_DIRECTION dir)
        {
            switch (dir)
            {
                case LOAD_DIRECTION.RIGHT:
                    if (!loadCsd1Right.Run())
                        break;
                    
                    csd1.ForceLoadPending();
                    csd1LastLoaded = LOAD_DIRECTION.RIGHT;
                    break;
                
                case LOAD_DIRECTION.LEFT:
                    if (loadCsd1Left.Run())
                        break;
                
                    csd1.ForceLoadPending();
                    csd1LastLoaded = LOAD_DIRECTION.LEFT;
                    break;
            }
        }

        private void LoadCsd2(LOAD_DIRECTION dir)
        {
            switch (dir)
            {
                case LOAD_DIRECTION.RIGHT:
                    if (!loadCsd2Right.Run())
                        break;
                
                    csd2.ForceLoadPending();
                    csd2LastLoaded = LOAD_DIRECTION.RIGHT;
                    break;

                case LOAD_DIRECTION.LEFT:
                    if (!loadCsd2Left.Run())
                        break;
                    
                    csd2.ForceLoadPending();
                    csd2LastLoaded = LOAD_DIRECTION.LEFT;
                    break;

                case LOAD_DIRECTION.MAIN:
                    if (!csd2.LoadNormal())
                        break;
                
                    csd2.ForceLoadPending();
                    csd2LastLoaded = LOAD_DIRECTION.MAIN;
                    break;
            }
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
        }

        private enum LOAD_DIRECTION
        {
            RIGHT,
            LEFT,
            MAIN
        }
    }
}