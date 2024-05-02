// Decompiled with JetBrains decompiler
// Type: PharmaProject.Route
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using Ephi.Core.Helping;

namespace PharmaProject
{
    public class Route
    {
        public Route(START start, DESTINATION destination = DESTINATION.TBD)
        {
            Start = start;
            Destination = destination;
        }

        public START Start { get; }

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
    }
}