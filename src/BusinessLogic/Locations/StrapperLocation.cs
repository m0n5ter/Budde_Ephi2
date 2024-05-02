// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Locations.StrapperLocation
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System.Net;
using System.Text;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Devices;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.Wms_Communication;
using PharmaProject.BusinessLogic.Wms_Communication.Messages;

namespace PharmaProject.BusinessLogic.Locations
{
    public class StrapperLocation : BaseLocation
    {
        private string barcode = string.Empty;
        private readonly BarcodeScanner BSTop;
        private OutPin dispatchSegment;
        private Conditional dispatchToStrapper;
        private bool doSkipStrapping;
        private OutPin doStrap;
        private InPin firstSegmentOccupied;
        private InPin inBtn;
        private OutPin loadSegment;
        private InPin preStrapOccupied;
        private InPin secondSegmentOccupied;
        private Conditional skipStrapping;
        private readonly DelayedEvent stopLoading;
        private InPin strapperReady;
        private bool wmsFeedbackReceived;

        public StrapperLocation(string IP, uint locationNumber, string BSTopIP)
            : base(IP, locationNumber, 0U)
        {
            stopLoading = new DelayedEvent(10000U, StopLoading);
            BSTop = new BarcodeScanner("Strapper BS Top", IPAddress.Parse(BSTopIP));
            BSTop.OnBarcodeScanned += OnBarcodeScanned;
            InitScrpits();
            preStrapOccupied.OnStateChanged += PreStrapOccupied_OnStateChanged;
            preStrapOccupied.OnStateChanged += Sensor_OnStateChanged;
            secondSegmentOccupied.OnStateChanged += Sensor_OnStateChanged;
            strapperReady.OnStateChanged += Sensor_OnStateChanged;
        }

        private bool WmsFeedbackReceived
        {
            get => wmsFeedbackReceived;
            set
            {
                if (wmsFeedbackReceived == value)
                    return;
                wmsFeedbackReceived = value;
                if (!value)
                    return;
                Evaluate();
            }
        }

        protected override InPin[] ResetEmergencyPins => new InPin[1] { inBtn };

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._24);
            preStrapOccupied = MakeIn(PIN._20).LowActive;
            firstSegmentOccupied = MakeIn(PIN._21).LowActive;
            secondSegmentOccupied = MakeIn(PIN._22).LowActive;
            dispatchSegment = MakeOut(PIN._19);
            loadSegment = MakeOut(PIN._20);
            strapperReady = MakeIn(PIN._1);
            doStrap = MakeOut(PIN._1).LowActive;
        }

        private void InitScrpits()
        {
            dispatchToStrapper = MakeConditionalStatement("Pre-strapper dispatch", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(4000U)
                .AddCondition(preStrapOccupied, PIN_STATE.INACTIVE).AddOutputState(dispatchSegment);
            skipStrapping = MakeConditionalStatement("Skip strapping", OUTPUT_ENFORCEMENT.ENF_NEGATE_WHEN_TRUE).AddCondition(preStrapOccupied).AddOutputState(doStrap);
            dispatchToStrapper.OnStateChanged += DispatchToStrapper_OnStateChanged;
        }

        private void StartLoading()
        {
            loadSegment.Activate();
            stopLoading.Start();
        }

        private void StopLoading()
        {
            loadSegment.Deactivate();
        }

        private void DispatchToStrapper_OnStateChanged(Conditional disp)
        {
            switch (disp.Status)
            {
                case CONDITIONAL_STATE.FINISHED:
                    try
                    {
                        if (string.IsNullOrEmpty(barcode))
                            break;
                        var direction = doSkipStrapping ? WMS_TOTE_DIRECTION.DIRECTION_2 : WMS_TOTE_DIRECTION.DIRECTION_1;
                        WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(direction, Encoding.ASCII.GetBytes(barcode), LocationNumber)));
                        NdwConnectCommunicator.DirectionSentUpdate(LocationNumber, barcode, direction);
                        break;
                    }
                    finally
                    {
                        doSkipStrapping = false;
                        barcode = string.Empty;
                    }
                case CONDITIONAL_STATE.TIMED_OUT:
                    ReEvaluate.Start();
                    break;
            }
        }

        private void PreStrapOccupied_OnStateChanged(InPin pin)
        {
            if (pin.Inactive)
                StartLoading();
            if (!pin.Active)
                return;
            WaitWmsFeedbackStart();
        }

        protected override void OnBarcodeScanned(string barcode)
        {
            this.barcode = barcode;
            base.OnBarcodeScanned(barcode);
            WaitWmsFeedbackStart();
        }

        private void Sensor_OnStateChanged(InPin obj)
        {
            Evaluate();
        }

        public override void DoEvaluate()
        {
            if (!strapperReady.Active)
            {
                log.Warn("Strapper not ready");
            }
            else if (!preStrapOccupied.Active)
            {
                log.Warn("No tote to dispatch");
            }
            else if (!secondSegmentOccupied.Inactive)
            {
                log.Warn("No room to load");
            }
            else if (dispatchToStrapper.IsRunningOrAboutToBe)
            {
                log.Warn("Dispatching already in progress");
            }
            else if (!WmsFeedbackReceived && !WaitWmsFeedbackTimedout)
            {
                log.Warn("WMS did not respond yet");
            }
            else
            {
                if (doSkipStrapping)
                    skipStrapping.Run();
                dispatchToStrapper.Run();
                WmsFeedbackReceived = false;
            }
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            if (string.IsNullOrEmpty(this.barcode))
            {
                WmsFeedbackReceived = true;
            }
            else
            {
                if (!this.barcode.Equals(barcode))
                    return;
                doSkipStrapping = target == WMS_TOTE_DIRECTION.DIRECTION_1;
                WmsFeedbackReceived = true;
            }
        }
    }
}