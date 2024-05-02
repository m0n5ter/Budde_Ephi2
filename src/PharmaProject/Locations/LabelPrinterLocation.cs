// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.LabelPrinterLocation
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using System.Net;
using System.Text;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.Segments;
using PharmaProject.Wms_Communication;
using PharmaProject.Wms_Communication.Messages;

namespace PharmaProject.Locations
{
    public class LabelPrinterLocation : BaseLocation
    {
        private Conditional autoLoadPrinter1;
        private Conditional BarcodeTrigger;
        private Conditional dispatchPrn1;
        private Conditional dispatchPrn2;
        private OutPin exitBarcodeTrigger;
        private InPin inPrinter1Occupied;
        private InPin inPrinter2Occupied;
        private bool printAt1;
        private readonly LabelPrinter prn1;
        private readonly LabelPrinter prn2;
        private readonly DelayedEvent ReEvaluate;
        private OutPin s1_Run;
        private OutPin s2_Run;
        private InPin s2_Sensor;
        private OutPin s3_Run;
        private OutPin s4_Run;
        private InPin s4_Sensor;

        public LabelPrinterLocation(
            string IP,
            uint locationNumber,
            string BSleftIP,
            string BSrightIP,
            LabelPrinter prn1,
            LabelPrinter prn2)
            : base(IP, locationNumber, 2U)
        {
            AddBarcodeScanner(new BarcodeScanner("Barcode Scanner Left", IPAddress.Parse(BSleftIP)));
            AddBarcodeScanner(new BarcodeScanner("Barcode Scanner Right", IPAddress.Parse(BSrightIP)));
            this.prn1 = prn1;
            this.prn2 = prn2;
            prn1.OnStateChanged += Prn_OnStateChanged;
            prn2.OnStateChanged += Prn_OnStateChanged;
            prn1.OnStateChanged += ReEvaluateState;
            prn1.OnDoEvaluate += ReEvaluateState;
            prn2.OnStateChanged += ReEvaluateState;
            prn2.OnDoEvaluate += ReEvaluateState;
            ReEvaluate = new DelayedEvent(TimeSpan.FromMilliseconds(500.0), Evaluate);
        }

        public OutPin ExitBarcodeTrigger
        {
            get => exitBarcodeTrigger;
            set
            {
                exitBarcodeTrigger = value;
                if (value == null)
                    return;
                BarcodeTrigger = exitBarcodeTrigger.Utc.MakeConditionalStatement("BarcodeTrigger", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddTimeoutCondition(500U)
                    .AddOutputState(exitBarcodeTrigger);
            }
        }

        public CSD ReboundEndCsd => csd2;

        protected override void InitPins()
        {
            base.InitPins();
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            scripts1.LoadTriggerNormal = MakeIn(PIN._19).LowActive;
            scripts1.UpstreamStartDispatching = MakeOut(PIN._18);
            var occupiedRollers = scripts1.OccupiedRollers;
            scripts2.DispatchAlternativeSegmentOccupied = occupiedRollers;
            inPrinter1Occupied = MakeIn(PIN._20).HighActive;
            s2_Sensor = MakeIn(PIN._21).HighActive;
            inPrinter2Occupied = MakeIn(PIN._22).HighActive;
            s4_Sensor = MakeIn(PIN._23).HighActive;
            s1_Run = MakeOut(PIN._19);
            s2_Run = MakeOut(PIN._20);
            s3_Run = MakeOut(PIN._21);
            s4_Run = MakeOut(PIN._22);
            inPrinter1Occupied.OnStateChanged += Sensor_OnStateChanged;
            s2_Sensor.OnStateChanged += Sensor_OnStateChanged;
            inPrinter2Occupied.OnStateChanged += Sensor_OnStateChanged;
            s4_Sensor.OnStateChanged += Sensor_OnStateChanged;
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var scripts = GetScripts(1U);
            GetScripts(2U);
            MakeConditionalMacro(string.Format("Auto Seg2 => Seg3 Loc:{0}", LocationNumber), RUN_MODE.PERMANENTLY)
                .AddStatement(MakeConditionalStatement("Auto Seg2 => Seg3 precondition", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).MakePrecondition().AddLogicBlock(LOGIC_FUNCTION.AND)
                    .AddCondition(s2_Sensor).AddCondition(inPrinter2Occupied, PIN_STATE.INACTIVE).CloseBlock()).AddStatement(
                    MakeConditionalStatement("Auto Seg2 => Seg3 Move", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(4000U).AddOutputState(s2_Run).AddOutputState(s3_Run)
                        .AddCondition(inPrinter2Occupied));
            dispatchPrn1 = MakeConditionalStatement("Dispatch print 1 location", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(4000U).AddOutputState(s1_Run).AddOutputState(s2_Run)
                .AddCondition(s2_Sensor);
            dispatchPrn2 = MakeConditionalStatement("Dispatch print 2 location", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(4000U).AddOutputState(s3_Run).AddOutputState(s4_Run)
                .AddCondition(s4_Sensor);
            Conditional conditional = MakeConditionalStatement(string.Format("Auto load precondition CSD:{0}, Loc:{1}", 1, LocationNumber), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                .MakePrecondition().AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts.RollersRun, PIN_STATE.INACTIVE)
                .AddCondition(scripts.LiftRun, PIN_STATE.INACTIVE).AddCondition(scripts.OccupiedBelts, PIN_STATE.INACTIVE).AddCondition(scripts.OccupiedRollers, PIN_STATE.INACTIVE)
                .AddCondition(inPrinter1Occupied, PIN_STATE.INACTIVE).AddCondition(scripts.LoadTriggerNormal).CloseBlock();
            autoLoadPrinter1 = MakeConditionalMacro(string.Format("Auto load script CSD:{0}, Loc:{1}", 1, LocationNumber)).AddStatement(conditional).AddStatement(scripts.LoadNormal)
                .AddStatement(scripts.DispatchNormal);
            autoLoadPrinter1.OnStateChanged += Script_OnStateChanged;
            dispatchPrn1.OnStateChanged += Script_OnStateChanged;
            dispatchPrn2.OnStateChanged += Script_OnStateChanged;
            dispatchPrn1.OnStateChanged += Script_OnStateChanged;
            dispatchPrn2.OnStateChanged += Script_OnStateChanged;
            autoLoadPrinter1.OnStateChanged += Script_OnStateChanged;
        }

        private void Sensor_OnStateChanged(InPin obj)
        {
            Evaluate();
        }

        private void Prn_OnStateChanged(LabelPrinter prn)
        {
            Log(string.Format("PRN:{0} => {1}", prn == prn1 ? 1 : 2, prn.State));
            Evaluate();
        }

        private void ReEvaluateState(LabelPrinter obj)
        {
            Evaluate();
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 1U
                ? MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, prevSegDispatch: scripts.UpstreamStartDispatching)
                : base.LoadNormalScript(csdNum);
        }

        protected override Conditional LoadAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 1U
                ? MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, endDelay: 400U, middleMotorRun: scripts.MiddleRollersRun,
                    middleMotorDir: scripts.MiddleRollersDir)
                : base.LoadAlternativeScript(csdNum);
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 1U
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, inPrinter1Occupied, scripts.OccupiedRollers, csdNum, middleMotorRun: s1_Run)
                : base.DispatchNormalScript(csdNum);
        }

        protected override Conditional DispatchAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 2U
                ? MakeDispatchStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CCW, scripts.DispatchAlternativeSegmentOccupied, scripts.OccupiedRollers, csdNum,
                    middleMotorRun: scripts.MiddleRollersRun)
                : base.DispatchAlternativeScript(csdNum);
        }

        protected override void RequestWmsDirection(string barcode)
        {
            printAt1 = !printAt1;
            if (printAt1)
                prn1.Barcode = barcode;
            else
                prn2.Barcode = barcode;
            prn1.State = printAt1 ? PRINTER_STATE.WAITING_FOR_PACKAGE_PRN : PRINTER_STATE.WAITING_FOR_PACKAGE_NEXT_PRN;
            Log(string.Format("Requesting printer for barcode:{0} (loc:{1})", barcode, LocationNumber));
            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AnmeldungLabeldruck(printAt1, !printAt1, false, false, Encoding.ASCII.GetBytes(barcode), LocationNumber)));
            MessageSent();
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
        }

        private void HandleRebound()
        {
            if (csd2.State != CSD_STATE.OCCUPIED)
            {
                autoLoadPrinter1.Run();
            }
            else
            {
                if (!csd1.IsIdle)
                    return;
                autoLoadPrinter1.Cancel();
                if (!csd1.LoadAlternative())
                    return;
                csd2.DispatchAlternative();
            }
        }

        public override void DoEvaluate()
        {
            HandleRebound();
            if (csd1.IsOccupied && inPrinter1Occupied.Inactive)
                csd1.DispatchNormal();
            HandlePrinter1();
            HandlePrinter2();
        }

        private void HandlePrinter1()
        {
            switch (prn1.State)
            {
                case PRINTER_STATE.PASSTHROUGH:
                case PRINTER_STATE.WAITING_FOR_PACKAGE_NEXT_PRN:
                case PRINTER_STATE.LABEL_APPLIED:
                    if (!inPrinter1Occupied.Active || !inPrinter2Occupied.Inactive || dispatchPrn1.IsRunningOrAboutToBe)
                        break;
                    prn2.State = prn1.State == PRINTER_STATE.WAITING_FOR_PACKAGE_NEXT_PRN ? PRINTER_STATE.WAITING_FOR_PACKAGE_PRN : PRINTER_STATE.PASSTHROUGH;
                    dispatchPrn1.Run();
                    prn1.State = PRINTER_STATE.PASSTHROUGH;
                    break;
                case PRINTER_STATE.WAITING_FOR_PACKAGE_PRN:
                    if (!inPrinter1Occupied.Active)
                        break;
                    switch (prn1.PrintState)
                    {
                        case PRINT_STATE.PRINT_IDLE:
                            if (inPrinter1Occupied.PinStateAge < TimeSpan.FromSeconds(3.0))
                            {
                                ReEvaluate.Start();
                                return;
                            }

                            Log("prn1: PRINST_STATE == IDLE. Forcing passthrough");
                            prn1.State = PRINTER_STATE.PASSTHROUGH;
                            return;
                        case PRINT_STATE.PRINT_PENDING:
                        case PRINT_STATE.PRINT_STARTED:
                            prn1.PrintLabel();
                            return;
                        case PRINT_STATE.PRINT_FINISHED:
                            prn1.ApplyLabel();
                            return;
                        default:
                            return;
                    }
                case PRINTER_STATE.PRINTING_READY:
                case PRINTER_STATE.APPLYING:
                    prn1.ApplyLabel();
                    break;
            }
        }

        private void HandlePrinter2()
        {
            switch (prn2.State)
            {
                case PRINTER_STATE.PASSTHROUGH:
                case PRINTER_STATE.LABEL_APPLIED:
                    if (!inPrinter2Occupied.Active || !s4_Sensor.Inactive || dispatchPrn2.IsRunningOrAboutToBe)
                        break;
                    dispatchPrn2.Run();
                    var barcodeTrigger = BarcodeTrigger;
                    if (barcodeTrigger == null)
                        break;
                    barcodeTrigger.Run();
                    break;
                case PRINTER_STATE.WAITING_FOR_PACKAGE_PRN:
                    if (!inPrinter2Occupied.Active)
                        break;
                    switch (prn2.PrintState)
                    {
                        case PRINT_STATE.PRINT_IDLE:
                            if (inPrinter2Occupied.PinStateAge < TimeSpan.FromSeconds(3.0))
                            {
                                ReEvaluate.Start();
                                return;
                            }

                            Log("prn2: PRINST_STATE == IDLE. Forcing passthrough");
                            prn2.State = PRINTER_STATE.PASSTHROUGH;
                            return;
                        case PRINT_STATE.PRINT_PENDING:
                        case PRINT_STATE.PRINT_STARTED:
                            prn2.PrintLabel();
                            return;
                        case PRINT_STATE.PRINT_FINISHED:
                            prn2.ApplyLabel();
                            return;
                        default:
                            return;
                    }
                case PRINTER_STATE.PRINTING_READY:
                    prn2.ApplyLabel();
                    break;
            }
        }

        public override bool ProcessRxMessage(BaseMessage message)
        {
            switch ((FUNCTION_CODES)message.functionCode)
            {
                case FUNCTION_CODES.FEHLER_LABELDRUCK:
                    var str1 = Encoding.ASCII.GetString((message as FehlerLabeldruck).Barcode).TrimEnd(new char[1]);
                    if (str1.Equals(prn1.Barcode))
                    {
                        prn1.State = PRINTER_STATE.PASSTHROUGH;
                        break;
                    }

                    if (str1.Equals(prn2.Barcode))
                    {
                        prn2.State = PRINTER_STATE.PASSTHROUGH;
                    }

                    break;
                case FUNCTION_CODES.LABELDRUCK_ERFOLGREICH:
                    var labeldruckErfolgreich = message as LabeldruckErfolgreich;
                    var str2 = Encoding.ASCII.GetString(labeldruckErfolgreich.Barcode).TrimEnd(new char[1]);
                    if (str2.Equals(prn1.Barcode) && labeldruckErfolgreich.UseLabelPress1)
                    {
                        prn1.PrintLabel();
                        break;
                    }

                    if (str2.Equals(prn2.Barcode) && labeldruckErfolgreich.UseLabelPress2) prn2.PrintLabel();
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}