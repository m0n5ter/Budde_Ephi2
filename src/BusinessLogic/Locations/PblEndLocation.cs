﻿using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.UTC;

namespace PharmaProject.BusinessLogic.Locations
{
    public class PblEndLocation : BaseLocation
    {
        private InPin inBtn;
        protected Conditional loadDispatch;

        public PblEndLocation(string IP, uint locationNumber)
            : base(IP, locationNumber, 2U)
        {
        }

        protected override InPin[] ResetEmergencyPins => new InPin[1] { inBtn };

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._18);
            var scripts = GetScripts(2U);
            scripts.DispatchNormalSegmentOccupied = MakeIn(PIN._21).LowActive;
            scripts.DownstreamStartLoading = MakeOut(PIN._20);
            GetScripts(1U).LoadTriggerNormal = MakeIn(PIN._20).LowActive;
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);

            loadDispatch = MakeConditionalBatch($" location {LocationNumber}, Load and dispatch CSD 1 => 2").AddStatement(scripts1.DispatchAlternative)
                .AddStatement(scripts2.LoadAlternative);
            
            MakeConditionalMacro($"Autonomous load location {LocationNumber}", RUN_MODE.PERMANENTLY).AddStatement(
                MakeConditionalStatement($"Autonomous load trigger location {LocationNumber}", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).MakePrecondition()
                    .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts1.LoadTriggerNormal).AddCondition(scripts1.BeltsRun, PIN_STATE.INACTIVE)
                    .AddCondition(scripts1.RollersRun, PIN_STATE.INACTIVE).AddCondition(scripts1.OccupiedRollers, PIN_STATE.INACTIVE).CloseBlock()).AddStatement(scripts1.LoadNormal);
            
            InitAutoCrossoverScript(scripts1, scripts2);
            
            MakeConditionalMacro($"Autonomous dispatch location {LocationNumber}", RUN_MODE.PERMANENTLY).AddStatement(
                MakeConditionalStatement($"Autonomous dispatch trigger location {LocationNumber}", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).MakePrecondition()
                    .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts2.DispatchNormalSegmentOccupied, PIN_STATE.INACTIVE).AddCondition(scripts2.BeltsRun, PIN_STATE.INACTIVE)
                    .AddCondition(scripts2.RollersRun, PIN_STATE.INACTIVE).AddCondition(scripts2.OccupiedBelts).CloseBlock()).AddStatement(scripts2.DispatchNormal);
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            return csdNum == 1U
                ? MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, prevSegDispatch: scripts.UpstreamStartDispatching,
                    endDelay: 1000U)
                : null;
        }

        protected override Conditional LoadAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            return csdNum == 2U
                ? MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedBelts, csdNum, endDelay: 400U, middleMotorRun: scripts.MiddleRollersRun)
                : null;
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            return csdNum == 2U
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.DispatchNormalSegmentOccupied, scripts.OccupiedRollers, csdNum,
                    nextSegLoad: scripts.DownstreamStartLoading)
                : null;
        }

        protected override Conditional DispatchAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            return csdNum == 1U
                ? MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.DispatchAlternativeSegmentOccupied, scripts.OccupiedRollers, csdNum,
                    middleMotorRun: scripts.MiddleRollersRun)
                : null;
        }

        protected virtual void InitAutoCrossoverScript(CSD_PinsAndScripts s1, CSD_PinsAndScripts s2)
        {
            MakeConditionalMacro($"Autonomous carry over location {LocationNumber}", RUN_MODE.PERMANENTLY).AddStatement(
                    MakeConditionalStatement($"Autonomous carry over trigger  location {LocationNumber}", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).MakePrecondition()
                        .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(s1.OccupiedRollers).AddCondition(s1.BeltsRun, PIN_STATE.INACTIVE).AddCondition(s1.RollersRun, PIN_STATE.INACTIVE)
                        .AddCondition(s2.BeltsRun, PIN_STATE.INACTIVE).AddCondition(s2.RollersRun, PIN_STATE.INACTIVE).AddCondition(s2.OccupiedBelts, PIN_STATE.INACTIVE).CloseBlock())
                .AddStatement(loadDispatch);
        }

        public override void DoEvaluate()
        {
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
        }
    }
}