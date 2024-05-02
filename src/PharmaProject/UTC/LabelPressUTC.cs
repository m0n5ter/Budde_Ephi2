// Decompiled with JetBrains decompiler
// Type: PharmaProject.UTC.LabelPressUTC
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using Ephi.Core.UTC;

namespace PharmaProject.UTC
{
    internal class LabelPressUTC : CSD_UTC
    {
        public LabelPressUTC(string IP)
            : base(IP, "LabelPress", 0U)
        {
            SoftEmergency = false;
            labelPress1 = new LabelPrinter(MakeIn(PIN._1), MakeIn(PIN._2), MakeIn(PIN._3).LowActive, MakeIn(PIN._4), MakeIn(PIN._5), MakeOut(PIN._1), MakeOut(PIN._2));
            labelPress2 = new LabelPrinter(MakeIn(PIN._11), MakeIn(PIN._12), MakeIn(PIN._13).LowActive, MakeIn(PIN._14), MakeIn(PIN._15), MakeOut(PIN._11), MakeOut(PIN._12));
        }

        public LabelPrinter labelPress1 { private set; get; }

        public LabelPrinter labelPress2 { private set; get; }

        protected override void InvalidateCsdStates()
        {
        }
    }
}