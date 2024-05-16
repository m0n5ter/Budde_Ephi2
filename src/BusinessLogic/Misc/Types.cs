namespace PharmaProject.BusinessLogic.Misc
{
    public class PrintSegJob
    {
        public string BarcodeSide = string.Empty;
        public string BarcodeTop = string.Empty;
        public string CheckCode = string.Empty;

        private PrintSegJob(PRINT_SEG_JOB_TYPE job, string barcode)
        {
            JobType = job;
            BarcodeSide = barcode;
        }

        public PRINT_SEG_JOB_TYPE JobType { get; set; }

        public override string ToString()
        {
            return $"PrinsegJob {(string.IsNullOrWhiteSpace(BarcodeSide) ? "<NoBarcode>" : (object)BarcodeSide)} -> {JobType}";
        }

        public static PrintSegJob Make(PRINT_SEG_JOB_TYPE job)
        {
            return new PrintSegJob(job, string.Empty);
        }

        public static PrintSegJob Make(PRINT_SEG_JOB_TYPE job, string barcode)
        {
            return new PrintSegJob(job, barcode);
        }
    }
}