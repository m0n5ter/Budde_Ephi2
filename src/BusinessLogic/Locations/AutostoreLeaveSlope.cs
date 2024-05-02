﻿// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Locations.AutostoreLeaveSlope
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
    public class AutostoreLeaveSlope : AutostoreLocation
    {
        private const uint DISP_INTERVAL_ms = 4500;
        private readonly SharedSlopeControl SlopeControl;
        private readonly DelayedEvent deWaitNewDespatch;
        private Conditional DispatchToSlope;
        private InPin inAcmDownstreamOcc;
        private OutPin outDownstreamDispatch;

        public AutostoreLeaveSlope(string IP, uint locationNumber, SharedSlopeControl slopeControl)
            : base(IP, locationNumber)
        {
            SlopeControl = slopeControl;
            slopeControl.CanTransferChanged = Evaluate;
            deWaitNewDespatch = new DelayedEvent(TimeSpan.FromMilliseconds(4500.0), Evaluate);
        }

        protected override void InitPins()
        {
            base.InitPins();
            inAcmDownstreamOcc = MakeIn(PIN._22);
            outDownstreamDispatch = MakeOut(PIN._21);
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            DispatchToSlope = MakeConditionalStatement(string.Format("Dispatch to slope (Loc:{0})", LocId), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddGlobalTimeout(5000U)
                .AddCondition(inAcmDownstreamOcc, PIN_STATE.INACTIVE).AddOutputState(outDownstreamDispatch);
        }

        public override void DoEvaluate()
        {
            base.DoEvaluate();
            HandleSlope();
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            inAcmDownstreamOcc.OnStateChanged += inDownstreamOcc_OnStateChanged;
        }

        private void inDownstreamOcc_OnStateChanged(InPin pin)
        {
            if (!pin.Active)
                return;
            Evaluate();
        }

        private void HandleSlope()
        {
            if (!inAcmDownstreamOcc.Active || deWaitNewDespatch.Running)
                return;
            var slopeControl = SlopeControl;
            if ((slopeControl != null ? slopeControl.CanTransfer ? 1 : 0 : 0) == 0)
                return;
            deWaitNewDespatch.Start();
            DispatchToSlope.Run();
        }
    }
}