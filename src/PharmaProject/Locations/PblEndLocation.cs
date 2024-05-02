// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.PblEndLocation
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;

namespace PharmaProject.Locations
{
    public class PblEndLocation : BaseLocation
    {
        private InPin inBtn;
        private Conditional loadDispatch;

        public PblEndLocation(string IP, uint locationNumber)
            : base(IP, locationNumber, 2U)
        {
        }

        protected override InPin[] ResetEmergencyPins => new InPin[1] { inBtn };

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._18);
            GetScripts(2U).DispatchNormalSegmentOccupied = MakeIn(PIN._21).LowActive;
            GetScripts(1U).LoadTriggerNormal = MakeIn(PIN._20).LowActive;
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            loadDispatch = MakeConditionalBatch(string.Format(" location {0}, Load and dispatch CSD 1 => 2", LocationNumber)).AddStatement(scripts1.DispatchAlternative)
                .AddStatement(scripts2.LoadAlternative);
            MakeConditionalMacro(string.Format("Autonomous load location {0}", LocationNumber), RUN_MODE.PERMANENTLY).AddStatement(
                MakeConditionalStatement(string.Format("Autonomous load trigger location {0}", LocationNumber), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).MakePrecondition()
                    .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts1.LoadTriggerNormal).AddCondition(scripts1.BeltsRun, PIN_STATE.INACTIVE)
                    .AddCondition(scripts1.RollersRun, PIN_STATE.INACTIVE).AddCondition(scripts1.OccupiedRollers, PIN_STATE.INACTIVE).CloseBlock()).AddStatement(scripts1.LoadNormal);
            MakeConditionalMacro(string.Format("Autonomous carry over location {0}", LocationNumber), RUN_MODE.PERMANENTLY).AddStatement(
                    MakeConditionalStatement(string.Format("Autonomous carry over trigger  location {0}", LocationNumber), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).MakePrecondition()
                        .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts1.OccupiedRollers).AddCondition(scripts1.BeltsRun, PIN_STATE.INACTIVE)
                        .AddCondition(scripts1.RollersRun, PIN_STATE.INACTIVE)
                        .AddCondition(scripts2.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts2.RollersRun, PIN_STATE.INACTIVE).AddCondition(scripts2.OccupiedBelts, PIN_STATE.INACTIVE)
                        .CloseBlock())
                .AddStatement(loadDispatch);
            MakeConditionalMacro(string.Format("Autonomous dispatch location {0}", LocationNumber), RUN_MODE.PERMANENTLY).AddStatement(
                MakeConditionalStatement(string.Format("Autonomous dispatch trigger location {0}", LocationNumber), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).MakePrecondition()
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
                ? MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedBelts, csdNum, endDelay: 1000U, middleMotorRun: scripts.MiddleRollersRun)
                : null;
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 2U
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.DispatchNormalSegmentOccupied, scripts.OccupiedRollers, csdNum,
                    nextSegLoad: GetScripts(1U).DownstreamStartLoading)
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