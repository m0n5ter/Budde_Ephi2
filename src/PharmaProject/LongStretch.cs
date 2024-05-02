// Decompiled with JetBrains decompiler
// Type: PharmaProject.LongStretch
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;

namespace PharmaProject
{
    public class LongStretch
    {
        private const uint END_SNS_DEBOUNCE_ms = 2000;
        private const uint SINGLE_SHOT_WINDOW_ms = 100;
        private readonly uint ACM_DISP_START_DELAY_ms = 700;
        private bool empty;
        protected InPin endSensor;
        protected DelayedEvent flushTrack;
        private Conditional LoadTote;
        protected InPin manualInsert;
        protected InPin preEndSensor;
        private bool singleShot;
        protected DelayedEvent stopSingleShot;
        private Conditional ToteInserted;
        protected OutPin upstreamLoad;

        public LongStretch(
            InPin preEndSensor,
            InPin endSensor,
            InPin manualInsert,
            OutPin upstreamLoad,
            uint acmDispStartDelay_ms = 0)
        {
            this.preEndSensor = preEndSensor;
            this.endSensor = endSensor;
            this.manualInsert = manualInsert;
            this.upstreamLoad = upstreamLoad;
            ACM_DISP_START_DELAY_ms = Math.Max(500U, acmDispStartDelay_ms);
            InitScripts();
            AttachEventHandlers();
        }

        public bool Empty
        {
            get => empty;
            set
            {
                if (empty == value)
                    return;
                empty = value;
                RaiseOnEmptyChanged();
            }
        }

        public bool SingleShot
        {
            get => singleShot;
            set
            {
                if (value == singleShot)
                    return;
                singleShot = value;
                if (value)
                    stopSingleShot.Start();
                EvalEmpty();
            }
        }

        private void InitScripts()
        {
            ToteInserted = manualInsert.Utc.MakeConditionalStatement("Loong Stretch Tote insert detection", OUTPUT_ENFORCEMENT.ENF_NEGATE_WHEN_TRUE, RUN_MODE.PERMANENTLY)
                .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(manualInsert).AddGuardBlock(50U).AddGuardPin(manualInsert).CloseBlock().CloseBlock();
            LoadTote = upstreamLoad.Utc.MakeConditionalStatement("LongStretch Load Tote", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddTimeoutCondition(500U).AddOutputState(upstreamLoad);
        }

        private void ToteInserted_OnStateChanged(Conditional c)
        {
            if (c.Status != CONDITIONAL_STATE.FINISHED)
                return;
            flushTrack.Start();
        }

        protected virtual void AttachEventHandlers()
        {
            var debounceTimeout = TimeSpan.FromMilliseconds(2000.0);
            preEndSensor.RegisterDebouncedPinStateHandler(debounceTimeout, OnDeboucedTriggered);
            endSensor.RegisterDebouncedPinStateHandler(debounceTimeout, OnDeboucedTriggered);
            preEndSensor.OnStateChanged += Sensor_OnStateChanged;
            endSensor.OnStateChanged += Sensor_OnStateChanged;
            ToteInserted.OnStateChanged += ToteInserted_OnStateChanged;
            flushTrack = new DelayedEvent(TimeSpan.FromMilliseconds(1500.0), () => TryLoadPushOns());
            stopSingleShot = new DelayedEvent(TimeSpan.FromMilliseconds(100.0), () => SingleShot = false);
            if (ACM_DISP_START_DELAY_ms == 0U)
                endSensor.OnStateChanged += EndSensor_Changed;
            else
                endSensor.RegisterDebouncedPinStateHandler(TimeSpan.FromMilliseconds(ACM_DISP_START_DELAY_ms), EndSensor_Changed);
        }

        private void EndSensor_Changed(InPin pin)
        {
            if (!pin.Inactive || !preEndSensor.Active)
                return;
            SingleShot = true;
        }

        protected void EvalEmpty()
        {
            if (SingleShot)
            {
                Empty = true;
            }
            else if (!preEndSensor.Inactive || !endSensor.Inactive)
            {
                Empty = false;
            }
            else
            {
                var timeSpan = TimeSpan.FromMilliseconds(2000.0);
                Empty = preEndSensor.PinStateAge >= timeSpan && endSensor.PinStateAge >= timeSpan;
            }
        }

        protected virtual bool TryLoadPushOns()
        {
            if (!preEndSensor.Inactive)
                return false;
            LoadTote.Run();
            return true;
        }

        private void Sensor_OnStateChanged(InPin pin)
        {
            TryLoadPushOns();
        }

        private void OnDeboucedTriggered(InPin pin)
        {
            EvalEmpty();
        }

        public event Action<bool> OnEmptyChanged;

        protected virtual void RaiseOnEmptyChanged()
        {
            var onEmptyChanged = OnEmptyChanged;
            if (onEmptyChanged == null)
                return;
            onEmptyChanged(empty);
        }
    }
}