// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.UTC.CSD_PinsAndScripts
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;

namespace PharmaProject.BusinessLogic.UTC
{
    public class CSD_PinsAndScripts
    {
        public OutPin BeltsDir;
        public OutPin BeltsRun;
        public InPin DispatchAlternativeSegmentOccupied;
        public InPin DispatchNormalSegmentOccupied;
        public OutPin DownstreamStartLoading;
        public OutPin LiftDir;
        public InPin LiftDown;
        public OutPin LiftRun;
        public InPin LiftUp;
        public InPin LoadTriggerAlternative;
        public InPin LoadTriggerNormal;
        public OutPin MiddleRollersDir;
        public OutPin MiddleRollersRun;
        public InPin OccupiedBelts;
        public InPin OccupiedExtra;
        public InPin OccupiedRollers;
        public OutPin RollersDir;
        public OutPin RollersRun;
        public OutPin UpstreamStartDispatching;

        public CSD_PinsAndScripts(uint csdNum)
        {
            CsdNum = csdNum;
        }

        public uint CsdNum { get; private set; }

        public Conditional TableUp { get; set; }

        public Conditional TableDown { get; set; }

        public Conditional LoadNormal { get; set; }

        public Conditional LoadAlternative { get; set; }

        public Conditional DispatchNormal { get; set; }

        public Conditional DispatchAlternative { get; set; }

        public Conditional PassThrough { get; set; }
    }
}