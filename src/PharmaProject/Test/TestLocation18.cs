// Decompiled with JetBrains decompiler
// Type: PharmaProject.Test.TestLocation18
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using Ephi.Core.UTC;
using PharmaProject.Locations;

namespace PharmaProject.Test
{
    internal class TestLocation18 : BaseLocation
    {
        public TestLocation18(string IP, uint locationNumber, uint csdNum)
            : base(IP, locationNumber, csdNum)
        {
            csd1.Scripts.RollersRun.Activate();
            csd2.Scripts.RollersRun.Activate();
            MakeOut(PIN._23).Activate();
        }

        public override void DoEvaluate()
        {
            throw new NotImplementedException();
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            throw new NotImplementedException();
        }
    }
}