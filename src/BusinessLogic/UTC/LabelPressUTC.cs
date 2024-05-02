// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.UTC.LabelPressUTC
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using Ephi.Core.UTC;
using PharmaProject.BusinessLogic.Devices;

namespace PharmaProject.BusinessLogic.UTC
{
    internal class LabelPressUTC : CSD_UTC
    {
        public LabelPressUTC(string IP)
            : base(IP, "LabelPress", 0U)
        {
            SoftEmergency = false;
            labelPress1 = new LabelPrinter(1U, MakeIn(PIN._1), MakeIn(PIN._6), MakeIn(PIN._3).LowActive, MakeIn(PIN._4), MakeOut(PIN._1));
            labelPress2 = new LabelPrinter(2U, MakeIn(PIN._11), MakeIn(PIN._16), MakeIn(PIN._13).LowActive, MakeIn(PIN._14), MakeOut(PIN._11));
        }

        public LabelPrinter labelPress1 { private set; get; }

        public LabelPrinter labelPress2 { private set; get; }

        protected override void InvalidateCsdStates()
        {
        }
    }
}