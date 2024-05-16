// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.AutostoreEnterSlope
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using PharmaProject.Segments;

namespace PharmaProject.Locations
{
    public class AutostoreEnterSlope : AutostoreLocation
    {
        private const uint STOP_LOAD_AFTER_SEC = 10;
        private const uint ACM_FULL_DEBOUNCE_ms = 2000;
        private readonly SharedSlopeControl SlopeControl;
        private bool acmFull;
        private InPin inUpstreamOcc;
        private readonly DelayedEvent LoadDelayedStop;
        private OutPin outUpstreamLoad;

        public AutostoreEnterSlope(string IP, uint locationNumber, SharedSlopeControl slopeControl)
            : base(IP, locationNumber)
        {
            SlopeControl = slopeControl;
            slopeControl.CanTransferPtr = SlopeControl_CanTransfer;
            LoadDelayedStop = new DelayedEvent(TimeSpan.FromSeconds(10.0), () => outUpstreamLoad.Deactivate());
        }

        public bool AcmFull
        {
            get => acmFull;
            set
            {
                if (acmFull == value)
                    return;
                acmFull = value;
                var canTransferChanged = SlopeControl.CanTransferChanged;
                canTransferChanged?.Invoke();
                if (value)
                    return;
                outUpstreamLoad.Deactivate();
                LoadDelayedStop.Stop();
            }
        }

        protected override void InitPins()
        {
            base.InitPins();
            inUpstreamOcc = MakeIn(PIN._22);
            outUpstreamLoad = MakeOut(PIN._21);
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            inUpstreamOcc.OnStateChanged += InUpstreamOcc_OnStateChanged;
            inUpstreamOcc.RegisterDebouncedPinStateHandler(TimeSpan.FromMilliseconds(2000.0), DebouncedInUpstreamOcc);
        }

        private bool SlopeControl_CanTransfer()
        {
            if (AcmFull)
                return false;
            outUpstreamLoad.Activate();
            LoadDelayedStop.Start();
            return true;
        }

        private void InUpstreamOcc_OnStateChanged(InPin pin)
        {
            if (!pin.Inactive)
                return;
            AcmFull = false;
        }

        private void DebouncedInUpstreamOcc(InPin pin)
        {
            if (!pin.Active)
                return;
            AcmFull = true;
        }
    }
}