using System.Net;
using PharmaProject.BusinessLogic.Devices;

namespace PharmaProject.BusinessLogic.Locations
{
    public class MainTrackCrossingLocation_PblB : MainTrackCrossingLocation_Base
    {
        private readonly BarcodeScanner csd2BS2;

        public MainTrackCrossingLocation_PblB(
            string IP,
            uint locationNumber,
            string csd1BS1Ip,
            string csd1BS2Ip,
            string csd2BS1Ip,
            string csd2BS2Ip)
            : base(IP, locationNumber, csd1BS1Ip, csd1BS2Ip, csd2BS1Ip)
        {
            csd2BS2 = new BarcodeScanner($"csd2BS2 Loc:{locationNumber}", IPAddress.Parse(csd2BS2Ip));
            csd2BS2.OnBarcodeScanned += Csd2_OnBarcodeScanned;
            csd2BS2.OnNoRead += OnNoRead;
        }

        protected override void InitPins()
        {
            base.InitPins();
            var scripts1 = GetScripts(1U);
            scripts1.BeltsDir = scripts1.BeltsDir.LowActive;
            var scripts2 = GetScripts(2U);
            scripts2.BeltsDir = scripts2.BeltsDir.LowActive;
        }
    }
}