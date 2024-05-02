// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Locations.MainTrackCrossingLocation_PblA
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.Segments;

namespace PharmaProject.BusinessLogic.Locations
{
    public class MainTrackCrossingLocation_PblA : MainTrackCrossingLocation_Base
    {
        private const uint DISP_START_DELAY_ms = 4500;
        private const uint SLOPE_SLEEP_AFTER_SEC = 30;
        private bool acmFull;
        private readonly DelayedEvent deDispatchDelayedStart;
        private InPin inMainOcc1;
        private InPin inMainOcc2;
        private OutPin outMainAcmLoad;
        private OutPin outSlopeRun1;
        private bool slopeRun;
        private readonly DelayedEvent SlopeSleep;

        public MainTrackCrossingLocation_PblA(
            string IP,
            uint locationNumber,
            string csd1BS1Ip,
            string csd1BS2Ip,
            string csd2BS1Ip,
            SharedSlopeControl slopeControl)
            : base(IP, locationNumber, csd1BS1Ip, csd1BS2Ip, csd2BS1Ip)
        {
            var crossingLocationPblA = this;
            slopeControl.CanTransferPtr = SlopeLoadAllowed;
            SlopeSleep = new DelayedEvent(TimeSpan.FromSeconds(30.0), () => crossingLocationPblA.SlopeRun = false);
            deDispatchDelayedStart = new DelayedEvent(4500U, () =>
            {
                crossingLocationPblA.log.Info("Allowing upstream ACM to dispatch to slope");
                slopeControl.CanTransferChanged();
            });
        }

        public bool SlopeRun
        {
            get => slopeRun;
            set
            {
                if (value)
                    SlopeSleep.Start();
                else
                    SlopeSleep.Stop();
                if (slopeRun == value)
                    return;
                Log(string.Format("Slope {0}", value ? "start" : (object)"stop"));
                slopeRun = value;
                if (slopeRun)
                {
                    outSlopeRun1.Activate();
                    outMainAcmLoad.Activate();
                }
                else
                {
                    outSlopeRun1.Deactivate();
                    outMainAcmLoad.Deactivate();
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
                if (acmFull)
                {
                    log.InfoFormat("ACM Turned full. Stopping slope");
                    deDispatchDelayedStart.Stop();
                    SlopeRun = false;
                }
                else
                {
                    log.InfoFormat("ACM Turned empty. Starting slope");
                    deDispatchDelayedStart.Start();
                    SlopeRun = true;
                }
            }
        }

        protected override void AttachEventHandlers()
        {
            base.AttachEventHandlers();
            inMainOcc1.OnStateChanged += InMainOcc_OnStateChanged;
            inMainOcc2.OnStateChanged += InMainOcc_OnStateChanged;
        }

        private bool SlopeLoadAllowed()
        {
            if (AcmFull || deDispatchDelayedStart.Running)
                return false;
            SlopeRun = true;
            return true;
        }

        private void InMainOcc_OnStateChanged(InPin obj)
        {
            AcmFull = inMainOcc1.Active && inMainOcc2.Active;
        }

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._24);
            inMainOcc1 = MakeIn(PIN._23).LowActive;
            inMainOcc2 = MakeIn(PIN._19).LowActive;
            outSlopeRun1 = MakeOut(PIN._22);
            outMainAcmLoad = MakeOut(PIN._17);
            var scripts1 = GetScripts(1U);
            var scripts2 = GetScripts(2U);
            scripts1.BeltsDir = scripts1.BeltsDir.LowActive;
            scripts2.DownstreamStartLoading = MakeOut(PIN._21);
            scripts2.DispatchNormalSegmentOccupied = MakeIn(PIN._15).LowActive;
            scripts2.LoadTriggerNormal = MakeIn(PIN._17).HighActive;
            scripts2.UpstreamStartDispatching = MakeOut(PIN._14);
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var conditional = base.DispatchNormalScript(csdNum);
            if (csdNum != 1U)
                return conditional;
            var scripts = GetScripts(csdNum);
            return MakeConditionalMacro("Straighten box to the side")
                .AddStatement(MakeConditionalBatch("Align while raise").AddStatement(scripts.TableUp).AddStatement(MakeConditionalStatement("PreAlign", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                    .AddGlobalTimeout(2000U).AddCondition(scripts.LiftRun, PIN_STATE.INACTIVE).AddOutputState(scripts.BeltsRun).AddOutputState(scripts.BeltsDir, PIN_STATE.INACTIVE))).AddStatement(
                    MakeConditionalBatch("Align while lower").AddStatement(scripts.TableDown).AddStatement(MakeConditionalStatement("PostAlign", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                        .AddGlobalTimeout(2000U).AddCondition(scripts.LiftRun, PIN_STATE.INACTIVE).AddOutputState(scripts.BeltsRun).AddOutputState(scripts.BeltsDir, PIN_STATE.INACTIVE)))
                .AddStatement(conditional);
        }

        protected override Conditional LoadAlternativeScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            if (csdNum == 1U)
                return MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedBelts, csdNum, endDelay: 300U, middleMotorRun: scripts.MiddleRollersRun,
                    middleMotorDir: scripts.MiddleRollersDir);
            return csdNum == 2U
                ? MakeLoadStatement(scripts.BeltsRun, TABLE_POSITION.UP, scripts.BeltsDir, MOTOR_DIR.CW, scripts.OccupiedBelts, csdNum, endDelay: 300U, middleMotorRun: scripts.MiddleRollersRun)
                : base.LoadAlternativeScript(csdNum);
        }
    }
}