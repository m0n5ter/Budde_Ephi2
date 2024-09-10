using System;
using System.Net;
using System.Text;
using Ephi.Core.Helping;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Devices;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.Segments;
using PharmaProject.BusinessLogic.Wms_Communication;
using PharmaProject.BusinessLogic.Wms_Communication.Messages;

namespace PharmaProject.BusinessLogic.Locations
{
    public class LabelPrinterLocation : BaseLocation
    {
        private readonly LabelPrinter prn1;
        private readonly LabelPrinter prn2;
        private readonly PrintStationIoSegment s1;
        private readonly PrintStationIoSegment s2;
        private readonly PrintStationIoSegment s3;
        private readonly PrintStationIoSegment s4;
        private Conditional autoLoadCsd1;
        private Conditional BarcodeTrigger;
        private OutPin s1_Run;
        private InPin s1_Sensor;
        private OutPin s2_Run;
        private InPin s2_Sensor;
        private OutPin s3_Run;
        private InPin s3_Sensor;
        private OutPin s4_Run;
        private InPin s4_Sensor;
        private uint whereToPrint;

        public LabelPrinterLocation(
            string IP,
            uint locationNumber,
            string bsprePrintLeftIp,
            string bsprePrintRightIp,
            LabelPrinter prn1,
            LabelPrinter prn2)
            : base(IP, locationNumber, 2U)
        {
            AddBarcodeScanner(new BarcodeScanner("Barcode Scanner Left", IPAddress.Parse(bsprePrintLeftIp)));
            AddBarcodeScanner(new BarcodeScanner("Barcode Scanner Right", IPAddress.Parse(bsprePrintRightIp)));
            this.prn1 = prn1;
            this.prn2 = prn2;
            prn1.OnApplyingChanged += Prn_OnStateChanged;
            prn1.OnDeviceReadyChanged += Prn_OnStateChanged;
            prn2.OnApplyingChanged += Prn_OnStateChanged;
            prn2.OnDeviceReadyChanged += Prn_OnStateChanged;
            s1 = new PrintStationIoSegment(1U, s1_Sensor, s1_Run);
            s2 = new PrintStationIoSegment(2U, s2_Sensor, s2_Run);
            s3 = new PrintStationIoSegment(3U, s3_Sensor, s3_Run);
            s4 = new PrintStationIoSegment(4U, s4_Sensor, s4_Run);
            PrePrintCsd.DownstreamNeighbor = s1;
            s1.DownstreamNeighbor = s2;
            s2.DownstreamNeighbor = s3;
            s3.DownstreamNeighbor = s4;
            PrePrintCsd.AllowDispatch = true;
            s2.AllowDispatch = true;
            s4.AllowDispatch = true;
            s1.OnPrintStationSegmentStateChanged += prn1SegmentStateChanged;
            s2.OnPrintStationSegmentStateChanged += LogIoSegState;
            s3.OnPrintStationSegmentStateChanged += prn2SegmentStateChanged;
            s4.OnPrintStationSegmentStateChanged += LogIoSegState;
        }

        public CSD ReboundEndCsd => csd2;

        private CSD_PrePrinting PrePrintCsd => GetCsd(1U) as CSD_PrePrinting;

        public void HookupRebound(LabelPrinterRebound rebound)
        {
            if (rebound == null)
                return;

            rebound.printStationSegment = s4;
            rebound.CsdEndRebound = ReboundEndCsd;
            s4.DownstreamNeighbor = rebound.PostPrintCsd;
            BarcodeTrigger = rebound.BarcodeTrigger;
        }

        protected override void InitPins()
        {
            base.InitPins();
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            scripts1.LoadTriggerNormal = MakeIn(PIN._19).LowActive;
            scripts1.OccupiedRollers = MakeIn(PIN._4).HighActive;
            scripts1.OccupiedBelts = MakeIn(PIN._3).HighActive;
            scripts1.UpstreamStartDispatching = MakeOut(PIN._18);
            scripts1.DownstreamStartLoading = OutPin.Dummy;
            scripts2.DispatchAlternativeSegmentOccupied = scripts1.OccupiedRollers;
            scripts2.UpstreamStartDispatching = OutPin.Dummy;
            scripts2.DownstreamStartLoading = OutPin.Dummy;
            s1_Sensor = MakeIn(PIN._20).HighActive;
            s2_Sensor = MakeIn(PIN._21).HighActive;
            s3_Sensor = MakeIn(PIN._22).HighActive;
            s4_Sensor = MakeIn(PIN._23).HighActive;
            s1_Run = MakeOut(PIN._19);
            s2_Run = MakeOut(PIN._20);
            s3_Run = MakeOut(PIN._21);
            s4_Run = MakeOut(PIN._22);
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            
            Conditional conditional = MakeConditionalStatement($"Auto load precondition CSD:{1}, Loc:{LocationNumber}", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                .MakePrecondition().AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts1.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts1.RollersRun, PIN_STATE.INACTIVE)
                .AddCondition(scripts1.LiftRun, PIN_STATE.INACTIVE).AddCondition(scripts1.OccupiedBelts, PIN_STATE.INACTIVE).AddCondition(scripts1.OccupiedRollers, PIN_STATE.INACTIVE)
                .AddCondition(scripts1.LoadTriggerNormal).AddCondition(scripts2.OccupiedRollers, PIN_STATE.INACTIVE).AddCondition(scripts2.LiftRun, PIN_STATE.INACTIVE)
                .AddCondition(scripts2.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts2.RollersRun, PIN_STATE.INACTIVE).CloseBlock();
            
            autoLoadCsd1 = MakeConditionalMacro($"Auto load script CSD:{1}, Loc:{LocationNumber}", RUN_MODE.PERMANENTLY).AddStatement(conditional)
                .AddStatement(scripts1.LoadNormal);
            
            autoLoadCsd1.OnStateChanged += Script_OnStateChanged;
        }

        private void prn1SegmentStateChanged(IPrintStationSegment seg)
        {
            LogIoSegState(seg);
            Evaluate();
        }

        private void prn2SegmentStateChanged(IPrintStationSegment seg)
        {
            LogIoSegState(seg);
            
            if (Helpers.Contains(seg.State, SEGMENT_STATE.DISPATCHING, SEGMENT_STATE.DISPATCHING_LOADING))
                BarcodeTrigger?.Run();
            
            Evaluate();
        }

        private void LogIoSegState(IPrintStationSegment seg)
        {
            Log(string.Format("{3}\tIo Segment {0}({1}) => {2}", (seg as PrintStationIoSegment).SegNum, Formatting.TitleCase(seg.Job?.JobType) ?? "???", seg.State, DateTime.Now.TimeOfDay));
        }

        private void Prn_OnStateChanged(LabelPrinter prn)
        {
            Log(string.Format("{2}\tPRN:{0} => Apply:{1}", prn == prn1 ? 1 : 2, Formatting.TitleCase(prn.ApplyingState), DateTime.Now.TimeOfDay));
            Evaluate();
        }

        protected override CSD MakeCSD(uint csdNum)
        {
            return csdNum == 1U ? new CSD_PrePrinting(LocationNumber, csdNum, GetScripts(csdNum), this) : base.MakeCSD(csdNum);
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
                ? MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedBelts, csdNum, endDelay: 400U, middleMotorRun: scripts.MiddleRollersRun,
                    middleMotorDir: scripts.MiddleRollersDir)
                : base.LoadAlternativeScript(csdNum);
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            return csdNum == 1U
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.RollersRun, scripts.OccupiedRollers, csdNum, endDelay: 100U)
                : null;
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
            BarcodeSent();
            
            if (!prn1.DeviceReady && !prn2.DeviceReady)
            {
                Log($"Both printers inactive. passthrough for barcode:{barcode} (loc:{LocationNumber})");
                PrePrintCsd.Job = PrintSegJob.Make(PRINT_SEG_JOB_TYPE.NO_PRINT, barcode);
            }
            else
            {
                var printer1Available = prn1.DeviceReady && (!prn2.DeviceReady || whereToPrint++ % 4U > 1U);
                PrePrintCsd.Job = PrintSegJob.Make(printer1Available ? PRINT_SEG_JOB_TYPE.PRINT_AT_1 : PRINT_SEG_JOB_TYPE.PRINT_AT_2, barcode);
                Log($"Requesting printer for barcode:{barcode} (loc:{LocationNumber})");
                WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AnmeldungLabeldruck(printer1Available, !printer1Available, false, false, Encoding.ASCII.GetBytes(barcode), LocationNumber)));
            }
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
        }

        private void HandleRebound()
        {
            if (!csd2.IsOccupied || !csd1.IsIdle || !csd1.LoadAlternative())
                return;
            
            PrePrintCsd.Job = PrintSegJob.Make(PRINT_SEG_JOB_TYPE.NO_PRINT);
            csd2.DispatchAlternative();
        }

        public override void DoEvaluate()
        {
            HandleRebound();
            HandlePrinter(prn1);
            HandlePrinter(prn2);
        }

        private void HandlePrinter(LabelPrinter prn)
        {
            PrintStationIoSegment stationIoSegment1;
            PRINT_SEG_JOB_TYPE printSegJobType;
            
            if (prn == prn1)
            {
                stationIoSegment1 = s1;
                printSegJobType = PRINT_SEG_JOB_TYPE.PRINT_AT_1;
            }
            else
            {
                stationIoSegment1 = s3;
                printSegJobType = PRINT_SEG_JOB_TYPE.PRINT_AT_2;
            }

            if (stationIoSegment1.State == SEGMENT_STATE.OCCUPIED)
            {
                var stationIoSegment2 = stationIoSegment1;
                var job = stationIoSegment1.Job;
                var num = (job?.JobType ?? PRINT_SEG_JOB_TYPE.NO_PRINT) != printSegJobType ? 1 : 0;
                stationIoSegment2.AllowDispatch = num != 0;
            
                if (stationIoSegment1.AllowDispatch || !prn.DeviceReady)
                    return;
                
                switch (prn.ApplyingState)
                {
                    case APPLYING_STATE.WAITING_FOR_PRINT:
                        if (stationIoSegment1.StateAge > TimeSpan.FromSeconds(10.0))
                        {
                            log.WarnFormat("Not printing on printer {0} withing 10 sec. Forced apply", Formatting.TitleCase(printSegJobType));
                            stationIoSegment1.Job.JobType = PRINT_SEG_JOB_TYPE.PRINT_FAILED;
                            break;
                        }

                        ReEvaluate.Start();
                        break;
                
                    case APPLYING_STATE.LABEL_READY:
                        prn.ApplyLabel();
                        break;
                    
                    case APPLYING_STATE.APPLYING_READY:
                        stationIoSegment1.Job.JobType = PRINT_SEG_JOB_TYPE.PRINT_SUCCESS;
                        break;
                    
                    case APPLYING_STATE.APPLYING_FAILED:
                        log.WarnFormat("Applying failed on printer {0} withing 10 sec. Moving on", Formatting.TitleCase(printSegJobType));
                        stationIoSegment1.Job.JobType = PRINT_SEG_JOB_TYPE.PRINT_FAILED;
                        break;
                }

                if (stationIoSegment1.Job.JobType == printSegJobType)
                    return;
                
                prn.Reset();
                stationIoSegment1.AllowDispatch = true;
            }
            else
            {
                prn.Reset();
                stationIoSegment1.AllowDispatch = false;
            }
        }

        public override bool ProcessRxMessage(BaseMessage message)
        {
            switch ((FUNCTION_CODES)message.functionCode)
            {
                case FUNCTION_CODES.FEHLER_LABELDRUCK:
                    PrePrintCsd.SetJobType(Encoding.ASCII.GetString((message as FehlerLabeldruck).Barcode).TrimEnd(new char[1]), PRINT_SEG_JOB_TYPE.NO_PRINT);
                    break;
                
                case FUNCTION_CODES.LABELDRUCK_ERFOLGREICH:
                    var labeldruckErfolgreich = message as LabeldruckErfolgreich;
                    var forBarcode = Encoding.ASCII.GetString(labeldruckErfolgreich.Barcode).TrimEnd(new char[1]);
                    PrePrintCsd.GetJobType(forBarcode);
                    var jobType = PrePrintCsd.GetJobType(forBarcode);
                
                    if (jobType.HasValue)
                        switch (jobType.GetValueOrDefault())
                        {
                            case PRINT_SEG_JOB_TYPE.PRINT_AT_1:
                                if (!labeldruckErfolgreich.UseLabelPress1) PrePrintCsd.SetJobType(forBarcode, PRINT_SEG_JOB_TYPE.NO_PRINT);
                                break;
                            case PRINT_SEG_JOB_TYPE.PRINT_AT_2:
                                if (!labeldruckErfolgreich.UseLabelPress2) PrePrintCsd.SetJobType(forBarcode, PRINT_SEG_JOB_TYPE.NO_PRINT);
                                break;
                        }
                    break;

                default:
                    return false;
            }

            return true;
        }
    }
}