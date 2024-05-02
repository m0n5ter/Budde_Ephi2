// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Devices.LabelPrinter
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using Ephi.Core.Helping.Log4Net;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using log4net;
using PharmaProject.BusinessLogic.Misc;

namespace PharmaProject.BusinessLogic.Devices
{
    public class LabelPrinter
    {
        private readonly uint PrnNum;
        private Conditional apply;
        private APPLYING_STATE applyingState;
        private bool deviceReady = true;
        private readonly InPin inApplying;
        private readonly InPin inDeviceReady;
        private readonly InPin inPrintready;
        private InPin inWarning;
        private ILog Log;
        private readonly OutPin outApply;

        public LabelPrinter(
            uint prnNum,
            InPin applying,
            InPin printReady,
            InPin deviceReady,
            InPin warning,
            OutPin apply)
        {
            PrnNum = prnNum;
            Log = Log4NetHelpers.AddIsolatedLogger(string.Format("Printer.{0}", prnNum), "..\\Log\\Printers\\");
            inApplying = applying;
            inPrintready = printReady;
            inDeviceReady = deviceReady;
            inWarning = warning;
            outApply = apply;
            InitScrips();
            inDeviceReady.OnStateChanged += DeviceReady_Check;
            inPrintready.OnStateChanged += InPrintready_OnStateChanged;
        }

        public APPLYING_STATE ApplyingState
        {
            get => applyingState;
            private set
            {
                if (applyingState == value)
                    return;
                switch (value)
                {
                    case APPLYING_STATE.WAITING_FOR_PRINT:
                        if (inPrintready.Active)
                        {
                            ApplyingState = APPLYING_STATE.LABEL_READY;
                            return;
                        }

                        break;
                    case APPLYING_STATE.APPLYING:
                        apply.Run();
                        break;
                }

                applyingState = value;
                var onApplyingChanged = OnApplyingChanged;
                if (onApplyingChanged == null)
                    return;
                onApplyingChanged(this);
            }
        }

        public bool DeviceReady
        {
            get => deviceReady;
            set
            {
                if (deviceReady == value)
                    return;
                deviceReady = value;
                var deviceReadyChanged = OnDeviceReadyChanged;
                if (deviceReadyChanged == null)
                    return;
                deviceReadyChanged(this);
            }
        }

        public bool LabelReady => inPrintready.Active;

        private void InPrintready_OnStateChanged(InPin pin)
        {
            if (!pin.Active || ApplyingState != APPLYING_STATE.WAITING_FOR_PRINT)
                return;
            ApplyingState = APPLYING_STATE.LABEL_READY;
        }

        private void DeviceReady_Check(InPin pin)
        {
            DeviceReady = inDeviceReady.Active;
        }

        private void InitScrips()
        {
            apply = inApplying.Utc.MakeConditionalStatement(string.Format("Apply command (prn:{0})", PrnNum), OUTPUT_ENFORCEMENT.ENF_NEGATE_WHEN_TRUE).AddGlobalTimeout(3000U).AddCondition(inApplying)
                .AddOutputState(outApply);
            apply.OnStateChanged += Applying_OnStateChanged;
        }

        public event Action<LabelPrinter> OnApplyingChanged;

        private void Applying_OnStateChanged(Conditional applyScript)
        {
            switch (applyScript.Status)
            {
                case CONDITIONAL_STATE.RUNNING:
                case CONDITIONAL_STATE.RUN_REQUESTED:
                    ApplyingState = APPLYING_STATE.APPLYING;
                    break;
                case CONDITIONAL_STATE.FINISHED:
                    ApplyingState = APPLYING_STATE.APPLYING_READY;
                    break;
                case CONDITIONAL_STATE.TIMED_OUT:
                    ApplyingState = APPLYING_STATE.APPLYING_FAILED;
                    break;
            }
        }

        public event Action<LabelPrinter> OnDeviceReadyChanged;

        public void ApplyLabel()
        {
            ApplyingState = APPLYING_STATE.APPLYING;
        }

        public void Reset()
        {
            ApplyingState = APPLYING_STATE.WAITING_FOR_PRINT;
        }
    }
}