﻿// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Locations.PackingLeaveSlopeLocation
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Segments;

namespace PharmaProject.BusinessLogic.Locations
{
    public class PackingLeaveSlopeLocation : PackingLocation
    {
        private const uint DISP_START_STOP_DELAY_ms = 1000;
        private const uint DISP_INTERVAL_ms = 4500;
        private const uint SLOPE_SLEEP_AFTER_SEC = 30;
        private readonly SharedSlopeControl SlopeControl;
        private bool acmFull;
        private DateTime acmFullSet = DateTime.Now;
        private Conditional DispatchToSlope;
        private InPin inDownstreamOcc;
        private DateTime lastAllowed = DateTime.Now;
        private OutPin outDownstreamDispatch;
        private OutPin outSlopeRun;
        private readonly DelayedEvent SlopeDelayedStop;
        private bool slopeRun;
        private readonly DelayedEvent SlopeSleep;

        public PackingLeaveSlopeLocation(
            string IP,
            uint locationNumber,
            string bs1IP,
            string bs2IP,
            SharedSlopeControl slopeControl)
            : base(IP, locationNumber, bs1IP, bs2IP)
        {
            SlopeControl = slopeControl;
            slopeControl.CanTransferChanged = SlopeControl_CanTransferChanged;
            SlopeSleep = new DelayedEvent(TimeSpan.FromSeconds(30.0), () => SlopeRun = false);
            SlopeDelayedStop = new DelayedEvent(1000U, () => SlopeRun = false);
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
                Log(string.Format("Slope {0}", value ? "start" : (object)"stop"));
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

        private TimeSpan AcmFullAge => DateTime.Now.Subtract(acmFullSet);

        public bool AcmFull
        {
            get => acmFull;
            set
            {
                if (acmFull == value)
                    return;
                acmFull = value;
                acmFullSet = DateTime.Now;
                if (acmFull)
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
            inDownstreamOcc = MakeIn(PIN._23);
            outSlopeRun = MakeOut(PIN._22);
            outDownstreamDispatch = MakeOut(PIN._24);
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            DispatchToSlope = MakeConditionalStatement(string.Format("Dispatch to slope (Loc:{0})", LocId), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(5000U)
                .AddCondition(inDownstreamOcc, PIN_STATE.INACTIVE).AddOutputState(outDownstreamDispatch);
        }

        public override void DoEvaluate()
        {
            base.DoEvaluate();
            HandleSlope();
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            inDownstreamOcc.OnStateChanged += inDownstreamOcc_OnStateChanged;
        }

        private void SlopeControl_CanTransferChanged()
        {
            var slopeControl = SlopeControl;
            AcmFull = (slopeControl != null ? slopeControl.CanTransfer ? 1 : 0 : 0) == 0;
            if (AcmFull)
                return;
            Evaluate();
        }

        private void inDownstreamOcc_OnStateChanged(InPin pin)
        {
            if (!pin.Active)
                return;
            Evaluate();
        }

        private void HandleSlope()
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
                var slopeControl = SlopeControl;
                AcmFull = (slopeControl != null ? slopeControl.CanTransfer ? 1 : 0 : 0) == 0;
                if (AcmFull)
                    return;
                if (AcmFullAge < timeSpan)
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