// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.MainTrackCrossingLocation
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using System.Net;
using System.Text;
using Ephi.Core.Helping;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.Segments;
using PharmaProject.Wms_Communication;
using PharmaProject.Wms_Communication.Messages;

namespace PharmaProject.Locations
{
    public class MainTrackCrossingLocation : BaseLocation
    {
        private Conditional csd1AutoLoad;
        private readonly BarcodeScanner csd1BS1;
        private readonly BarcodeScanner csd1BS2;
        private Conditional csd2AutoLoad;
        private readonly BarcodeScanner csd2BS1;
        private readonly BarcodeScanner csd2BS2;
        private InPin inBtn;

        public MainTrackCrossingLocation(
            string IP,
            uint locationNumber,
            string csd1BS1Ip,
            string csd1BS2Ip,
            string csd2BS1Ip,
            string csd2BS2Ip)
            : base(IP, locationNumber, 2U)
        {
            csd1BS1 = new BarcodeScanner(string.Format("csd1BS1 Loc:{0}", locationNumber), IPAddress.Parse(csd1BS1Ip));
            csd1BS2 = new BarcodeScanner(string.Format("csd1BS2 Loc:{0}", locationNumber), IPAddress.Parse(csd1BS2Ip));
            csd2BS1 = new BarcodeScanner(string.Format("csd2BS1 Loc:{0}", locationNumber), IPAddress.Parse(csd2BS1Ip));
            csd2BS2 = new BarcodeScanner(string.Format("csd2BS2 Loc:{0}", locationNumber), IPAddress.Parse(csd2BS2Ip));
            csd1BS1.OnBarcodeScanned += Csd1_OnBarcodeScanned;
            csd1BS2.OnBarcodeScanned += Csd1_OnBarcodeScanned;
            csd2BS1.OnBarcodeScanned += Csd2_OnBarcodeScanned;
            csd2BS2.OnBarcodeScanned += Csd2_OnBarcodeScanned;
        }

        protected override InPin[] ResetEmergencyPins => new InPin[1] { inBtn };

        public void Set1Barcode(string barcode)
        {
            Csd1_OnBarcodeScanned(barcode);
        }

        public void Set2Barcode(string barcode)
        {
            Csd2_OnBarcodeScanned(barcode);
        }

        private void Csd1_OnBarcodeScanned(string barcode)
        {
            if (csd1.Route == null)
            {
                Log("Rx barcode, but ROUTE IS NULL");
            }
            else
            {
                csd1.Route.Barcode = barcode;
                RequestWmsDirection(barcode);
            }
        }

        private void Csd2_OnBarcodeScanned(string barcode)
        {
            if (csd2.Route == null)
            {
                Log("Rx barcode, but ROUTE IS NULL");
            }
            else
            {
                csd2.Route.Barcode = barcode;
                RequestWmsDirection(barcode);
            }
        }

        protected override void InitPins()
        {
            base.InitPins();
            inBtn = MakeIn(PIN._18);
            var scripts = GetScripts(1U);
            scripts.RollersDir = scripts.RollersDir.LowActive;
        }

        protected override void InitScripts()
        {
            base.InitScripts();
            GetScripts(1U);
            GetScripts(2U);
        }

        protected override Conditional LoadNormalScript(uint csdNum)
        {
            var scripts = GetScripts(csdNum);
            var conditional1 = base.LoadNormalScript(csdNum);
            Conditional conditional2 = MakeConditionalStatement(string.Format("Auto load precondition CSD:{0}, Loc:{1}", csdNum, LocationNumber), OUTPUT_ENFORCEMENT.ENF_UNTIL_CONDITION_TRUE)
                .MakePrecondition().AddLogicBlock(LOGIC_FUNCTION.AND).AddCondition(scripts.BeltsRun, PIN_STATE.INACTIVE).AddCondition(scripts.RollersRun, PIN_STATE.INACTIVE)
                .AddCondition(scripts.LiftRun, PIN_STATE.INACTIVE).AddCondition(scripts.OccupiedBelts, PIN_STATE.INACTIVE).AddCondition(scripts.OccupiedRollers, PIN_STATE.INACTIVE)
                .AddCondition(scripts.LoadTriggerNormal).CloseBlock();
            Conditional conditional3 = MakeConditionalMacro(string.Format("Auto load script CSD:{0}, Loc:{1}", csdNum, LocationNumber)).AddStatement(conditional2).AddStatement(conditional1);
            switch (csdNum)
            {
                case 1:
                    csd1AutoLoad = conditional3;
                    break;
                case 2:
                    csd2AutoLoad = conditional3;
                    break;
            }

            return conditional1;
        }

        protected override void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1)
        {
            CSD csd;
            Route route;
            if (csd1.Route != null && csd1.Route.Barcode.Equals(barcode))
            {
                csd = csd1;
                route = csd1.Route;
            }
            else
            {
                if (csd2.Route == null || !csd2.Route.Barcode.Equals(barcode))
                    return;
                csd = csd2;
                route = csd2.Route;
            }

            if (route.Destination != DESTINATION.TBD)
                return;
            if (target == WMS_TOTE_DIRECTION.DIRECTION_1)
                Dispatch(csd, DESTINATION.DISPATCH_CSD1);
            else
                Dispatch(csd, DESTINATION.DISPATCH_CSD2);
        }

        protected override void DispatchWmsFeedback(Route route)
        {
            if (route == null || route.Barcode == null || route.Barcode.Equals(string.Empty))
                return;
            var direction = route.Destination == DESTINATION.DISPATCH_CSD1 ? WMS_TOTE_DIRECTION.DIRECTION_2 : WMS_TOTE_DIRECTION.DIRECTION_1;
            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new RückmeldungPackstück(direction, Encoding.ASCII.GetBytes(route.Barcode), LocationNumber)));
            NdwConnectCommunicator.DirectionSentUpdate(LocationNumber, route.Barcode, direction);
        }

        public override void DoEvaluate()
        {
            if (Status != UTC_STATUS.OPERATIONAL)
                return;
            EvaluateCsds(csd1, csd2);
            EvaluateCsds(csd2, csd1);
            var route1 = csd1.Route;
            if ((route1 != null ? route1.IsCrossover ? 1 : 0 : 0) == 0)
                return;
            var route2 = csd2.Route;
            if ((route2 != null ? route2.IsCrossover ? 1 : 0 : 0) == 0)
                return;
            csd2.Route.Destination = DESTINATION.DISPATCH_CSD2;
            EvaluateCsds(csd2, csd1);
        }

        private void EvaluateCsds(CSD a, CSD b)
        {
            var route1 = b.Route;
            var flag = route1 != null && route1.IsCrossover;
            switch (a.State)
            {
                case CSD_STATE.IDLE:
                    a.Route = null;
                    if (flag)
                    {
                        if (a == csd1)
                        {
                            csd1AutoLoad.Cancel();
                            break;
                        }

                        csd2AutoLoad.Cancel();
                        break;
                    }

                    if (a == csd1)
                    {
                        csd1AutoLoad.Run();
                        break;
                    }

                    csd2AutoLoad.Run();
                    break;
                case CSD_STATE.LOADING:
                    if (a.Route != null)
                        break;
                    a.Route = new Route(a == csd1 ? START.LOAD_CSD1 : START.LOAD_CSD2);
                    break;
                case CSD_STATE.OCCUPIED:
                    var route2 = a.Route;
                    if ((route2 != null ? (int)route2.Destination : 6) == 6)
                    {
                        Dispatch(a, a == csd1 ? DESTINATION.DISPATCH_CSD1 : DESTINATION.DISPATCH_CSD2);
                        break;
                    }

                    if (a.Route.IsCrossover && !b.IsIdle)
                        break;
                    Dispatch(a, a.Route.Destination);
                    break;
            }
        }

        private void Dispatch(CSD csd, DESTINATION to)
        {
            var route = csd.Route;
            if (route != null)
                route.Destination = to;
            if (!Helpers.Contains(csd.State, CSD_STATE.LOADING_PENDING, CSD_STATE.LOADING, CSD_STATE.OCCUPIED))
                return;
            switch (to)
            {
                case DESTINATION.DISPATCH_CSD1:
                case DESTINATION.DISPATCH_CSD1_ALT:
                    if (csd == csd1)
                    {
                        if (csd.Scripts.DispatchNormalSegmentOccupied.Active)
                            return;
                        csd.PassThrough();
                        break;
                    }

                    if (!csd1.IsIdle || csd1.Scripts.DispatchNormalSegmentOccupied.Active)
                        return;
                    csd1AutoLoad.Cancel();
                    if (csd1.LoadAlternative())
                    {
                        csd2.DispatchAlternative();
                    }

                    break;
                case DESTINATION.DISPATCH_CSD2:
                    if (csd != csd1)
                    {
                        if (csd.Scripts.DispatchNormalSegmentOccupied.Active)
                            return;
                        csd.PassThrough();
                        break;
                    }

                    if (!csd2.IsIdle || csd2.Scripts.DispatchNormalSegmentOccupied.Active)
                        return;
                    csd2AutoLoad.Cancel();
                    if (csd2.LoadAlternative()) csd1.DispatchAlternative();
                    break;
                default:
                    throw new ArgumentException("Illegal dispatch destination");
            }

            if (route != null)
                DispatchWmsFeedback(route);
            csd.Route = null;
        }
    }
}