// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.StrapperLocation
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System.Net;
using System.Text;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using PharmaProject.Wms_Communication;
using PharmaProject.Wms_Communication.Messages;

namespace PharmaProject.Locations
{
    public class StrapperLocation : BaseLocation
    {
        private string barcode = string.Empty;
        private readonly BarcodeScanner BSTop;
        private OutPin dispatchSegment;
        private readonly DelayedEvent endAction;
        private InPin firstSegmentOccupied;
        private InPin inBtn;
        private OutPin loadSegment;
        private InPin loadTrigger;
        private InPin secondSegmentOccupied;
        private readonly DelayedEvent stopDispatching;
        private readonly DelayedEvent stopLoading;
        private InPin strapperReady;
        private bool strappingNeeded = true;
        private OutPin toStrap;

        public StrapperLocation(string IP, uint locationNumber, string BSTopIP)
            : base(IP, locationNumber, 0U)
        {
            stopDispatching = new DelayedEvent(500U, () => dispatchSegment.Deactivate());
            stopLoading = new DelayedEvent(10000U, () => loadSegment.Deactivate());
            endAction = new DelayedEvent(1500U, () =>
            {
                if (barcode.Equals(string.Empty))
                    return;
                var direction = strappingNeeded ? WMS_TOTE_DIRECTION.DIRECTION_1 : WMS_TOTE_DIRECTION.DIRECTION_2;
                WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(direction, Encoding.ASCII.GetBytes(barcode), LocationNumber)));
                NdwConnectCommunicator.DirectionSentUpdate(LocationNumber, barcode, direction);
                barcode = string.Empty;
                strappingNeeded = true;
            });
            BSTop = new BarcodeScanner("Strapper BS Top", IPAddress.Parse(BSTopIP));
            BSTop.OnBarcodeScanned += OnBarcodeScanned;
            loadTrigger.OnStateChanged += Sensor_OnStateChanged;
            loadTrigger.OnStateChanged += LoadTrigger_OnStateChanged;
            firstSegmentOccupied.OnStateChanged += Sensor_OnStateChanged;
            secondSegmentOccupied.OnStateChanged += Sensor_OnStateChanged;
            strapperReady.OnStateChanged += Sensor_OnStateChanged;
        }

        protected override InPin[] ResetEmergencyPins => new InPin[1] { inBtn };

        private void LoadTrigger_OnStateChanged(InPin obj)
        {
            if (!obj.Inactive)
                return;
            LoadSegment();
            endAction.Start();
        }

        private void Sensor_OnStateChanged(InPin obj)
        {
            Evaluate();
        }

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._24);
            loadTrigger = MakeIn(PIN._20).LowActive;
            firstSegmentOccupied = MakeIn(PIN._21).LowActive;
            secondSegmentOccupied = MakeIn(PIN._22).LowActive;
            dispatchSegment = MakeOut(PIN._19);
            loadSegment = MakeOut(PIN._20);
            strapperReady = MakeIn(PIN._1);
            toStrap = MakeOut(PIN._1).LowActive;
        }

        private void DispatchSegment()
        {
            dispatchSegment.Activate();
            stopDispatching.Start();
        }

        private void LoadSegment()
        {
            loadSegment.Activate();
            stopLoading.Start();
            barcode = string.Empty;
        }

        public override void DoEvaluate()
        {
            if (!loadTrigger.Active || !strapperReady.Active || !secondSegmentOccupied.Inactive)
                return;
            DispatchSegment();
            if (strappingNeeded)
                toStrap.Activate();
            else
                toStrap.Deactivate();
        }

        protected override void OnBarcodeScanned(string barcode)
        {
            this.barcode = barcode;
            base.OnBarcodeScanned(barcode);
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            if (!this.barcode.Equals(barcode))
                return;
            if (target == WMS_TOTE_DIRECTION.DIRECTION_2)
                strappingNeeded = false;
            else
                strappingNeeded = true;
        }
    }
}