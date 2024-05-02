// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Segments.LongStretch
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using Ephi.Core.Helping.General;
using Ephi.Core.Helping.Log4Net;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using log4net;

namespace PharmaProject.BusinessLogic.Segments
{
    public class LongStretch
    {
        private const uint END_SNS_DEBOUNCE_ms = 2000;
        private const uint SINGLE_SHOT_WINDOW_ms = 100;
        private readonly uint ACM_DISP_START_DELAY_ms = 700;
        private readonly byte BP120_LOAD_TIMEOUT_Sec = 30;
        private bool empty;
        protected InPin endSensor;
        private readonly object EvalEmptyLock = new object();
        protected bool evalPending;
        protected bool evalRunning;
        private Conditional Flush;
        private volatile bool flushPending;
        protected OutPin loadFromUS;
        private readonly ILog log;
        private readonly string logName;
        protected InPin manualInsert;
        protected InPin preEndSensor;
        private bool singleShot;
        protected DelayedEvent stopSingleShot;
        private Conditional ToteInserted;
        protected DelayedEvent watchdogEmpty;

        public LongStretch(
            InPin preEndSensor,
            InPin endSensor,
            InPin manualInsert,
            OutPin upstreamLoad,
            string logName,
            uint acmDispStartDelay_ms = 0)
        {
            this.preEndSensor = preEndSensor;
            this.endSensor = endSensor;
            this.manualInsert = manualInsert;
            loadFromUS = upstreamLoad;
            ACM_DISP_START_DELAY_ms = Math.Max(500U, acmDispStartDelay_ms);
            this.logName = logName;
            log = Log4NetHelpers.AddIsolatedLogger(logName, "..\\Log\\");
            InitScripts();
            AttachEventHandlers();
        }

        public bool Empty
        {
            get
            {
                EvalEmpty();
                return empty;
            }
            private set
            {
                if (!value)
                    watchdogEmpty.Start();
                if (empty == value)
                    return;
                empty = value;
                RaiseOnEmptyChanged();
            }
        }

        public bool SingleShot
        {
            get => singleShot;
            private set
            {
                if (value == singleShot)
                    return;
                singleShot = value;
                log.Info(value ? "Single-shot enabled" : (object)"Single-shot disabled");
                if (value)
                    stopSingleShot.Start();
                EvalEmpty();
            }
        }

        private void InitScripts()
        {
            ToteInserted = manualInsert.Utc.MakeConditionalStatement(string.Format("Long Stretch {0} Tote insert detection", logName), OUTPUT_ENFORCEMENT.ENF_NEGATE_WHEN_TRUE, RUN_MODE.PERMANENTLY)
                .AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(manualInsert).AddGuardBlock(50U).AddGuardPin(manualInsert).CloseBlock().CloseBlock();
            Flush = loadFromUS.Utc.MakeConditionalStatement(string.Format("Long Stretch {0} flush", logName), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                .AddTimeoutCondition(TIMEOUT_RANGE.TR_SEC, BP120_LOAD_TIMEOUT_Sec).AddOutputState(loadFromUS);
            Flush.OnStateChanged += Flush_OnStateChanged;
        }

        private void ToteInserted_OnStateChanged(Conditional c)
        {
            if (c.Status != CONDITIONAL_STATE.FINISHED)
                return;
            FlushTrack();
        }

        protected virtual void AttachEventHandlers()
        {
            var debounceTimeout = TimeSpan.FromMilliseconds(2500.0);
            preEndSensor.RegisterDebouncedPinStateHandler(debounceTimeout, OnDeboucedTriggered);
            endSensor.RegisterDebouncedPinStateHandler(debounceTimeout, OnDeboucedTriggered);
            preEndSensor.OnStateChanged += preEndSensor_OnStateChanged;
            ToteInserted.OnStateChanged += ToteInserted_OnStateChanged;
            stopSingleShot = new DelayedEvent(100U, () => SingleShot = false);
            watchdogEmpty = new DelayedEvent(500U, EvalEmpty);
            if (ACM_DISP_START_DELAY_ms == 0U)
                endSensor.OnStateChanged += EndSensor_Changed;
            else
                endSensor.RegisterDebouncedPinStateHandler(TimeSpan.FromMilliseconds(ACM_DISP_START_DELAY_ms), EndSensor_Changed);
        }

        public void PerformedUpstreamDispatch()
        {
            FlushTrack();
        }

        private void EvalEmpty()
        {
            lock (EvalEmptyLock)
            {
                evalPending = true;
                if (evalRunning)
                    return;
                evalRunning = true;
            }

            while (evalRunning)
            {
                lock (EvalEmptyLock)
                {
                    evalPending = false;
                }

                DoEvalEmpty();
                lock (EvalEmptyLock)
                {
                    if (!evalPending)
                    {
                        evalRunning = false;
                        break;
                    }
                }
            }
        }

        private void DoEvalEmpty()
        {
            if (SingleShot)
            {
                Empty = true;
            }
            else if (!preEndSensor.Inactive || !endSensor.Inactive)
            {
                log.Info("Not empty because either or both sensors is active");
                Empty = false;
            }
            else
            {
                var timeSpan = TimeSpan.FromMilliseconds(2000.0);
                Empty = preEndSensor.PinStateAge >= timeSpan && endSensor.PinStateAge >= timeSpan;
                if (empty)
                    return;
                log.Info("Not empty because both sensors have not been inactive long enough");
            }
        }

        protected virtual bool FlushTrack()
        {
            if (!preEndSensor.Inactive)
            {
                log.Info("pre-end sensor not inactive. Cancelling flush");
                Flush.Cancel();
                return false;
            }

            flushPending = Flush.IsRunning;
            if (flushPending)
            {
                log.Info("Flush still running. Not restarting");
                return true;
            }

            log.Info("Starting flush");
            Flush.Run();
            return true;
        }

        private void EndSensor_Changed(InPin pin)
        {
            if (!pin.Inactive)
                return;
            SingleShot = true;
        }

        private void preEndSensor_OnStateChanged(InPin pin)
        {
            FlushTrack();
        }

        private void OnDeboucedTriggered(InPin pin)
        {
            EvalEmpty();
        }

        private void Flush_OnStateChanged(Conditional obj)
        {
            if (Flush.Status != CONDITIONAL_STATE.FINISHED || !flushPending)
                return;
            FlushTrack();
        }

        public event Action<bool> OnEmptyChanged;

        protected virtual void RaiseOnEmptyChanged()
        {
            log.Info(empty ? "Stretch turned empty" : (object)"Stretch got full");
            var onEmptyChanged = OnEmptyChanged;
            if (onEmptyChanged == null)
                return;
            onEmptyChanged(empty);
        }
    }
}