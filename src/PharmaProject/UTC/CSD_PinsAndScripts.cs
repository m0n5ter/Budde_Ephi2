// Decompiled with JetBrains decompiler
// Type: PharmaProject.UTC.CSD_PinsAndScripts
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;

namespace PharmaProject.UTC
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