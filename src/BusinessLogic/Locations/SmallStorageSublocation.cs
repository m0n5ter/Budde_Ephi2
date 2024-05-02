// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Locations.SmallStorageSublocation
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.Segments;
using PharmaProject.BusinessLogic.UTC;

namespace PharmaProject.BusinessLogic.Locations
{
    internal class SmallStorageSublocation : CSD_UTC
    {
        internal CSD csd4;
        internal CSD csd5;
        internal CSD csd6;
        internal InPin inReturn_1_Btn;
        internal InPin inReturn_2_Btn;
        internal InPin inReturn_3_Btn;
        private OutPin inReturn3_Run;

        internal SmallStorageSublocation(string ip, SmallStorageLocation parent)
            : base(ip, string.Format("{0}sub", parent.LocationNumber), 3U)
        {
            csd4 = new CSD(parent.LocationNumber, 4U, GetScripts(1U), parent);
            csd5 = new CSD(parent.LocationNumber, 5U, GetScripts(2U), parent);
            csd6 = new CSD(parent.LocationNumber, 6U, GetScripts(3U), parent);
        }

        protected override InPin[] ResetEmergencyPins
        {
            get
            {
                return new InPin[3]
                {
                    inReturn_1_Btn,
                    inReturn_2_Btn,
                    inReturn_3_Btn
                };
            }
        }

        protected override void InitPins()
        {
            base.InitPins();
            inReturn3_Run = MakeOut(PIN._24);
            inReturn_1_Btn = MakeIn(PIN._24);
            inReturn_2_Btn = MakeIn(PIN._14);
            inReturn_3_Btn = MakeIn(PIN._15);
            var scripts1 = GetScripts(1U);
            scripts1.LoadTriggerNormal = InPin.Dummy;
            scripts1.DispatchNormalSegmentOccupied = MakeIn(PIN._20);
            scripts1.UpstreamStartDispatching = OutPin.Dummy;
            scripts1.DownstreamStartLoading = MakeOut(PIN._19);
            scripts1.MiddleRollersDir = OutPin.Dummy;
            scripts1.MiddleRollersRun = MakeOut(PIN._23);
            var scripts2 = GetScripts(2U);
            scripts2.LoadTriggerNormal = InPin.Dummy;
            scripts2.DispatchNormalSegmentOccupied = MakeIn(PIN._21);
            scripts2.UpstreamStartDispatching = OutPin.Dummy;
            scripts2.DownstreamStartLoading = MakeOut(PIN._20);
            scripts2.MiddleRollersDir = OutPin.Dummy;
            scripts2.MiddleRollersRun = MakeOut(PIN._23);
            var scripts3 = GetScripts(3U);
            scripts3.LoadTriggerNormal = InPin.Dummy;
            scripts3.DispatchNormalSegmentOccupied = InPin.Dummy;
            scripts3.DispatchAlternativeSegmentOccupied = MakeIn(PIN._22);
            scripts3.LoadTriggerAlternative = MakeIn(PIN._23);
            scripts3.UpstreamStartDispatching = MakeOut(PIN._22);
            scripts3.DownstreamStartLoading = MakeOut(PIN._21);
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 3U
                ? MakeConditionalBatch(string.Format("Load CSD 6 from IO conveyor (Loc:{0}, CSD:{1})", LocId, csdNum))
                    .AddStatement(MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CCW, scripts.OccupiedRollers, csdNum, endDelay: 1000U)).AddStatement(
                        MakeConditionalStatement(string.Format("Disp IO segment to CSD 6 (Loc:{0}, CSD:{1})", LocId, csdNum), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                            .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts.OccupiedRollers).AddGuardBlock(1000U).AddGuardPin(scripts.OccupiedRollers).CloseBlock().CloseBlock()
                            .AddOutputState(inReturn3_Run))
                : (Conditional)null;
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (csdNum == 1U)
                return MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CCW, scripts.DispatchNormalSegmentOccupied, scripts.OccupiedRollers, csdNum,
                    nextSegLoad: scripts.DownstreamStartLoading);
            return csdNum == 2U ? base.DispatchNormalScript(csdNum) : null;
        }

        protected override Conditional DispatchAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (csdNum == 1U)
                return MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, GetScripts(2U).OccupiedBelts, scripts.OccupiedBelts, csdNum,
                    middleMotorRun: scripts.MiddleRollersRun);
            return csdNum == 3U
                ? MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.DispatchAlternativeSegmentOccupied, scripts.OccupiedRollers, csdNum,
                    nextSegLoad: scripts.DownstreamStartLoading)
                : null;
        }

        protected override Conditional LoadAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (csdNum == 2U)
                return base.LoadAlternativeScript(csdNum);
            return csdNum == 3U
                ? MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.OccupiedRollers, csdNum, prevSegDispatch: scripts.UpstreamStartDispatching,
                    endDelay: 500U)
                : MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.OccupiedBelts, csdNum, middleMotorRun: scripts.MiddleRollersRun);
        }

        protected override Conditional PassThroughScript(uint csdNum)
        {
            if (csdNum != 1U && csdNum != 3U)
                return null;
            var scripts = GetScripts(csdNum);
            return scripts == null || scripts.LoadAlternative == null || scripts.DispatchAlternative == null
                ? null
                : (Conditional)MakeConditionalMacro(string.Format("Pass through alternative (Loc:{0}, CSD:{1})", LocId, csdNum)).AddStatement(scripts.LoadAlternative)
                    .AddStatement(scripts.DispatchAlternative);
        }

        protected override void InvalidateCsdStates()
        {
            csd4.InvalidateState();
            csd5.InvalidateState();
            csd6.InvalidateState();
        }
    }
}