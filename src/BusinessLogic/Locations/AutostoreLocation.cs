using System;
using System.Text;
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
    public class AutostoreLocation : BaseLocation
    {
        private const int TOTE_ENTER_INTERVAL_ms = 4000;
        private readonly DelayedEvent checkLoadNextTote;
        private Conditional dispatchToBelt;
        private volatile int flushBeltToteCnt = 1;
        private InPin inBtn;
        private DateTime lastToteEnter = DateTime.Now;
        private OutPin outBeltRun;
        private OutPin outStartSegmentRun;
        private string pendingBarcode = string.Empty;

        public AutostoreLocation(string IP, uint locationNumber)
            : base(IP, locationNumber, 2U)
        {
            checkLoadNextTote = new DelayedEvent(4100U, Evaluate);
        }

        private bool TotePending => !string.IsNullOrEmpty(pendingBarcode);

        protected override InPin[] ResetEmergencyPins => new InPin[1] { inBtn };

        private void DispatchToBelt_OnStateChanged(Conditional dtb)
        {
            if (dtb.Status != CONDITIONAL_STATE.FINISHED)
                return;

            lastToteEnter = DateTime.Now;
            checkLoadNextTote.Start();
            
            if (string.IsNullOrEmpty(pendingBarcode))
                return;
            
            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(WMS_TOTE_DIRECTION.DIRECTION_1, Encoding.ASCII.GetBytes(pendingBarcode), LocationNumber)));
            NdwConnectCommunicator.DirectionSentUpdate(LocationNumber, pendingBarcode, WMS_TOTE_DIRECTION.DIRECTION_1);
            pendingBarcode = null;
        }

        private void Csd1_OnStateChanged(CSD obj)
        {
            if (csd1.State != SEGMENT_STATE.LOADING || flushBeltToteCnt <= 0)
                return;
            
            --flushBeltToteCnt;
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            csd1.OnStateChanged += Csd1_OnStateChanged;
        }

        protected override void AttachCsdEventHandlers(CSD csd)
        {
            if (csd == null)
                return;
            
            base.AttachCsdEventHandlers(csd);
            var scripts = csd.Scripts;
            
            switch (csd.CsdNum)
            {
                case 2:
                    scripts.PassThrough.OnStateChanged += csd.Dispatch_OnStateChanged;
                    break;
            }
        }

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._24);
            outStartSegmentRun = MakeOut(PIN._24);
            outBeltRun = MakeOut(PIN._22);
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            var occupiedRollers = scripts2.OccupiedRollers;
            scripts1.DispatchAlternativeSegmentOccupied = occupiedRollers;
            scripts2.UpstreamStartDispatching = MakeOut(PIN._19);
            scripts2.DownstreamStartLoading = MakeOut(PIN._20);
            scripts2.DispatchNormalSegmentOccupied = MakeIn(PIN._21);
            scripts2.LoadTriggerNormal = MakeIn(PIN._20);
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var scripts = GetScripts(1U);
            
            MakeConditionalStatement($"Auto run belt autostore (Loc:{LocId})", OUTPUT_ENFORCEMENT.ENF_AT_CONDITION_TRUE, RUN_MODE.PERMANENTLY).AddLogicBlock(LOGIC_FUNCTION.OR)
                .AddCondition(scripts.RollersRun)
                .AddCondition(outStartSegmentRun)
                .CloseBlock()
                .AddOutputState(outBeltRun);
            
            dispatchToBelt = MakeConditionalStatement("Move segment 2 seconds", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddTimeoutCondition(2000U).AddOutputState(outStartSegmentRun);
            dispatchToBelt.OnStateChanged += DispatchToBelt_OnStateChanged;
        }

        protected override Conditional PassThroughScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            return csdNum == 2U
                ? MakeConditionalBatch("Passthrough CSD2")
                    .AddStatement(MakeConditionalStatement("Upstream dispatch command CSD2", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddTimeoutCondition(100U)
                        .AddOutputState(scripts.UpstreamStartDispatching)).AddStatement(MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW,
                        scripts.DispatchNormalSegmentOccupied, csdNum, timeOut: 8000U, prevSegDispatch: scripts.DownstreamStartLoading))
                : (Conditional)null;
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 1U ? MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, timeOut: 15000U, endDelay: 500U) : null;
        }

        protected override Conditional LoadAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            return csdNum == 2U
                ? MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, endDelay: 700U, middleMotorRun: scripts.MiddleRollersRun)
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

        private void CheckDispatch()
        {
            if (!TotePending || dispatchToBelt.IsRunningOrAboutToBe || DateTime.Now.Subtract(lastToteEnter) < TimeSpan.FromMilliseconds(4000.0))
                return;
            
            switch (csd1.State)
            {
                case SEGMENT_STATE.DISPATCHING_PENDING:
                case SEGMENT_STATE.OCCUPIED:
                case SEGMENT_STATE.DISPATCHING:
                    if (flushBeltToteCnt > 0)
                        return;
                    break;
            }

            ++flushBeltToteCnt;
            dispatchToBelt.Run();
            csd1.LoadNormal();
            lastToteEnter = DateTime.Now;
        }

        public override void DoEvaluate()
        {
            var scripts = GetScripts(2U);
            
            if (csd1.State == SEGMENT_STATE.IDLE && flushBeltToteCnt != 0)
            {
                lastToteEnter = DateTime.Now;
                checkLoadNextTote.Start();
                csd1.LoadNormal();
            }

            switch (csd2.State)
            {
                case SEGMENT_STATE.IDLE:
                    if (scripts.DispatchNormalSegmentOccupied.Inactive)
                    {
                        if (csd1.IsOccupied && !scripts.PassThrough.IsRunningOrAboutToBe && !scripts.DispatchNormal.IsRunningOrAboutToBe)
                        {
                            if (csd2.LoadAlternative()) csd1.DispatchAlternative();
                            break;
                        }

                        if (scripts.LoadTriggerNormal.Active && !scripts.LoadAlternative.IsRunningOrAboutToBe && scripts.PassThrough.Run()) csd2.ForceDispatchPending();
                    }

                    break;
            
                case SEGMENT_STATE.OCCUPIED:
                    if (scripts.DispatchNormalSegmentOccupied.Inactive)
                    {
                        csd2.DispatchNormal();
                    }

                    break;
            }

            CheckDispatch();
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            if (string.IsNullOrEmpty(barcode))
                return;

            pendingBarcode = barcode;
            Evaluate();
        }
    }
}