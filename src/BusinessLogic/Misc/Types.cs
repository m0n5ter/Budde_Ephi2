// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Misc.PrintSegJob
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll


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
            return string.Format("PrinsegJob {0} -> {1}", string.IsNullOrWhiteSpace(BarcodeSide) ? "<NoBarcode>" : (object)BarcodeSide, JobType);
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