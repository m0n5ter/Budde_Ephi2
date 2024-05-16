using System;
using Ephi.Core.Helping;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.Segments;

namespace PharmaProject.BusinessLogic.Locations
{
    public class Pbl_B_EnterMainLocation : BaseLocation
    {
        private Conditional csd1AutoAlign;
        private InPin inBtn;
        private Conditional merge2to1;

        public Pbl_B_EnterMainLocation(string IP, uint locationNumber)
            : base(IP, locationNumber, 2U)
        {
        }

        protected override InPin[] ResetEmergencyPins => new InPin[1] { inBtn };

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._24);
            var scripts = GetScripts(1U);
            scripts.RollersDir = scripts.RollersDir.LowActive;
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            var name = $" Tote merge 2=>1 (Loc:{LocationNumber})";
            Conditional conditional = MakeConditionalBatch("Simultaneously load+dispatch" + name).AddStatement(scripts1.LoadAlternative).AddStatement(scripts2.DispatchAlternative);
            merge2to1 = MakeConditionalMacro(name).AddStatement(scripts2.LoadNormal).AddStatement(conditional);
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts1 = GetScripts(csdNum);
            Conditional conditional1 = null;
            Conditional conditional2;

            switch (csdNum)
            {
                case 1:
                    conditional2 = base.LoadNormalScript(csdNum);
                    conditional1 = MakeConditionalMacro("Straighten box to the side + load")
                        .AddStatement(MakeConditionalBatch("Align while raise").AddStatement(scripts1.TableUp).AddStatement(
                            MakeConditionalStatement("PreAlign", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(2000U).AddCondition(scripts1.LiftRun, PIN_STATE.INACTIVE)
                                .AddOutputState(scripts1.BeltsRun).AddOutputState(scripts1.BeltsDir, PIN_STATE.INACTIVE))).AddStatement(MakeConditionalBatch("Align while lower")
                            .AddStatement(scripts1.TableDown).AddStatement(MakeConditionalStatement("PostAlign", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(2000U)
                                .AddCondition(scripts1.LiftRun, PIN_STATE.INACTIVE).AddOutputState(scripts1.BeltsRun).AddOutputState(scripts1.BeltsDir, PIN_STATE.INACTIVE)));
                    csd1AutoAlign = conditional1;
                    csd1AutoAlign.OnStateChanged += Script_OnStateChanged;
                    break;
            
                case 2:
                    conditional2 = MakeLoadStatement(scripts1.RollersRun, TABLE_POSITION.DOWN, scripts1.RollersDir, MOTOR_DIR.CW, scripts1.OccupiedRollers, csdNum,
                        prevSegDispatch: scripts1.UpstreamStartDispatching, endDelay: 200U);
                    break;
                
                default:
                    return null;
            }

            var logicBlock = MakeConditionalStatement($"Auto load precondition CSD:{csdNum}, Loc:{LocationNumber}", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).MakePrecondition()
                .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts1.LoadTriggerNormal).AddCondition(scripts1.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts1.RollersRun, PIN_STATE.INACTIVE)
                .AddCondition(scripts1.LiftRun, PIN_STATE.INACTIVE).AddCondition(scripts1.OccupiedBelts, PIN_STATE.INACTIVE).AddCondition(scripts1.OccupiedRollers, PIN_STATE.INACTIVE)
                .AddGuardBlock(100U).AddGuardPin(scripts1.BeltsRun).AddGuardPin(scripts1.LiftRun).CloseBlock();
            
            if (csdNum == 1U)
            {
                var scripts2 = GetScripts(2U);
                logicBlock = logicBlock.AddCondition(scripts2.RollersRun, PIN_STATE.INACTIVE).AddCondition(scripts2.BeltsRun, PIN_STATE.INACTIVE)
                    .AddCondition(scripts2.OccupiedBelts, PIN_STATE.INACTIVE).AddCondition(scripts2.OccupiedRollers, PIN_STATE.INACTIVE);
            }

            Conditional conditional3 = logicBlock.CloseBlock();
            
            MakeConditionalMacro($"Auto load script CSD:{csdNum}, Loc:{LocationNumber}", RUN_MODE.PERMANENTLY).AddStatement(conditional3).AddStatement(conditional2)
                .AddStatement(conditional1);
            
            return conditional2;
        }

        protected override Conditional LoadAlternativeScript(uint csdNum)
        {
            if (csdNum != 1U)
                return null;
            
            var scripts = GetScripts(csdNum);
            
            return MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, endDelay: 1000U, middleMotorRun: scripts.MiddleRollersRun,
                middleMotorDir: scripts.MiddleRollersDir);
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            return csdNum == 1U
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.RollersRun, scripts.OccupiedRollers, csdNum,
                    nextSegLoad: scripts.DownstreamStartLoading, endDelay: 200U)
                : null;
        }

        protected override Conditional DispatchAlternativeScript(uint csdNum)
        {
            if (csdNum != 2U)
                return null;
            
            var scripts = GetScripts(csdNum);
            
            return MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, GetScripts(1U).OccupiedRollers, scripts.OccupiedRollers, csdNum, endDelay: 900U,
                middleMotorRun: scripts.MiddleRollersRun, middleMotorDir: scripts.MiddleRollersDir);
        }

        protected override Conditional PassThroughScript(uint csd)
        {
            return null;
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
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
                case SEGMENT_STATE.LOADING:
                case SEGMENT_STATE.OCCUPIED:
                    Dispatch(a);
                    break;
            }
        }

        private void Dispatch(CSD csd)
        {
            if (!Helpers.Contains(csd.State, SEGMENT_STATE.OCCUPIED))
                return;
            
            if (csd == csd1)
            {
                if (csd.Scripts.DispatchNormalSegmentOccupied.Active)
                    return;
            
                if (csd.Scripts.DownstreamStartLoading.Active || csd.Scripts.DownstreamStartLoading.PinStateAge < TimeSpan.FromSeconds(2.0) || csd1AutoAlign.IsRunningOrAboutToBe)
                {
                    ReEvaluate.Start();
                }
                else
                {
                    if (!csd.IsOccupied)
                        return;
                
                    csd.DispatchNormal();
                }
            }
            else
            {
                if (!csd1.IsIdle || !merge2to1.Run())
                    return;
                
                csd1.ForceLoadPending();
                csd2.ForceDispatchPending();
            }
        }
    }
}