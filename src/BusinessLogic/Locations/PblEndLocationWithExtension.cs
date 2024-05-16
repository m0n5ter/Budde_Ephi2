using System.Net;
using System.Text;
using Ephi.Core.Helping;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Devices;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.UTC;
using PharmaProject.BusinessLogic.Wms_Communication;
using PharmaProject.BusinessLogic.Wms_Communication.Messages;

namespace PharmaProject.BusinessLogic.Locations
{
    public class PblEndLocationWithExtension : PblEndLocation
    {
        public PblEndLocationWithExtension(string IP, string barcodeScannerIp, uint locationNumber)
            : base(IP, locationNumber)
        {
            AddBarcodeScanner(new BarcodeScanner($"Loc:{locationNumber} Barcodescanner", IPAddress.Parse(barcodeScannerIp)));
        }

        protected override void InitPins()
        {
            base.InitPins();
            var scripts = GetScripts(1U);
            scripts.DispatchNormalSegmentOccupied = MakeIn(PIN._13);
            scripts.DownstreamStartLoading = MakeOut(PIN._13);
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);

            return csdNum == 1U
                ? MakeLoadStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.OccupiedRollers, csdNum, prevSegDispatch: scripts.UpstreamStartDispatching)
                : null;
        }

        protected override Conditional DispatchNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            
            if (csdNum != 1U)
                return base.DispatchNormalScript(csdNum);
            
            return scripts != null
                ? MakeDispatchStatement(scripts.RollersRun, TABLE_POSITION.DOWN, scripts.RollersDir, MOTOR_DIR.CW, scripts.DispatchNormalSegmentOccupied, scripts.OccupiedRollers, csdNum,
                    nextSegLoad: scripts.DownstreamStartLoading)
                : null;
        }

        protected override void InitAutoCrossoverScript(CSD_PinsAndScripts s1, CSD_PinsAndScripts s2)
        {
        }

        public override void DoEvaluate()
        {
            if (!Helpers.Contains(csd1.State, SEGMENT_STATE.OCCUPIED))
                return;
            
            if (csd1.Route == null)
                csd1.Route = new Route(START.LOAD_CSD1);
            
            var def = DESTINATION.DISPATCH_CSD1_ALT;
            
            if (!GetCsdDispatchDestination(csd1, ref def))
                return;
            
            DispatchCSD1(def);
        }

        public void DispatchCSD1(DESTINATION to)
        {
            if (!Helpers.Contains(csd1.State, SEGMENT_STATE.OCCUPIED))
                return;
            
            var scripts = GetScripts(1U);
            
            if (to == DESTINATION.DISPATCH_CSD1)
            {
                if (scripts.DispatchNormalSegmentOccupied.Inactive)
                    csd1.DispatchNormal();
            }
            else
            {
                if (csd2.State != SEGMENT_STATE.IDLE || loadDispatch.IsRunningOrAboutToBe)
                    return;
            
                if (loadDispatch.Run())
                {
                    csd1.ForceDispatchPending();
                    csd2.ForceLoadPending();
                }
            }

            if (csd1.Route != null)
                DispatchWmsFeedback(csd1.Route);
            
            csd1.Route = null;
        }

        protected override void OnBarcodeScanned(string barcode)
        {
            base.OnBarcodeScanned(barcode);
            
            if (csd1.Route == null)
                csd1.Route = new Route(START.LOAD_CSD1);
            
            csd1.Route.Barcode = barcode;
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            if (csd1.Route == null || !csd1.Route.Barcode.Equals(barcode))
                return;
            
            switch (target)
            {
                case WMS_TOTE_DIRECTION.DIRECTION_1:
                    csd1.Route.Destination = DESTINATION.DISPATCH_CSD1_ALT;
                    break;
            
                case WMS_TOTE_DIRECTION.DIRECTION_2:
                    csd1.Route.Destination = DESTINATION.DISPATCH_CSD1;
                    break;
            }

            Evaluate();
        }

        protected override void DispatchWmsFeedback(Route route)
        {
            var direction = WMS_TOTE_DIRECTION.DIRECTION_1;
            
            if (route.Destination == DESTINATION.DISPATCH_CSD1)
                direction = WMS_TOTE_DIRECTION.DIRECTION_2;
            
            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(direction, Encoding.ASCII.GetBytes(route.Barcode), LocationNumber)));
        }
    }
}