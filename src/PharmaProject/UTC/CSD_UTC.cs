// Decompiled with JetBrains decompiler
// Type: PharmaProject.UTC.CSD_UTC
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using System.Net;
using Ephi.Core.Helping;
using Ephi.Core.Helping.Log4Net;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;

namespace PharmaProject.UTC
{
    public abstract class CSD_UTC : RemoteUtc
    {
        private static readonly UtcServer utcServer = new UtcServer();
        private readonly CSD_PinsAndScripts csdScript1;
        private readonly CSD_PinsAndScripts csdScript2;
        private readonly CSD_PinsAndScripts csdScript3;
        private bool operational;

        public CSD_UTC(string ip, string locId, uint numOfCSDs)
            : base(utcServer, Log4NetHelpers.AddIsolatedLogger("Base UTC", "..\\Log\\UTC\\"))
        {
            Configuration.WatchdogEnabled = true;
            Configuration.DisableOutputsOnDisconnect = false;
            Configuration.CancelConditionalsOnDisconnect = false;
            Configuration.HeartbeatTimeoutMs = 2000;
            DelayedUnoperational_ms = 3000U;
            LocId = locId;
            ReplaceIp(IPAddress.Parse(ip));
            if (numOfCSDs > 3U)
                throw new ArgumentOutOfRangeException(nameof(numOfCSDs), "Argument value too large");
            if (numOfCSDs > 0U)
                csdScript1 = new CSD_PinsAndScripts(1U);
            if (numOfCSDs > 1U)
                csdScript2 = new CSD_PinsAndScripts(2U);
            if (numOfCSDs > 2U)
                csdScript3 = new CSD_PinsAndScripts(3U);
            InitPins();
            InitScripts();
            InitEmergencyResetPins();
        }

        protected string LocId { get; set; } = "Unk.";

        public bool Initializing => Helpers.Contains(Status, UTC_STATUS.AWAITING_HANDSHAKE, UTC_STATUS.AWAITING_INITIALIZATION, UTC_STATUS.OFF_LINE);

        public new bool Operational
        {
            get => operational;
            private set
            {
                if (operational == value)
                    return;
                operational = value;
            }
        }

        public CSD_UTC MimicSoftEmergencyUtc
        {
            set
            {
                if (value == null)
                    return;
                value.OnSoftEmrgChanged += lead => SoftEmergency = lead.SoftEmergency;
            }
        }

        protected virtual InPin[] ResetEmergencyPins => new InPin[0];

        public static UtcServer UtcServerConnect(int port, string ip = null)
        {
            return UtcServerConnect(port, IPAddress.Parse(ip));
        }

        public static UtcServer UtcServerConnect(int port, IPAddress ip)
        {
            utcServer.SetIpPort(port, ip ?? IPAddress.Any);
            utcServer.Connect();
            return utcServer;
        }

        public static void UtcServerClear()
        {
            utcServer.ClearRegistered();
        }

        public static void UtcServerDisconnect()
        {
            utcServer.Disconnect();
        }

        public bool HasCSD(uint CSDnumber)
        {
            try
            {
                return GetScripts(CSDnumber) != null;
            }
            catch
            {
                return false;
            }
        }

        public CSD_PinsAndScripts GetScripts(uint csdNum)
        {
            CSD_PinsAndScripts scripts = null;
            switch (csdNum)
            {
                case 1:
                    scripts = csdScript1;
                    break;
                case 2:
                    scripts = csdScript2;
                    break;
                case 3:
                    scripts = csdScript3;
                    break;
            }

            return scripts;
        }

        protected virtual void InitPins()
        {
            if (HasCSD(1U))
            {
                csdScript1.LoadTriggerNormal = MakeIn(PIN._20).LowActive;
                csdScript1.DispatchNormalSegmentOccupied = MakeIn(PIN._21).LowActive;
                csdScript1.LiftUp = MakeIn(PIN._2);
                csdScript1.LiftDown = MakeIn(PIN._1);
                csdScript1.OccupiedRollers = MakeIn(PIN._3);
                csdScript1.OccupiedBelts = MakeIn(PIN._4);
                csdScript1.LiftRun = MakeOut(PIN._3);
                csdScript1.LiftDir = MakeOut(PIN._4);
                csdScript1.RollersRun = MakeOut(PIN._1);
                csdScript1.RollersDir = MakeOut(PIN._2);
                csdScript1.BeltsRun = MakeOut(PIN._5);
                csdScript1.BeltsDir = MakeOut(PIN._6);
                csdScript1.UpstreamStartDispatching = MakeOut(PIN._19);
                csdScript1.DownstreamStartLoading = MakeOut(PIN._20);
                csdScript1.MiddleRollersRun = MakeOut(PIN._23);
                csdScript1.MiddleRollersDir = MakeOut(PIN._24);
            }

            if (HasCSD(2U))
            {
                csdScript2.LoadTriggerNormal = MakeIn(PIN._22).LowActive;
                csdScript2.DispatchNormalSegmentOccupied = MakeIn(PIN._23).LowActive;
                csdScript2.LiftUp = MakeIn(PIN._7);
                csdScript2.LiftDown = MakeIn(PIN._6);
                csdScript2.OccupiedRollers = MakeIn(PIN._8);
                csdScript2.OccupiedBelts = MakeIn(PIN._9);
                csdScript2.LiftRun = MakeOut(PIN._9);
                csdScript2.LiftDir = MakeOut(PIN._10);
                csdScript2.RollersRun = MakeOut(PIN._7);
                csdScript2.RollersDir = MakeOut(PIN._8);
                csdScript2.BeltsRun = MakeOut(PIN._11);
                csdScript2.BeltsDir = MakeOut(PIN._12);
                csdScript2.UpstreamStartDispatching = MakeOut(PIN._21);
                csdScript2.DownstreamStartLoading = MakeOut(PIN._22);
                csdScript2.MiddleRollersRun = csdScript1.MiddleRollersRun;
                csdScript2.MiddleRollersDir = csdScript1.MiddleRollersDir;
                csdScript1.LoadTriggerAlternative = csdScript2.OccupiedBelts;
                csdScript1.DispatchAlternativeSegmentOccupied = csdScript2.OccupiedBelts;
                csdScript2.LoadTriggerAlternative = csdScript1.OccupiedBelts;
                csdScript2.DispatchAlternativeSegmentOccupied = csdScript1.OccupiedBelts;
            }

            if (!HasCSD(3U))
                return;
            csdScript3.LiftUp = MakeIn(PIN._12);
            csdScript3.LiftDown = MakeIn(PIN._11);
            csdScript3.OccupiedRollers = MakeIn(PIN._13);
            csdScript3.OccupiedBelts = MakeIn(PIN._14);
            csdScript3.LiftRun = MakeOut(PIN._15);
            csdScript3.LiftDir = MakeOut(PIN._16);
            csdScript3.RollersRun = MakeOut(PIN._13);
            csdScript3.RollersDir = MakeOut(PIN._14);
            csdScript3.BeltsRun = MakeOut(PIN._17);
            csdScript3.BeltsDir = MakeOut(PIN._18);
        }

        protected virtual Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return scripts != null
                ? MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, prevSegDispatch: scripts.UpstreamStartDispatching)
                : null;
        }

        protected virtual Conditional LoadAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (scripts == null)
                return null;
            if (csdNum == 1U)
                return MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedBelts, csdNum, middleMotorRun: scripts.MiddleRollersRun,
                    middleMotorDir: scripts.MiddleRollersDir);
            return csdNum == 2U
                ? MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedBelts, csdNum, middleMotorRun: scripts.MiddleRollersRun)
                : null;
        }

        protected virtual Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return scripts != null
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.DispatchNormalSegmentOccupied, scripts.OccupiedRollers, csdNum,
                    nextSegLoad: scripts.DownstreamStartLoading)
                : null;
        }

        protected virtual Conditional DispatchAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (scripts == null)
                return null;
            if (csdNum == 1U)
                return MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.DispatchAlternativeSegmentOccupied, scripts.OccupiedBelts, csdNum,
                    middleMotorRun: scripts.MiddleRollersRun);
            return csdNum == 2U
                ? MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.DispatchAlternativeSegmentOccupied, scripts.OccupiedBelts, csdNum,
                    middleMotorRun: scripts.MiddleRollersRun, middleMotorDir: scripts.MiddleRollersDir)
                : null;
        }

        protected virtual Conditional PassThroughScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return scripts?.LoadNormal == null || scripts.DispatchNormal == null
                ? null
                : (Conditional)MakeConditionalMacro($"Pass through (Loc:{LocId}, CSD:{csdNum})").AddStatement(scripts.LoadNormal).AddStatement(scripts.DispatchNormal);
        }

        protected virtual void InitScripts()
        {
            InitCsdScrips(1U);
            InitCsdScrips(2U);
            InitCsdScrips(3U);
        }

        protected virtual void InitCsdScrips(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (scripts == null)
                return;
            scripts.TableUp = MakeTableMovementStatement(TABLE_POSITION.UP, scripts.LiftUp, scripts.LiftDown, scripts.LiftRun, scripts.LiftDir, csdNum);
            scripts.TableDown = MakeTableMovementStatement(TABLE_POSITION.DOWN, scripts.LiftUp, scripts.LiftDown, scripts.LiftRun, scripts.LiftDir, csdNum);
            scripts.LoadNormal = LoadNormalScript(csdNum);
            scripts.LoadAlternative = LoadAlternativeScript(csdNum);
            scripts.DispatchNormal = DispatchNormalScript(csdNum);
            scripts.DispatchAlternative = DispatchAlternativeScript(csdNum);
            scripts.PassThrough = PassThroughScript(csdNum);
        }

        protected Conditional MakeTableMovementStatement(
            TABLE_POSITION pos,
            InPin up,
            InPin down,
            OutPin motor,
            OutPin dir,
            uint csdNum)
        {
            var logicBlock =
                MakeConditionalStatement(string.Format("Table " + (pos == TABLE_POSITION.UP ? "Up" : "Down") + " (Loc:{0}, CSD:{1})", LocId, csdNum), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                    .MakePrecondition().AddGlobalTimeout(TIMEOUT_RANGE.TR_SEC, 4).AddOutputState(motor).AddOutputState(dir, pos == TABLE_POSITION.UP ? PIN_STATE.ACTIVE : PIN_STATE.INACTIVE)
                    .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(up, pos == TABLE_POSITION.UP ? PIN_STATE.ACTIVE : PIN_STATE.INACTIVE)
                    .AddCondition(down, pos == TABLE_POSITION.UP ? PIN_STATE.INACTIVE : PIN_STATE.ACTIVE);
            if (pos == TABLE_POSITION.UP)
                logicBlock = logicBlock.AddGuardBlock(100U).AddGuardPin(up).CloseBlock();
            return logicBlock.CloseBlock();
        }

        protected Conditional MakeDispatchStatement(
            OutPin motor,
            TABLE_POSITION pos,
            OutPin dir,
            MOTOR_DIR motorDir,
            BasePin activeFinished,
            BasePin activeNotFinished,
            uint csdNum,
            BasePin extraActiveFinished = null,
            BasePin extraActiveNotFinished = null,
            uint timeOut = 5000,
            OutPin nextSegLoad = null,
            uint endDelay = 0,
            OutPin middleMotorRun = null,
            OutPin middleMotorDir = null)
        {
            nextSegLoad = nextSegLoad ?? OutPin.Dummy;
            middleMotorRun = middleMotorRun ?? OutPin.Dummy;
            middleMotorDir = middleMotorDir ?? OutPin.Dummy;
            var str = $" {(pos == TABLE_POSITION.DOWN ? "Rollers" : (object)"Belts")} {(motorDir == MOTOR_DIR.CW ? "CW" : (object)"CCW")} (Loc:{LocId}, CSD:{csdNum})";
            var conditional1 = pos == TABLE_POSITION.DOWN ? GetScripts(csdNum).TableDown : GetScripts(csdNum).TableUp;
            Conditional conditional2 = MakeConditionalStatement("Move 1" + str, OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddOutputState(motor).AddOutputState(nextSegLoad)
                .AddOutputState(middleMotorRun).AddOutputState(middleMotorDir).AddOutputState(dir, motorDir == MOTOR_DIR.CCW ? PIN_STATE.ACTIVE : PIN_STATE.INACTIVE).AddLogicBlock(LOGIC_FUNCTION.AND)
                .AddTimeoutCondition(1000U).AddLogicBlock(LOGIC_FUNCTION.OR).AddCondition(activeFinished).AddCondition(extraActiveFinished).CloseBlock().AddLogicBlock(LOGIC_FUNCTION.AND)
                .AddCondition(activeNotFinished, PIN_STATE.INACTIVE).AddCondition(extraActiveNotFinished, PIN_STATE.INACTIVE).CloseBlock().CloseBlock();
            Conditional conditional3 = null;
            if (endDelay > 0U)
                conditional3 = MakeConditionalStatement("Move 2" + str, OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddOutputState(motor).AddOutputState(middleMotorRun).AddOutputState(middleMotorDir)
                    .AddOutputState(dir, motorDir == MOTOR_DIR.CCW ? PIN_STATE.ACTIVE : PIN_STATE.INACTIVE).AddLogicBlock(LOGIC_FUNCTION.AND).AddLogicBlock(LOGIC_FUNCTION.OR)
                    .AddCondition(activeFinished).AddCondition(extraActiveFinished).CloseBlock().AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(activeNotFinished, PIN_STATE.INACTIVE)
                    .AddCondition(extraActiveNotFinished, PIN_STATE.INACTIVE).CloseBlock().AddGuardBlock(endDelay).AddGuardPin(activeFinished).AddGuardPin(extraActiveFinished)
                    .AddGuardPin(activeNotFinished).AddGuardPin(extraActiveNotFinished).CloseBlock().CloseBlock();
            return MakeConditionalMacro("Dispatch" + str).AddGlobalTimeout(timeOut).AddStatement(conditional1).AddStatement(conditional2).AddStatement(conditional3)
                .AddStatement(GetScripts(csdNum).TableDown);
        }

        protected Conditional MakeLoadStatement(
            OutPin motor,
            TABLE_POSITION pos,
            OutPin dir,
            MOTOR_DIR motorDir,
            BasePin activeFinished,
            uint csdNum,
            BasePin extraActiveFinished = null,
            uint timeOut = 5000,
            OutPin prevSegDispatch = null,
            uint endDelay = 0,
            OutPin middleMotorRun = null,
            OutPin middleMotorDir = null)
        {
            prevSegDispatch = prevSegDispatch ?? OutPin.Dummy;
            middleMotorRun = middleMotorRun ?? OutPin.Dummy;
            middleMotorDir = middleMotorDir ?? OutPin.Dummy;
            var str = $" {(pos == TABLE_POSITION.DOWN ? "Rollers" : (object)"Belts")} {(motorDir == MOTOR_DIR.CW ? "CW" : (object)"CCW")} (Loc:{LocId}, CSD:{csdNum})";
            var conditional1 = pos == TABLE_POSITION.DOWN ? GetScripts(csdNum).TableDown : GetScripts(csdNum).TableUp;
            Conditional conditional2 = MakeConditionalStatement("Move 1" + str, OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(timeOut).AddOutputState(motor)
                .AddOutputState(prevSegDispatch).AddOutputState(middleMotorRun).AddOutputState(middleMotorDir).AddOutputState(dir, motorDir == MOTOR_DIR.CCW ? PIN_STATE.ACTIVE : PIN_STATE.INACTIVE)
                .AddLogicBlock(LOGIC_FUNCTION.OR).AddCondition(activeFinished).AddCondition(extraActiveFinished).CloseBlock();
            Conditional conditional3 = null;
            if (endDelay > 0U)
                conditional3 = MakeConditionalStatement("Move 2" + str, OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(timeOut).AddOutputState(motor).AddOutputState(prevSegDispatch)
                    .AddOutputState(middleMotorRun).AddOutputState(middleMotorDir).AddOutputState(dir, motorDir == MOTOR_DIR.CCW ? PIN_STATE.ACTIVE : PIN_STATE.INACTIVE)
                    .AddLogicBlock(LOGIC_FUNCTION.AND).AddLogicBlock(LOGIC_FUNCTION.OR).AddCondition(activeFinished).AddCondition(extraActiveFinished).CloseBlock().AddGuardBlock(endDelay)
                    .AddGuardPin(activeFinished).AddGuardPin(extraActiveFinished).CloseBlock().CloseBlock();
            return MakeConditionalMacro("Load" + str).AddGlobalTimeout(timeOut).AddStatement(conditional1).AddStatement(conditional2).AddStatement(conditional3);
        }

        public void Reconnect()
        {
            log.Info("Reconnect requested");
            SoftDisconnect();
        }

        protected override void ConnectionStateChanged()
        {
            base.ConnectionStateChanged();
            Operational = Status == UTC_STATUS.OPERATIONAL;
        }

        public override void ResetTimeoutsAndErrors()
        {
            base.ResetTimeoutsAndErrors();
            StatusChanged();
        }

        public override string ToString()
        {
            return $"{Formatting.TitleCase(LocId)}: {Status}";
        }

        protected override void StatusChanged()
        {
            base.StatusChanged();
            if (Status != UTC_STATUS.OPERATIONAL)
                return;
            InvalidateCsdStates();
        }

        protected abstract void InvalidateCsdStates();

        protected override void HardEmrgChanged()
        {
            if (!HardEmergency)
                return;
            SoftEmergency = true;
        }

        public static void GroupSoftEmergency(params CSD_UTC[] utcs)
        {
            foreach (RemoteUtc utc1 in utcs)
                utc1.OnSoftEmrgChanged += chgUtc =>
                {
                    foreach (var utc2 in utcs)
                        if (utc2 != chgUtc && (chgUtc as CSD_UTC).ResetEmergencyPins.Length != 0)
                            utc2.SoftEmergency = chgUtc.SoftEmergency;
                };
        }

        private void InitEmergencyResetPins()
        {
            foreach (var resetEmergencyPin in ResetEmergencyPins)
                resetEmergencyPin.OnStateChanged += EmrgRst_OnStateChanged;
        }

        private void EmrgRst_OnStateChanged(InPin pin)
        {
            if (!EmergencyStop || HardEmergency || !pin.Active)
                return;
            SoftEmergency = false;
        }
    }
}