// Decompiled with JetBrains decompiler
// Type: PharmaProject.LabelPrinter
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using Ephi.Core.Helping;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;

namespace PharmaProject
{
    public class LabelPrinter
    {
        private Conditional apply;
        private string barcode = string.Empty;
        private readonly InPin inApplyReady;
        private readonly InPin inDataReady;
        private readonly InPin inDeviceReady;
        private readonly InPin inPrintready;
        private readonly InPin inWarning;
        private readonly OutPin outApply;
        private readonly OutPin outPrint;
        private Conditional print;
        private PRINT_STATE printState;
        private PRINTER_STATE state;

        public LabelPrinter(
            InPin sync,
            InPin end,
            InPin ready,
            InPin warning,
            InPin dataReady,
            OutPin apply,
            OutPin print)
        {
            inApplyReady = sync;
            inPrintready = end;
            inDeviceReady = ready;
            inWarning = warning;
            inDataReady = dataReady;
            outApply = apply;
            outPrint = print;
            InitScrips();
            inDataReady.OnStateChanged += IO_Changed;
            inWarning.OnStateChanged += IO_Changed;
        }

        public PRINTER_STATE State
        {
            get => state;
            set
            {
                if (state == value)
                    return;
                state = value;
                var onStateChanged = OnStateChanged;
                if (onStateChanged == null)
                    return;
                onStateChanged(this);
            }
        }

        public string Barcode
        {
            get => barcode;
            set
            {
                barcode = value;
                PrintState = PRINT_STATE.PRINT_IDLE;
            }
        }

        public PRINT_STATE PrintState
        {
            get => printState;
            set
            {
                if (printState == value)
                    return;
                Console.WriteLine("PRINTER STATE: {0} => {1}", printState, value);
                printState = value;
            }
        }

        public bool DataReady => inDataReady.Active;

        public bool DeviceReady => inDeviceReady.Active;

        public bool DeviceWarning => !inWarning.Inactive;

        public bool CanPrint => DataReady && !DeviceWarning && DeviceReady;

        public event Action<LabelPrinter> OnStateChanged;

        private void InitScrips()
        {
            print = outPrint.Utc.MakeConditionalStatement("Print command", OUTPUT_ENFORCEMENT.ENF_NEGATE_WHEN_TRUE).AddGlobalTimeout(5000U).AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(inPrintready)
                .AddGuardBlock(100U).AddGuardPin(inPrintready).CloseBlock().CloseBlock().AddOutputState(outPrint);
            apply = outApply.Utc.MakeConditionalStatement("Apply command", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(5000U).AddLogicBlock(LOGIC_FUNCTION.AND)
                .AddCondition(inApplyReady).AddGuardBlock(100U).AddGuardPin(inApplyReady).CloseBlock().CloseBlock().AddOutputState(outApply);
            print.OnStateChanged += Script_Changed;
            apply.OnStateChanged += Script_Changed;
        }

        private void Script_Changed(Conditional obj)
        {
            EvalPrinterIO();
            Console.WriteLine("PRINTER SCRIPT: {0} => {1}", obj.Name, obj.Status);
        }

        private void IO_Changed(InPin obj)
        {
            EvalPrinterIO();
        }

        private void EvalPrinterIO()
        {
            switch (PrintState)
            {
                case PRINT_STATE.PRINT_PENDING:
                    PrintLabel();
                    break;
                case PRINT_STATE.PRINT_STARTED:
                    if (!print.IsRunningOrAboutToBe)
                    {
                        PrintState = PRINT_STATE.PRINT_FINISHED;
                    }

                    break;
            }

            switch (State)
            {
                case PRINTER_STATE.PRINTING:
                    if (PrintState != PRINT_STATE.PRINT_FINISHED)
                        break;
                    State = PRINTER_STATE.PRINTING_READY;
                    break;
                case PRINTER_STATE.APPLYING:
                    if (apply.IsRunningOrAboutToBe)
                        break;
                    State = PRINTER_STATE.LABEL_APPLIED;
                    break;
                default:
                    var onDoEvaluate = OnDoEvaluate;
                    if (onDoEvaluate == null)
                        break;
                    onDoEvaluate(this);
                    break;
            }
        }

        public event Action<LabelPrinter> OnDoEvaluate;

        public bool PrintLabel()
        {
            if (Helpers.Contains(PrintState, PRINT_STATE.PRINT_STARTED, PRINT_STATE.PRINT_FINISHED) || print.IsRunningOrAboutToBe)
                return true;
            if (!CanPrint)
            {
                PrintState = PRINT_STATE.PRINT_PENDING;
                return false;
            }

            print.Run();
            PrintState = PRINT_STATE.PRINT_STARTED;
            return true;
        }

        public bool ApplyLabel()
        {
            if (apply.IsRunningOrAboutToBe)
                return true;
            apply.Run();
            State = PRINTER_STATE.APPLYING;
            return true;
        }
    }
}