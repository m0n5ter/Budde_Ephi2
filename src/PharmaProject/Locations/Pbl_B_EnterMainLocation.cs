// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.Pbl_B_EnterMainLocation
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using Ephi.Core.Helping;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.Segments;

namespace PharmaProject.Locations
{
    public class Pbl_B_EnterMainLocation : BaseLocation
    {
        private Conditional csd1AutoLoad;
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
            var scripts = GetScripts(csdNum);
            Conditional conditional1;
            switch (csdNum)
            {
                case 1:
                    conditional1 = MakeConditionalMacro("Straighten box to the side + load").AddStatement(base.LoadNormalScript(csdNum)).AddStatement(scripts.TableUp)
                        .AddStatement(MakeConditionalStatement("Move 2", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddTimeoutCondition(1200U).AddOutputState(scripts.BeltsRun)
                            .AddOutputState(scripts.BeltsDir, PIN_STATE.INACTIVE)).AddStatement(scripts.TableDown);
                    break;
                case 2:
                    conditional1 = base.LoadNormalScript(csdNum);
                    break;
                default:
                    return null;
            }

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
            if (csdNum != 1U)
                return null;
            var scripts = GetScripts(csdNum);
            return MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, endDelay: 1500U, middleMotorRun: scripts.MiddleRollersRun,
                middleMotorDir: scripts.MiddleRollersDir);
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            return csdNum != 1U ? null : base.DispatchNormalScript(csdNum);
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
            var scripts = GetScripts(csd);
            return csd == 1U
                ? MakeConditionalMacro($"Pass through (Loc:{LocationNumber}, CSD:{csd})").AddStatement(base.LoadNormalScript(csd)).AddStatement(scripts.DispatchNormal)
                : base.PassThroughScript(csd);
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
                case CSD_STATE.IDLE:
                    a.Route = null;
                    if (a == csd2)
                        break;
                    if (!csd2.IsIdle)
                    {
                        if (csd1AutoLoad.IsCancelledOrAboutToBe)
                            break;
                        Log("Load trigger 1 CANCELLED");
                        csd1AutoLoad.Cancel();
                        break;
                    }

                    if (csd1AutoLoad.IsRunningOrAboutToBe)
                        break;
                    Log("Load trigger 1 RUN");
                    csd1AutoLoad.Run();
                    break;
                case CSD_STATE.LOADING:
                case CSD_STATE.OCCUPIED:
                    Dispatch(a);
                    break;
            }
        }

        private void Dispatch(CSD csd)
        {
            if (!Helpers.Contains(csd.State, CSD_STATE.LOADING_PENDING, CSD_STATE.LOADING, CSD_STATE.OCCUPIED))
                return;
            if (csd == csd1)
            {
                if (csd.Scripts.DispatchNormalSegmentOccupied.Active)
                    return;
                if (csd1.IsOccupied)
                {
                    csd.DispatchNormal();
                }
                else
                {
                    if (Helpers.Contains(csd2.State, CSD_STATE.DISPATCHING_PENDING, CSD_STATE.DISPATCHING, CSD_STATE.LOADING_PENDING, CSD_STATE.LOADING))
                        return;
                    csd.PassThrough();
                }
            }
            else
            {
                if (!csd1.IsIdle)
                    return;
                csd1AutoLoad.Cancel();
                csd1.Scripts.PassThrough.Cancel();
                if (!merge2to1.Run())
                    return;
                csd1.ForceLoadPending();
                csd2.ForceDispatchPending();
                csd.Route = null;
            }
        }
    }
}