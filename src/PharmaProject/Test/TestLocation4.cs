// Decompiled with JetBrains decompiler
// Type: PharmaProject.Test.TestLocation4
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using Ephi.Core.UTC;
using PharmaProject.Locations;

namespace PharmaProject.Test
{
    internal class TestLocation4 : BaseLocation
    {
        public TestLocation4(string IP, uint locationNumber)
            : base(IP, locationNumber, 0U)
        {
            MakeOut(PIN._1).Activate();
            MakeOut(PIN._19).Activate();
            MakeOut(PIN._20).Activate();
            MakeOut(PIN._22).Activate();
            MakeOut(PIN._24).Activate();
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