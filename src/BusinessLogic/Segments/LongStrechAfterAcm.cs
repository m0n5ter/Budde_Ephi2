// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Segments.LongStrechAfterAcm
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;

namespace PharmaProject.BusinessLogic.Segments
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
            OutPin dispatchToLongStretch,
            string logName)
            : base(preEndSensor, endSensor, manualInsert, loadUpstream, logName, 1000U)
        {
            this.dispatchToLongStretch = dispatchToLongStretch;
            this.usToteAvl = usToteAvl;
            Init();
        }

        private void Init()
        {
            usToteAvl.OnStateChanged += UsToteAvlChanged;
            DispatchTote = dispatchToLongStretch.Utc.MakeConditionalStatement("Dispatch Tote to LongStretch", OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE).AddTimeoutCondition(500U)
                .AddOutputState(dispatchToLongStretch);
        }

        private void UsToteAvlChanged(InPin obj)
        {
            if (!usToteAvl.Active || !Empty)
                return;
            FlushTrack();
        }

        protected override void RaiseOnEmptyChanged()
        {
            if (Empty)
            {
                PerformedUpstreamDispatch();
                DispatchTote.Run();
            }

            base.RaiseOnEmptyChanged();
        }

        protected override bool FlushTrack()
        {
            if (!base.FlushTrack())
                return false;
            DispatchTote.Run();
            return true;
        }
    }
}