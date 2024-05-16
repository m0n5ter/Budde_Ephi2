using System;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Segments;
using PharmaProject.BusinessLogic.UTC;

namespace PharmaProject.BusinessLogic.Locations
{
    public class PostPackingSlopeLocation : CSD_UTC
    {
        private const uint DISP_START_STOP_DELAY_ms = 1000;
        private const uint DISP_INTERVAL_ms = 4500;
        private const uint SLOPE_SLEEP_AFTER_SEC = 10;
        private const uint STOP_LOAD_AFTER_SEC = 30;
        private const uint ACM_FULL_DEBOUNCE_ms = 2000;
        private readonly SharedSlopeControl postPackingSlopeControl;
        private readonly SharedSlopeControl preAutoStoreSlopeControl;
        private DateTime acm_ds_FullSet = DateTime.Now;
        private bool acmFull_ds;
        private bool acmFull_us;
        private Conditional DispatchToSlope;
        private InPin inBtn;
        private InPin inDownstreamOcc;
        private InPin inUpstreamOcc;
        private DateTime lastAllowed = DateTime.Now;
        private readonly DelayedEvent LoadDelayedStop;
        private OutPin outDownstreamDispatch;
        private OutPin outSlopeRun;
        private OutPin outUpstreamLoad;
        private readonly DelayedEvent ReEvaluate;
        private readonly DelayedEvent SlopeDelayedStop;
        private bool slopeRun;
        private readonly DelayedEvent SlopeSleep;

        public PostPackingSlopeLocation(
            string IP,
            SharedSlopeControl postPackingSlopeControl,
            SharedSlopeControl preAutoStoreSlopeControl)
            : base(IP, "-", 0U)
        {
            this.postPackingSlopeControl = postPackingSlopeControl;
            this.preAutoStoreSlopeControl = preAutoStoreSlopeControl;
            ReEvaluate = new DelayedEvent(500U, HandleSlope_ds);
            SlopeSleep = new DelayedEvent(TimeSpan.FromSeconds(10.0), () => SlopeRun = false);
            SlopeDelayedStop = new DelayedEvent(1000U, () => SlopeRun = false);
            postPackingSlopeControl.CanTransferPtr = SlopeControl_CanTransfer;
            LoadDelayedStop = new DelayedEvent(TimeSpan.FromSeconds(30.0), () => outUpstreamLoad.Deactivate());
            preAutoStoreSlopeControl.CanTransferChanged = SlopeControl_CanTransferChanged;
            AttachEventHandlers();
        }

        protected override InPin[] ResetEmergencyPins => new InPin[1] { inBtn };

        public bool AcmFull_us
        {
            get => acmFull_us;
            set
            {
                if (acmFull_us == value)
                    return;

                acmFull_us = value;
                postPackingSlopeControl.RaiseCanTransferChanged();
                
                if (value)
                {
                    RestartLoad();
                }
                else
                {
                    LoadDelayedStop.Stop();
                    outUpstreamLoad.Deactivate();
                }
            }
        }

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
                    SlopeSleep.Start();
                    outSlopeRun.Activate();
                }
                else
                {
                    SlopeSleep.Stop();
                    outSlopeRun.Deactivate();
                }
            }
        }

        private TimeSpan Acm_ds_FullAge => DateTime.Now.Subtract(acm_ds_FullSet);

        public bool AcmFull_ds
        {
            get => acmFull_ds;
            set
            {
                if (acmFull_ds == value)
                    return;
                
                acmFull_ds = value;
                acm_ds_FullSet = DateTime.Now;
                
                if (acmFull_ds)
                {
                    if (LastAllowedAge < TimeSpan.FromMilliseconds(1000.0))
                        SlopeDelayedStop.Start();
                    else
                        SlopeRun = false;
                }
                else
                {
                    SlopeDelayedStop.Stop();
                    SlopeRun = true;
                }
            }
        }

        private TimeSpan LastAllowedAge => DateTime.Now.Subtract(lastAllowed);

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._24);
            inUpstreamOcc = MakeIn(PIN._21).LowActive;
            inDownstreamOcc = MakeIn(PIN._22).LowActive;
            outUpstreamLoad = MakeOut(PIN._20);
            outDownstreamDispatch = MakeOut(PIN._21);
            outSlopeRun = MakeOut(PIN._22);
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            DispatchToSlope = MakeConditionalStatement($"Dispatch to slope (Loc:{LocId})", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(5000U)
                .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(inDownstreamOcc, PIN_STATE.INACTIVE).AddGuardBlock(1000U).AddGuardPin(inDownstreamOcc).CloseBlock().CloseBlock()
                .AddOutputState(outDownstreamDispatch);
        }

        private void AttachEventHandlers()
        {
            inUpstreamOcc.OnStateChanged += InUpstreamOcc_OnStateChanged;
            inUpstreamOcc.RegisterDebouncedPinStateHandler(TimeSpan.FromMilliseconds(2000.0), DebouncedInUpstreamOcc);
            inDownstreamOcc.OnStateChanged += inDownstreamOcc_OnStateChanged;
        }

        protected override void InvalidateCsdStates()
        {
        }

        private bool SlopeControl_CanTransfer()
        {
            if (AcmFull_us)
                return false;
            
            RestartLoad();
            return true;
        }

        private void InUpstreamOcc_OnStateChanged(InPin pin)
        {
            if (!pin.Inactive)
                return;
            
            AcmFull_us = false;
        }

        private void DebouncedInUpstreamOcc(InPin pin)
        {
            if (!pin.Active)
                return;
            
            AcmFull_us = true;
        }

        private void RestartLoad()
        {
            outUpstreamLoad.Activate();
            LoadDelayedStop.Start();
        }

        private void SlopeControl_CanTransferChanged()
        {
            var storeSlopeControl = preAutoStoreSlopeControl;
            AcmFull_ds = (storeSlopeControl != null ? storeSlopeControl.CanTransfer ? 1 : 0 : 0) == 0;
            
            if (AcmFull_ds)
                return;
            
            HandleSlope_ds();
        }

        private void inDownstreamOcc_OnStateChanged(InPin pin)
        {
            if (!pin.Active)
                return;
            
            HandleSlope_ds();
        }

        private void HandleSlope_ds()
        {
            if (!inDownstreamOcc.Active)
                return;
            
            var timeSpan = TimeSpan.FromMilliseconds(4500.0);
            
            if (LastAllowedAge < timeSpan)
            {
                ReEvaluate.Start();
            }
            else
            {
                var storeSlopeControl = preAutoStoreSlopeControl;
                AcmFull_ds = (storeSlopeControl != null ? storeSlopeControl.CanTransfer ? 1 : 0 : 0) == 0;
            
                if (AcmFull_ds)
                    return;
                
                if (Acm_ds_FullAge < timeSpan)
                {
                    ReEvaluate.Start();
                }
                else
                {
                    lastAllowed = DateTime.Now;
                    SlopeRun = true;
                    DispatchToSlope.Run();
                }
            }
        }
    }
}