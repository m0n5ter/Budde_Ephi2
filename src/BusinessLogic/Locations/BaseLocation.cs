// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Locations.BaseLocation
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using System.Collections.Generic;
using System.Text;
using Ephi.Core.Helping;
using Ephi.Core.Helping.General;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.BusinessLogic.Devices;
using PharmaProject.BusinessLogic.Misc;
using PharmaProject.BusinessLogic.Segments;
using PharmaProject.BusinessLogic.UTC;
using PharmaProject.BusinessLogic.Wms_Communication;
using PharmaProject.BusinessLogic.Wms_Communication.Messages;

namespace PharmaProject.BusinessLogic.Locations
{
    public abstract class BaseLocation : CSD_UTC
    {
        private static readonly Dictionary<uint, BaseLocation> locationList = new Dictionary<uint, BaseLocation>();
        protected CSD csd1;
        protected CSD csd2;
        protected CSD csd3;
        public bool DoLog;
        protected bool evalPending;
        protected bool evalRunning;
        protected object EvaluateLock = new object();
        private DateTime lastRequestSent;
        protected DelayedEvent ReEvaluate;
        protected object SendAllowedLock = new object();
        private uint waitForWmsFeedbackMs;
        private DelayedEvent WaitWmsEvaluate;

        public BaseLocation(string IP, uint locationNumber, uint numOfCSDs)
            : base(IP, locationNumber.ToString(), numOfCSDs)
        {
            LocationNumber = locationNumber;
            PreInit();
            if (HasCSD(1U))
                csd1 = MakeCSD(1U);
            if (HasCSD(2U))
                csd2 = MakeCSD(2U);
            if (HasCSD(3U))
                csd3 = MakeCSD(3U);
            if (locationList.ContainsKey(LocationNumber))
                throw new ArgumentOutOfRangeException(nameof(locationNumber), "Location already exists");
            if (locationNumber != 0U)
                locationList.Add(locationNumber, this);
            AttachEventHandlers();
            ReEvaluate = new DelayedEvent(500U, Evaluate);
        }

        public uint WaitForWmsFeedbackMs
        {
            get => waitForWmsFeedbackMs;
            set
            {
                if ((int)waitForWmsFeedbackMs == (int)value)
                    return;
                waitForWmsFeedbackMs = value;
                if (value == 0U)
                {
                    WaitWmsEvaluate?.Dispose();
                    WaitWmsEvaluate = null;
                }
                else
                {
                    WaitWmsEvaluate = new DelayedEvent(value + 100U, Evaluate);
                }
            }
        }

        public bool WaitWmsFeedbackTimedout
        {
            get
            {
                var waitWmsEvaluate = WaitWmsEvaluate;
                return (waitWmsEvaluate != null ? waitWmsEvaluate.Running ? 1 : 0 : 0) == 0;
            }
        }

        public uint LocationNumber { get; }

        protected bool SendAllowed
        {
            get
            {
                lock (SendAllowedLock)
                {
                    return DateTime.Now.Subtract(lastRequestSent) >= TimeSpan.FromMilliseconds(2700.0);
                }
            }
        }

        public static BaseLocation FindLocation(uint locationNumber)
        {
            BaseLocation baseLocation;
            return locationList.TryGetValue(locationNumber, out baseLocation) ? baseLocation : null;
        }

        public static void AssignWmsTimeouts()
        {
            foreach (var location in locationList)
                location.Value.WaitForWmsFeedbackMs = 3000U;
        }

        protected virtual void PreInit()
        {
        }

        protected virtual CSD MakeCSD(uint csdNum)
        {
            return new CSD(LocationNumber, csdNum, GetScripts(csdNum), this);
        }

        ~BaseLocation()
        {
            locationList.Remove(LocationNumber);
        }

        public void WaitWmsFeedbackStart()
        {
            WaitWmsEvaluate?.Start();
        }

        protected bool GetCsdDispatchDestination(CSD csd, ref DESTINATION def)
        {
            var route = csd.Route;
            var destination = route != null ? route.Destination : DESTINATION.TBD;
            if (destination == DESTINATION.TBD && csd.StateAge < TimeSpan.FromMilliseconds(WaitForWmsFeedbackMs))
            {
                ReEvaluate.Start();
                return false;
            }

            if (destination != DESTINATION.TBD)
                def = destination;
            return true;
        }

        public void Log(string txt)
        {
            log.Info(txt);
            if (!DoLog)
                return;
            Console.WriteLine("{0}\tLoc{1}, {2}", DateTime.Now.TimeOfDay, LocationNumber, txt);
        }

        protected virtual void AttachEventHandlers()
        {
            AttachCsdEventHandlers(csd1);
            AttachCsdEventHandlers(csd2);
            AttachCsdEventHandlers(csd3);
        }

        protected void AddBarcodeScanner(BarcodeScanner b)
        {
            b.OnBarcodeScanned += OnBarcodeScanned;
            b.OnNoRead += OnNoRead;
        }

        protected virtual void AttachCsdEventHandlers(CSD csd)
        {
            if (csd == null)
                return;
            var loadTriggerNormal = csd.Scripts.LoadTriggerNormal;
            if ((loadTriggerNormal != null ? loadTriggerNormal.IsDummy ? 1 : 0 : 1) == 0)
                csd.Scripts.LoadTriggerNormal.OnStateChanged += TriggerEvaluate;
            var triggerAlternative = csd.Scripts.LoadTriggerAlternative;
            if ((triggerAlternative != null ? triggerAlternative.IsDummy ? 1 : 0 : 1) == 0)
                csd.Scripts.LoadTriggerAlternative.OnStateChanged += TriggerEvaluate;
            var occupiedBelts = csd.Scripts.OccupiedBelts;
            if ((occupiedBelts != null ? occupiedBelts.IsDummy ? 1 : 0 : 1) == 0)
                csd.Scripts.OccupiedBelts.OnStateChanged += TriggerEvaluate;
            var occupiedRollers = csd.Scripts.OccupiedRollers;
            if ((occupiedRollers != null ? occupiedRollers.IsDummy ? 1 : 0 : 1) == 0)
                csd.Scripts.OccupiedRollers.OnStateChanged += TriggerEvaluate;
            var normalSegmentOccupied = csd.Scripts.DispatchNormalSegmentOccupied;
            if ((normalSegmentOccupied != null ? normalSegmentOccupied.IsDummy ? 1 : 0 : 1) == 0)
                csd.Scripts.DispatchNormalSegmentOccupied.OnStateChanged += TriggerEvaluate;
            var alternativeSegmentOccupied = csd.Scripts.DispatchAlternativeSegmentOccupied;
            if ((alternativeSegmentOccupied != null ? alternativeSegmentOccupied.IsDummy ? 1 : 0 : 1) == 0)
                csd.Scripts.DispatchAlternativeSegmentOccupied.OnStateChanged += TriggerEvaluate;
            csd.OnStateChanged += Csd_OnStateChanged;
        }

        protected void Csd_OnStateChanged(CSD csd)
        {
            if (csd.IsOccupied)
                WaitWmsEvaluate?.Start();
            switch (csd.State)
            {
                case SEGMENT_STATE.IDLE:
                case SEGMENT_STATE.LOADING:
                case SEGMENT_STATE.OCCUPIED:
                    Evaluate();
                    break;
            }
        }

        protected void TriggerEvaluate(InPin obj)
        {
            Evaluate();
        }

        protected void Script_OnStateChanged(Conditional obj)
        {
            Evaluate();
        }

        protected override void InvalidateCsdStates()
        {
            csd1?.InvalidateState();
            csd2?.InvalidateState();
            csd3?.InvalidateState();
        }

        protected override void SoftEmrgChanged()
        {
            base.SoftEmrgChanged();
            if (SoftEmergency)
                return;
            Evaluate();
        }

        protected CSD GetCsd(uint csdNum)
        {
            switch (csdNum)
            {
                case 1:
                    return csd1;
                case 2:
                    return csd2;
                case 3:
                    return csd3;
                default:
                    return null;
            }
        }

        protected void Evaluate()
        {
            if (Status != UTC_STATUS.OPERATIONAL || EmergencyStop)
                return;
            lock (EvaluateLock)
            {
                evalPending = true;
                if (evalRunning)
                    return;
                evalRunning = true;
            }

            while (evalRunning)
            {
                lock (EvaluateLock)
                {
                    evalPending = false;
                }

                try
                {
                    csd1?.CheckValid();
                    csd2?.CheckValid();
                    csd3?.CheckValid();
                    csd1?.InvalidateRoute();
                    csd2?.InvalidateRoute();
                    csd3?.InvalidateRoute();
                    DoEvaluate();
                }
                catch (Exception ex)
                {
                    evalRunning = false;
                    throw ex;
                }

                lock (EvaluateLock)
                {
                    if (!evalPending)
                    {
                        evalRunning = false;
                        break;
                    }
                }
            }
        }

        public abstract void DoEvaluate();

        protected virtual void OnBarcodeScanned(string barcode)
        {
            RequestWmsDirection(barcode);
            NdwConnectCommunicator.BarcodeScannedUpdate(LocationNumber, barcode);
        }

        protected virtual void OnNoRead()
        {
            if (!SendAllowed)
                return;
            NotifyWmsNoread();
            NdwConnectCommunicator.BarcodeScannedUpdate(LocationNumber, "NoRead");
        }

        protected void BarcodeSent()
        {
            lock (SendAllowedLock)
            {
                lastRequestSent = DateTime.Now;
            }
        }

        protected void NoReadSent()
        {
            lock (SendAllowedLock)
            {
                lastRequestSent = DateTime.Now.Subtract(TimeSpan.FromMilliseconds(2000.0));
            }
        }

        private void NotifyWmsNoread()
        {
            NoReadSent();
            Log(string.Format("Notify WMS NoRead: (loc:{0})", LocationNumber));
            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AnmeldungPackstück(0U, 0U, Encoding.ASCII.GetBytes("NoRead"), 0U, 0U, LocationNumber)));
        }

        protected virtual void RequestWmsDirection(string barcode)
        {
            BarcodeSent();
            Log(string.Format("Requesting direction for barcode:{0} (loc:{1})", barcode, LocationNumber));
            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AnmeldungPackstück(0U, 0U, Encoding.ASCII.GetBytes(barcode), 0U, 0U, LocationNumber)));
        }

        public void WmsSetDirection(string barcode, WMS_TOTE_DIRECTION target, uint value1)
        {
            lock (EvaluateLock)
            {
                DoWmsSetDirection(barcode, target, value1);
            }
        }

        public virtual bool ProcessRxMessage(BaseMessage message)
        {
            if (message.functionCode != 2U)
                return false;
            var anweisungPackstück = message as AnweisungPackstück;
            var barcode = Encoding.ASCII.GetString(anweisungPackstück.Barcode).TrimEnd(new char[1]);
            Log(string.Format("WMS Rx: AnweisungPackstück({0}), Direction {1}", barcode, Formatting.TitleCase(anweisungPackstück.Direction)));
            WmsSetDirection(barcode, anweisungPackstück.Direction, anweisungPackstück.Value1);
            NdwConnectCommunicator.DirectionRequestedUpdate(LocationNumber, barcode, anweisungPackstück.Direction);
            return true;
        }

        protected virtual void DispatchWmsFeedback(Route route)
        {
        }

        protected abstract void DoWmsSetDirection(
            string barcode,
            WMS_TOTE_DIRECTION target,
            uint value1);
    }
}