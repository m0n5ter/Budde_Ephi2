﻿// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Locations.LabelPrinterRebound
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using System.Net;
using System.Text;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Devices;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.Segments;
using PharmaProject.BusinessLogic.Wms_Communication;
using PharmaProject.BusinessLogic.Wms_Communication.Messages;

namespace PharmaProject.BusinessLogic.Locations
{
    public class LabelPrinterRebound : BaseLocation
    {
        public Conditional BarcodeTrigger;
        private readonly BarcodeScanner BSLeft;
        private readonly BarcodeScanner BSRight;
        private readonly BarcodeScanner BSTop;
        private CSD csdEndRebound;
        private Conditional dispAcm;
        private OutPin outDispatchAcm;
        private OutPin outScannerTrigger;
        private InPin reboundButton;
        internal PrintStationIoSegment printStationSegment;
        private InPin toteAvlRebound;
        private string _sideBarcode;
        private string _topBarcode;

        public LabelPrinterRebound(
            string IP,
            uint locationNumber,
            string BSLeftIP,
            string BSRightIP,
            string BSTopIP)
            : base(IP, locationNumber, 2U)
        {
            //AddBarcodeScanner(new BarcodeScanner($"Loc:{locationNumber}, BS Left", IPAddress.Parse(BSLeftIP)));
            //AddBarcodeScanner(new BarcodeScanner($"Loc:{locationNumber}, BS Right", IPAddress.Parse(BSRightIP)));

            BSLeft = new BarcodeScanner($"Loc:{locationNumber}, BS Left", IPAddress.Parse(BSLeftIP));
            BSRight = new BarcodeScanner($"Loc:{locationNumber}, BS Right", IPAddress.Parse(BSRightIP));
            BSTop = new BarcodeScanner($"Loc:{locationNumber}, BS Top", IPAddress.Parse(BSTopIP));

            BSLeft.OnBarcodeScanned += OnLeftBarcodeScanned;
            BSRight.OnBarcodeScanned += OnRightBarcodeScanned;
            BSTop.OnBarcodeScanned += OnTopBarcodeScanned;        
            BSTop.OnNoRead += OnTopNoRead;
        }

        public CSD CsdEndRebound
        {
            get => csdEndRebound;
            set
            {
                if (csdEndRebound != null)
                    csdEndRebound.OnStateChanged -= CsdEndRebound_OnStateChanged;

                csdEndRebound = value;
                
                if (csdEndRebound == null)
                    return;
                
                csdEndRebound.OnStateChanged += CsdEndRebound_OnStateChanged;
            }
        }

        public CSD_PostPrinting PostPrintCsd => GetCsd(1U) as CSD_PostPrinting;

        protected override InPin[] ResetEmergencyPins => new InPin[1] { reboundButton };

        protected void OnLeftBarcodeScanned(string barcode)
        {
            _sideBarcode = printStationSegment?.Job?.BarcodeSide ?? barcode;
            //"Transport" label (?)
            "Left barcode scanned".WriteLineColor(ConsoleColor.Green);
            log.InfoFormat("Scanned LEFT Barcode: {0}", barcode);
        }

        protected void OnRightBarcodeScanned(string barcode)
        {
            _sideBarcode = printStationSegment?.Job?.BarcodeSide ?? barcode;
            //"Transport" label (?)
            "Right barcode scanned".WriteLineColor(ConsoleColor.Green);
            log.InfoFormat("Scanned RIGHT Barcode: {0}", barcode);
        }

        private void OnTopBarcodeScanned(string barcode)
        {
            _topBarcode = barcode;
            //"Shipment" label (?)
            "Top barcode scanned".WriteLineColor(ConsoleColor.Yellow);
            log.InfoFormat("Scanned TOP Barcode: {0}", barcode);

            CheckBarcodes();
        }

        protected void OnTopNoRead()
        {
            log.Info("---------------------- NOREAD-------------");

            _topBarcode = BarcodeScanner.NO_READ_MSG;
            
            CheckBarcodes();

            //if (!SendAllowed)
            //    return;

            //NoReadSent();

            //Log("Notify WMS NoRead: (Labeling, rebound)");
            //WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AnmeldungLabeldruck(false, false, false, true, Encoding.ASCII.GetBytes("NoRead"), LocationNumber)));
        }

        private void CheckBarcodes()
        {
            $"Sending 46: {_sideBarcode} | {_topBarcode}".WriteLineColor(ConsoleColor.Cyan);

            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new LabelCheckRequest(Encoding.ASCII.GetBytes(_sideBarcode ?? BarcodeScanner.NO_READ_MSG),
                Encoding.ASCII.GetBytes(_topBarcode ?? BarcodeScanner.NO_READ_MSG), LocationNumber)));

            _sideBarcode = null;
            _topBarcode = null;
            
            Evaluate();
        }

        //public bool CheckBarcodeValid(string BcSide, string BcTop, string BcCheck)
        //{
        //    var flag = !string.IsNullOrEmpty(BcSide) && !string.IsNullOrEmpty(BcTop) && !string.IsNullOrEmpty(BcCheck);

        //    if (flag)
        //        flag = BcTop.Equals(BcCheck);

        //    log.InfoFormat(flag ? "BC_VALID bc:{0}, top:{1}, check:{2}" : "BC_INVALID bc:{0}, top:{1}, check:{2}", BcSide, BcTop, BcCheck);
        //    return flag;
        //}

        protected override void InitPins()
        {
            base.InitPins();
            reboundButton = MakeIn(PIN._24);
            toteAvlRebound = MakeIn(PIN._23).HighActive;
            reboundButton.OnStateChanged += ReboundIO_OnStateChanged;
            toteAvlRebound.OnStateChanged += ReboundIO_OnStateChanged;
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            scripts1.DownstreamStartLoading = MakeOut(PIN._18);
            scripts1.DispatchNormalSegmentOccupied = MakeIn(PIN._21).LowActive;
            scripts2.DispatchNormalSegmentOccupied = MakeIn(PIN._22).HighActive;
            scripts2.DownstreamStartLoading = MakeOut(PIN._20);
            outDispatchAcm = MakeOut(PIN._19);
            outScannerTrigger = MakeOut(PIN._24).HighActive;
        }

        protected override void InitScripts()
        {
            base.InitScripts();

            dispAcm = MakeConditionalStatement("Dispatch ACM", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                .AddTimeoutCondition(500U)
                .AddOutputState(outDispatchAcm);

            BarcodeTrigger = MakeConditionalStatement("BarcodeTrigger", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                .AddTimeoutCondition(500U)
                .AddOutputState(outScannerTrigger);
        }

        protected override CSD MakeCSD(uint csdNum)
        {
            return csdNum == 1U ? new CSD_PostPrinting(LocationNumber, csdNum, GetScripts(csdNum), this) : base.MakeCSD(csdNum);
        }

        private void CsdEndRebound_OnStateChanged(CSD obj)
        {
            Evaluate();
        }

        private void ReboundIO_OnStateChanged(InPin pin)
        {
            if (!pin.Active)
                return;
            
            Evaluate();
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            return csdNum == 1U ? MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum) : base.LoadNormalScript(csdNum);
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            return csdNum == 1U
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.RollersRun, scripts.OccupiedRollers, csdNum,
                    nextSegLoad: scripts.DownstreamStartLoading, endDelay: 100U)
                : base.DispatchNormalScript(csdNum);
        }

        private bool _shouldProceed = false;

        public override void DoEvaluate()
        {
            HandleCsdEndRebound();
            var barcodeSide = PostPrintCsd.Job?.BarcodeSide;
            //var barcodeTop = PostPrintCsd.Job?.BarcodeTop;
            //var checkCode = PostPrintCsd.Job?.CheckCode;

            switch (csd1.State)
            {
                case SEGMENT_STATE.OCCUPIED:
                    if (_shouldProceed)
                    {
                        if (csd1.Scripts.DispatchNormalSegmentOccupied.Inactive)
                        {
                            if (csd1.Scripts.DispatchNormalSegmentOccupied.PinStateAge < TimeSpan.FromMilliseconds(500.0))
                            {
                                ReEvaluate.Start();
                                break;
                            }

                            csd1.DispatchNormal();
                            _shouldProceed = false;
                            //WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AufbringenLabel(true, Encoding.ASCII.GetBytes(barcodeSide), LocationNumber)));
                        }

                        break;
                    }

                    if (csd2.IsIdle && !_shouldProceed)
                    {
                        log.InfoFormat("BOX DISAPPROVED bc:{0}", barcodeSide);

                        if (csd2.LoadAlternative() && csd1.DispatchAlternative() && !string.IsNullOrEmpty(barcodeSide))
                        {
                            //WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AufbringenLabel(false, Encoding.ASCII.GetBytes(barcodeSide), LocationNumber)));
                        }
                    }

                    break;
            }

            if (csd2.State != SEGMENT_STATE.OCCUPIED || !csd2.Scripts.DispatchNormalSegmentOccupied.Inactive)
                return;

            csd2.DispatchNormal();
        }

        private void HandleCsdEndRebound()
        {
            if (CsdEndRebound == null || csdEndRebound.State != SEGMENT_STATE.IDLE || !toteAvlRebound.Active || !reboundButton.Active || !CsdEndRebound.LoadNormal())
                return;
            
            dispAcm.Run();
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
        }

        public override bool ProcessRxMessage(BaseMessage message)
        {
            switch ((FUNCTION_CODES)message.functionCode)
            {
                case FUNCTION_CODES.FEHLER_LABELDRUCK:
                    log.InfoFormat("Received Error for Barcode: {0}", Encoding.ASCII.GetString((message as FehlerLabeldruck).Barcode).TrimEnd(new char[1]));
                    break;

                case FUNCTION_CODES.LABEL_CHECK_RESPONSE:
                    _shouldProceed = (message as LabelCheckResponse).ShouldProceed;
                    log.InfoFormat("Received LabelCheckResponse: {0}", _shouldProceed);

                    ReEvaluate.Start();
                    break;            

                case FUNCTION_CODES.LABELDRUCK_ERFOLGREICH:
                    var labeldruckErfolgreich = message as LabeldruckErfolgreich;
                    var str1 = Encoding.ASCII.GetString(labeldruckErfolgreich.Barcode).TrimEnd(new char[1]);
                    var str2 = Encoding.ASCII.GetString(labeldruckErfolgreich.ComparisonBarcode).TrimEnd(new char[1]);
                    log.InfoFormat("Received Check Code: {0} for Barcode: {1}", str2, str1);
                    var job1 = printStationSegment.Job;
                
                    if (str1.Equals(job1?.BarcodeSide))
                    {
                        job1.CheckCode = str2;
                        break;
                    }

                    var job2 = PostPrintCsd.Job;
                    
                    if (str1.Equals(job2?.BarcodeSide))
                    {
                        job2.CheckCode = str2;
                        Evaluate();
                    }

                    break;

                default:
                    return false;
            }

            return true;
        }
    }
}