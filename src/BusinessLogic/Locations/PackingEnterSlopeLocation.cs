using System;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using PharmaProject.BusinessLogic.Segments;

namespace PharmaProject.BusinessLogic.Locations
{
    public class PackingEnterSlopeLocation : PackingLocation
    {
        private const uint STOP_DELAY_ms = 1000;
        private const uint TOTE_SPACING_ms = 2000;
        private readonly SharedSlopeControl slopeControl;
        private bool acmFull;
        private readonly DelayedEvent DelayedAllowAfterStart;
        private InPin inAcmOccupied;
        private InPin inSlopeTop;
        private DateTime lastAllowed = DateTime.Now;
        private volatile bool makeSpaceAfterSlopeStart;
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
            SlopeDelayedStop = new DelayedEvent(1000U, OnSlopeSleep);
            DelayedAllowAfterStart = new DelayedEvent(2000U, slopeControl.RaiseCanTransferChanged);
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
                    Log("Slope started");
                    SlopeDelayedStop.Stop();
                    outSlopeRun.Activate();
                    outAcmLoad.Activate();
                }
                else
                {
                    Log("Slope Stopped");
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
                Log("ACM on top of slope turned " + (value ? "full" : "empty"));
                
                if (!acmFull)
                {
                    SlopeDelayedStop.Stop();
                
                    if (makeSpaceAfterSlopeStart)
                    {
                        Log("Starting slope with a delayed allow for new tote");
                        DelayedAllowAfterStart.Start();
                    }
                    else
                    {
                        slopeControl.RaiseCanTransferChanged();
                    }

                    SlopeRun = true;
                }
                else
                {
                    DelayedAllowAfterStart.Stop();
                    makeSpaceAfterSlopeStart = LastAllowedAge < TimeSpan.FromMilliseconds(2000.0);
                    
                    if (LastAllowedAge < TimeSpan.FromMilliseconds(1000.0))
                    {
                        Log("Stopping slope with a delay to finish loading tote");
                        SlopeDelayedStop.Start();
                    }
                    else
                    {
                        SlopeRun = false;
                    }
                }
            }
        }

        private bool SlopeLoadAllowed()
        {
            if (AcmFull || DelayedAllowAfterStart.Running)
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

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            inAcmOccupied.OnStateChanged += InAcmOccupied_OnStateChanged;
        }

        private void InAcmOccupied_OnStateChanged(InPin pin)
        {
            if (pin.Inactive)
            {
                AcmFull = false;
                slopeControl.CanTransferChanged();
            }
            else
            {
                AcmFull = true;
            }
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