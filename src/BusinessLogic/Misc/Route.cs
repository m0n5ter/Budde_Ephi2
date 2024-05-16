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
            return $"{Start} => {Destination} ({Barcode})";
        }
    }
}