// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.LabelPrinterRebound
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System.Net;
using System.Text;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.Segments;
using PharmaProject.Wms_Communication;
using PharmaProject.Wms_Communication.Messages;

namespace PharmaProject.Locations
{
    public class LabelPrinterRebound : BaseLocation
    {
        private readonly BarcodeScanner BSTop;
        private string checkCode = string.Empty;
        private CSD csdEndRebound;
        private Conditional dispAcm;
        private readonly Conditional dispatchToCsd1;
        private Conditional loadDisp;
        private OutPin outDispatchAcm;
        private InPin reboundButton;
        private string sideScanned = string.Empty;
        private string topScanned = string.Empty;
        private readonly InPin toteAvlMainTrack;
        private InPin toteAvlRebound;

        public LabelPrinterRebound(
            string IP,
            uint locationNumber,
            InPin lastSensor,
            OutPin lastMotor,
            string BSLeftIP,
            string BSRightIP,
            string BSTopIP)
            : base(IP, locationNumber, 2U)
        {
            AddBarcodeScanner(new BarcodeScanner($"Loc:{locationNumber}, BS Left", IPAddress.Parse(BSLeftIP)));
            AddBarcodeScanner(new BarcodeScanner($"Loc:{locationNumber}, BS Right", IPAddress.Parse(BSRightIP)));
            BSTop = new BarcodeScanner($"Loc:{locationNumber}, BS Top", IPAddress.Parse(BSTopIP));
            toteAvlMainTrack = lastSensor;
            lastSensor.OnStateChanged += LastSensor_OnStateChanged;
            BSTop.OnBarcodeScanned += BSTop_OnBarcodeScanned;
            dispatchToCsd1 = lastMotor.Utc.MakeConditionalStatement("Dispatch post printer location", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(4000U)
                .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(lastSensor, PIN_STATE.INACTIVE).AddGuardBlock(200U).AddGuardPin(lastSensor).CloseBlock().CloseBlock().AddOutputState(lastMotor);
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

        private bool ToteBarcodeValid => !string.IsNullOrEmpty(checkCode) && checkCode.Equals(topScanned);

        protected override InPin[] ResetEmergencyPins => new InPin[1] { reboundButton };

        private void BSTop_OnBarcodeScanned(string barcode)
        {
            topScanned = barcode;
        }

        protected override void OnBarcodeScanned(string barcode)
        {
            lock (SendAllowedLock)
            {
                if (!SendAllowed)
                    return;
                MessageSent();
            }

            sideScanned = barcode;
            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AnmeldungLabeldruck(false, false, false, true, Encoding.ASCII.GetBytes(barcode), LocationNumber)));
        }

        protected override void OnNoRead()
        {
            lock (SendAllowedLock)
            {
                if (!SendAllowed)
                    return;
                MessageSent();
            }

            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AnmeldungLabeldruck(false, false, false, true, Encoding.ASCII.GetBytes(string.Empty), LocationNumber)));
        }

        protected override void InitPins()
        {
            base.InitPins();
            reboundButton = MakeIn(PIN._24);
            toteAvlRebound = MakeIn(PIN._23).HighActive;
            reboundButton.OnStateChanged += ReboundIO_OnStateChanged;
            toteAvlRebound.OnStateChanged += ReboundIO_OnStateChanged;
            var scripts1 = GetScripts(1U);
            scripts1.DownstreamStartLoading = MakeOut(PIN._18);
            scripts1.DispatchNormalSegmentOccupied = MakeIn(PIN._21).LowActive;
            var scripts2 = GetScripts(2U);
            scripts2.DispatchNormalSegmentOccupied = MakeIn(PIN._22).HighActive;
            scripts2.DownstreamStartLoading = MakeOut(PIN._20);
            outDispatchAcm = MakeOut(PIN._19);
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            loadDisp = MakeConditionalBatch("move CSD1 to CSD2").AddGlobalTimeout(TIMEOUT_RANGE.TR_SEC, 4).AddStatement(scripts1.DispatchAlternative).AddStatement(scripts2.LoadAlternative);
            dispAcm = MakeConditionalStatement("Dispatch ACM", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddTimeoutCondition(500U).AddOutputState(outDispatchAcm);
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

        private void LastSensor_OnStateChanged(InPin obj)
        {
            Evaluate();
        }

        public void SetCheckCode(string barcode, string checkCode)
        {
            if (!sideScanned.Equals(barcode))
                return;
            this.checkCode = checkCode;
        }

        public override void DoEvaluate()
        {
            HandleCsdEndRebound();
            switch (csd1.State)
            {
                case CSD_STATE.IDLE:
                    if (toteAvlMainTrack.Active)
                    {
                        if (!dispatchToCsd1.Run())
                            return;
                        if (ToteBarcodeValid)
                        {
                            Log($"TopScanned: {topScanned}, WmsReceived: {checkCode}");
                            if (!csd1.Scripts.DispatchNormalSegmentOccupied.Inactive)
                            {
                                csd1.LoadNormal();
                                break;
                            }

                            csd1.PassThrough();
                            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AufbringenLabel(true, Encoding.ASCII.GetBytes(sideScanned), LocationNumber)));
                            ClearStrings();
                            break;
                        }

                        csd1.LoadNormal();
                    }

                    break;
                case CSD_STATE.OCCUPIED:
                    if (ToteBarcodeValid)
                    {
                        if (csd1.Scripts.DispatchNormalSegmentOccupied.Inactive)
                        {
                            csd1.DispatchNormal();
                            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AufbringenLabel(true, Encoding.ASCII.GetBytes(sideScanned), LocationNumber)));
                            ClearStrings();
                        }

                        break;
                    }

                    if (csd2.IsIdle && loadDisp.Run())
                    {
                        csd1.ForceDispatchPending();
                        csd2.ForceLoadPending();
                        if (!sideScanned.Equals(string.Empty))
                            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AufbringenLabel(false, Encoding.ASCII.GetBytes(sideScanned), LocationNumber)));
                        ClearStrings();
                    }

                    break;
            }

            if (csd2.State != CSD_STATE.OCCUPIED || !csd2.Scripts.DispatchNormalSegmentOccupied.Inactive)
                return;
            csd2.DispatchNormal();
        }

        private void HandleCsdEndRebound()
        {
            if (CsdEndRebound == null || csdEndRebound.State != CSD_STATE.IDLE || !toteAvlRebound.Active || !reboundButton.Active || !CsdEndRebound.LoadNormal())
                return;
            dispAcm.Run();
        }

        private void ClearStrings()
        {
            checkCode = string.Empty;
            topScanned = string.Empty;
            sideScanned = string.Empty;
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
        }

        public override bool ProcessRxMessage(BaseMessage message)
        {
            if (message.functionCode != 43U)
                return false;
            var labeldruckErfolgreich = message as LabeldruckErfolgreich;
            SetCheckCode(Encoding.ASCII.GetString(labeldruckErfolgreich.Barcode).TrimEnd(new char[1]), Encoding.ASCII.GetString(labeldruckErfolgreich.ComparisonBarcode).TrimEnd(new char[1]));
            return true;
        }
    }
}