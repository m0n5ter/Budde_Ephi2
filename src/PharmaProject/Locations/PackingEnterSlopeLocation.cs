// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.PackingEnterSlopeLocation
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using PharmaProject.Segments;

namespace PharmaProject.Locations
{
    public class PackingEnterSlopeLocation : PackingLocation
    {
        private const uint STOP_DELAY_ms = 1000;
        private readonly SharedSlopeControl slopeControl;
        private bool acmFull;
        private InPin inAcmOccupied;
        private InPin inSlopeTop;
        private DateTime lastAllowed = DateTime.Now;
        private OutPin outAcmLoad;
        private OutPin outSlopeRun;
        private readonly DelayedEvent SlopeDelayedStop;
        private bool slopeRun;
        private readonly DelayedEvent SlopeSleep;

        public PackingEnterSlopeLocation(
            string IP,
            uint locationNumber,
            string bs1IP,
            string bs2IP,
            SharedSlopeControl slopeControl)
            : base(IP, locationNumber, bs1IP, bs2IP)
        {
            this.slopeControl = slopeControl;
            slopeControl.CanTransferPtr = SlopeLoadAllowed;
            SlopeSleep = new DelayedEvent(TimeSpan.FromSeconds(30.0), OnSlopeSleep);
            SlopeDelayedStop = new DelayedEvent(TimeSpan.FromMilliseconds(1000.0), OnSlopeSleep);
        }

        private TimeSpan LastAllowedAge => DateTime.Now.Subtract(lastAllowed);

        public bool SlopeRun
        {
            get => slopeRun;
            set
            {
                if (value)
                    SlopeSleep.Start();
                if (slopeRun == value)
                    return;
                slopeRun = value;
                if (slopeRun)
                {
                    SlopeDelayedStop.Stop();
                    outSlopeRun.Activate();
                    outAcmLoad.Activate();
                }
                else
                {
                    SlopeSleep.Stop();
                    outSlopeRun.Deactivate();
                    outAcmLoad.Deactivate();
                }
            }
        }

        public bool AcmFull
        {
            get => acmFull;
            set
            {
                if (acmFull == value)
                    return;
                acmFull = value;
                if (!acmFull)
                {
                    SlopeDelayedStop.Stop();
                    slopeControl.CanTransferChanged();
                    SlopeRun = true;
                }
                else if (LastAllowedAge < TimeSpan.FromMilliseconds(1000.0))
                {
                    SlopeDelayedStop.Start();
                }
                else
                {
                    SlopeRun = false;
                }
            }
        }

        private bool SlopeLoadAllowed()
        {
            if (AcmFull)
                return false;
            lastAllowed = DateTime.Now;
            SlopeRun = true;
            return true;
        }

        protected override void InitPins()
        {
            base.InitPins();
            inSlopeTop = MakeIn(PIN._19);
            inAcmOccupied = MakeIn(PIN._20).LowActive;
            outSlopeRun = MakeOut(PIN._22);
            outAcmLoad = MakeOut(PIN._19);
        }

        protected override void InitScripts()
        {
            base.InitScripts();
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            inAcmOccupied.OnStateChanged += InAcmOccupied_OnStateChanged;
            inAcmOccupied.RegisterDebouncedPinStateHandler(TimeSpan.FromMilliseconds(1000.0), DebouncedAcmSegOccupied);
        }

        private void InAcmOccupied_OnStateChanged(InPin obj)
        {
            if (!obj.Inactive)
                return;
            AcmFull = false;
            slopeControl.CanTransferChanged();
        }

        private void DebouncedAcmSegOccupied(InPin pin)
        {
            if (!pin.Active)
                return;
            AcmFull = true;
        }

        private void OnSlopeSleep()
        {
            SlopeRun = false;
        }
    }
}