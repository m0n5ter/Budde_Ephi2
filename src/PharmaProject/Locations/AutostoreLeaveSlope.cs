// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.AutostoreLeaveSlope
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.Segments;

namespace PharmaProject.Locations
{
    public class AutostoreLeaveSlope : AutostoreLocation
    {
        private const uint DISP_INTERVAL_ms = 4500;
        private readonly SharedSlopeControl SlopeControl;
        private Conditional DispatchToSlope;
        private InPin inDownstreamOcc;
        private DateTime lastAllowed = DateTime.Now;
        private OutPin outDownstreamDispatch;
        private readonly DelayedEvent ReEvaluate;

        public AutostoreLeaveSlope(string IP, uint locationNumber, SharedSlopeControl slopeControl)
            : base(IP, locationNumber)
        {
            SlopeControl = slopeControl;
            slopeControl.CanTransferChanged = Evaluate;
            ReEvaluate = new DelayedEvent(TimeSpan.FromMilliseconds(500.0), Evaluate);
        }

        private TimeSpan LastAllowedAge => DateTime.Now.Subtract(lastAllowed);

        protected override void InitPins()
        {
            base.InitPins();
            inDownstreamOcc = MakeIn(PIN._22);
            outDownstreamDispatch = MakeOut(PIN._21);
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
            if (LastAllowedAge < TimeSpan.FromMilliseconds(4500.0))
            {
                ReEvaluate.Start();
            }
            else
            {
                lastAllowed = DateTime.Now;
                var slopeControl = SlopeControl;
                if ((slopeControl != null ? slopeControl.CanTransfer ? 1 : 0 : 0) == 0)
                    return;
                DispatchToSlope.Run();
            }
        }
    }
}