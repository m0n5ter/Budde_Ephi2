// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Misc.Route
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using Ephi.Core.Helping;

namespace PharmaProject.BusinessLogic.Misc
{
    public class Route
    {
        public Route(START start, DESTINATION destination = DESTINATION.TBD, string barcode = "")
        {
            Start = start;
            Destination = destination;
        }

        public START Start { get; set; }

        public DESTINATION Destination { get; set; }

        public bool IsCrossover
        {
            get
            {
                switch (Start)
                {
                    case START.LOAD_CSD1:
                        return Helpers.Contains(Destination, DESTINATION.DISPATCH_CSD2, DESTINATION.DISPATCH_CSD2_ALT);
                    case START.LOAD_CSD2:
                        return Helpers.Contains(Destination, DESTINATION.DISPATCH_CSD1, DESTINATION.DISPATCH_CSD1_ALT);
                    default:
                        return false;
                }
            }
        }

        public string Barcode { get; set; } = string.Empty;

        public override string ToString()
        {
            return string.Format("{0} => {1} ({2})", Start, Destination, Barcode);
        }
    }
}