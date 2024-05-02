// Decompiled with JetBrains decompiler
// Type: PharmaProject.LongStrechAfterAcm
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;

namespace PharmaProject
{
    public class LongStrechAfterAcm : LongStretch
    {
        private const uint ACM_DISP_START_DELAY_ms = 1000;
        private readonly OutPin dispatchToLongStretch;
        private Conditional DispatchTote;
        private readonly InPin usToteAvl;

        public LongStrechAfterAcm(
            InPin preEndSensor,
            InPin endSensor,
            InPin usToteAvl,
            InPin manualInsert,
            OutPin loadUpstream,
            OutPin dispatchToLongStretch)
            : base(preEndSensor, endSensor, manualInsert, loadUpstream, 1000U)
        {
            this.dispatchToLongStretch = dispatchToLongStretch;
            this.usToteAvl = usToteAvl;
            Init();
        }

        private void Init()
        {
            usToteAvl.OnStateChanged += UsTreamAvlChanged;
            DispatchTote = dispatchToLongStretch.Utc.MakeConditionalStatement("Dispatch Tote to LongStretch", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddTimeoutCondition(500U)
                .AddOutputState(dispatchToLongStretch);
        }

        private void UsTreamAvlChanged(InPin obj)
        {
            if (!usToteAvl.Active || !Empty)
                return;
            TryLoadPushOns();
        }

        protected override void RaiseOnEmptyChanged()
        {
            if (Empty)
                DispatchTote.Run();
            base.RaiseOnEmptyChanged();
        }

        protected override bool TryLoadPushOns()
        {
            if (!base.TryLoadPushOns())
                return false;
            DispatchTote.Run();
            return true;
        }
    }
}