// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Segments.EmptyBoxInfeed
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using System.Net;
using System.Text;
using Ephi.Core.Helping;
using Ephi.Core.Helping.General;
using Ephi.Core.Helping.Log4Net;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using log4net;
using PharmaProject.BusinessLogic.Devices;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.Wms_Communication;
using PharmaProject.BusinessLogic.Wms_Communication.Messages;

namespace PharmaProject.BusinessLogic.Segments
{
    public class EmptyBoxInfeed
    {
        private readonly BarcodeScanner barcodeScanner;
        private readonly OutPin barcodeScannerTrigger;
        private Conditional dispatch;
        private readonly OutPin loadSegment;
        private InPin loadSegmentSensor;
        private readonly ILog log;
        private uint nextPackageID;
        private uint packageID;
        private Conditional scanBarcode;
        private INFEED_STATE state;
        private readonly OutPin weighingMotorRun;
        private readonly InPin weighingSensor;
        private readonly DelayedEvent weighingTimeout;

        public EmptyBoxInfeed(
            InPin weighingSensor,
            InPin loadSegmentSensor,
            OutPin barcodeScannerTrigger,
            OutPin weighingMotorRun,
            OutPin loadSegment,
            string barcodeScannerIP)
        {
            this.weighingSensor = weighingSensor;
            this.loadSegmentSensor = loadSegmentSensor;
            this.barcodeScannerTrigger = barcodeScannerTrigger;
            this.weighingMotorRun = weighingMotorRun;
            this.loadSegment = loadSegment;
            log = Log4NetHelpers.AddIsolatedLogger("iPunkt", "..\\Log\\");
            barcodeScanner = new BarcodeScanner("Loc:110, Barcode Scanner", IPAddress.Parse(barcodeScannerIP));
            barcodeScanner.OnBarcodeScanned += BarcodeScanner_OnBarcodeScanned;
            InitScripts();
            weighingTimeout = new DelayedEvent(TimeSpan.FromMilliseconds(3000.0), () =>
            {
                if (State != INFEED_STATE.WAITING_FOR_WEIGHING)
                    return;
                StartMovement();
            });
            this.weighingSensor.OnStateChanged += WeighingSensor_OnStateChanged;
            StopSequence();
            State = INFEED_STATE.WAITING_FOR_NEW_ORDER;
        }

        public INFEED_STATE State
        {
            get => state;
            set
            {
                if (state == value)
                    return;
                log.InfoFormat("State changed {0} => {1}", state, value);
                state = value;
            }
        }

        ~EmptyBoxInfeed()
        {
            barcodeScanner.OnBarcodeScanned -= BarcodeScanner_OnBarcodeScanned;
            weighingSensor.OnStateChanged -= WeighingSensor_OnStateChanged;
        }

        private void InitScripts()
        {
            scanBarcode = barcodeScannerTrigger.Utc.MakeConditionalStatement("Empty box infeed scanner trigger", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddTimeoutCondition(500U)
                .AddOutputState(barcodeScannerTrigger);
            dispatch = loadSegment.Utc.MakeConditionalStatement("Empty box infeed dispatch", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(3000U)
                .AddCondition(weighingSensor, PIN_STATE.INACTIVE).AddOutputState(weighingMotorRun).AddOutputState(loadSegment);
            dispatch.OnStateChanged += Dispatch_OnStateChanged;
        }

        private void Dispatch_OnStateChanged(Conditional dispScript)
        {
            switch (dispScript.Status)
            {
                case CONDITIONAL_STATE.FINISHED:
                case CONDITIONAL_STATE.CANCELLED:
                    StopSequence();
                    break;
                case CONDITIONAL_STATE.TIMED_OUT:
                    if (State != INFEED_STATE.DISPATCHING)
                        break;
                    StopSequence();
                    break;
            }
        }

        private void WeighingSensor_OnStateChanged(InPin obj)
        {
            if (!obj.Active)
                return;
            if (!Helpers.Contains(State, INFEED_STATE.WAITING_FOR_PACKAGE, INFEED_STATE.WAITING_FOR_SCAN))
                return;
            StartScan();
        }

        private void SendWeighingStartCommandLater(string barcode)
        {
            log.InfoFormat("Weight Tx command scheduled for barcode {0}", barcode);
            DelayedEvent.Once(TimeSpan.FromMilliseconds(1000.0), () =>
            {
                log.InfoFormat("Weight Tx command sending for barcode {0}", barcode);
                WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AnmeldungPackstück(0U, 0U, Encoding.ASCII.GetBytes(barcode), 0U, 0U, 111U)));
                weighingTimeout.Start();
            });
        }

        private void BarcodeScanner_OnBarcodeScanned(string barcode)
        {
            if (State != INFEED_STATE.WAITING_FOR_SCAN || string.IsNullOrEmpty(barcode))
                return;
            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AnmeldungPackstück(packageID, 0U, Encoding.ASCII.GetBytes(barcode), 0U, 0U, 110U)));
            SendWeighingStartCommandLater(barcode);
            State = INFEED_STATE.WAITING_FOR_WEIGHING;
        }

        public void StartSequence(uint packageID)
        {
            if (State != INFEED_STATE.WAITING_FOR_NEW_ORDER)
            {
                nextPackageID = packageID;
            }
            else
            {
                this.packageID = packageID;
                if (StartScan())
                    return;
                State = INFEED_STATE.WAITING_FOR_PACKAGE;
            }
        }

        private bool StartScan()
        {
            if (!weighingSensor.Active)
                return false;
            scanBarcode.Run();
            State = INFEED_STATE.WAITING_FOR_SCAN;
            return true;
        }

        private bool StartMovement()
        {
            State = INFEED_STATE.DISPATCHING;
            dispatch.Run();
            return true;
        }

        public void WeighingDone(WMS_TOTE_DIRECTION direction)
        {
            log.InfoFormat("Direction received: {0}", direction);
            if (State != INFEED_STATE.WAITING_FOR_WEIGHING)
                return;
            if (direction == WMS_TOTE_DIRECTION.DIRECTION_2)
                StartMovement();
            else
                State = INFEED_STATE.WAITING_FOR_NEW_ORDER;
        }

        public void StopSequence()
        {
            if (dispatch.IsRunningOrAboutToBe)
                dispatch.Cancel();
            State = INFEED_STATE.WAITING_FOR_NEW_ORDER;
            if (nextPackageID != 0U && (int)nextPackageID != (int)packageID)
                StartSequence(nextPackageID);
            else
                packageID = 0U;
            nextPackageID = 0U;
        }
    }
}