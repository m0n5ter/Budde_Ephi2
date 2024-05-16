// Decompiled with JetBrains decompiler
// Type: PharmaProject.Locations.BaseLocation
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;
using System.Collections.Generic;
using System.Text;
using Ephi.Core.UTC;
using Ephi.Core.UTC.ConditionalStatements;
using PharmaProject.Segments;
using PharmaProject.UTC;
using PharmaProject.Wms_Communication;
using PharmaProject.Wms_Communication.Messages;

namespace PharmaProject.Locations
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
        protected object SendAllowedLock = new object();

        public BaseLocation(string IP, uint locationNumber, uint numOfCSDs)
            : base(IP, locationNumber.ToString(), numOfCSDs)
        {
            LocationNumber = locationNumber;
            PreInit();
            if (HasCSD(1U))
                csd1 = new CSD(LocationNumber, 1U, GetScripts(1U), this);
            if (HasCSD(2U))
                csd2 = new CSD(LocationNumber, 2U, GetScripts(2U), this);
            if (HasCSD(3U))
                csd3 = new CSD(LocationNumber, 3U, GetScripts(3U), this);
            if (locationList.ContainsKey(LocationNumber))
                throw new ArgumentOutOfRangeException(nameof(locationNumber), "Location already exists");
            if (locationNumber != 0U)
                locationList.Add(locationNumber, this);
            OnConditionalStatusChanged += CSD_UTC_OnConditionalStatusChanged;
            AttachEventHandlers();
        }

        public uint LocationNumber { get; }

        protected bool SendAllowed => DateTime.Now.Subtract(lastRequestSent) >= TimeSpan.FromMilliseconds(3500.0);

        public static BaseLocation FindLocation(uint locationNumber)
        {
            return locationList.TryGetValue(locationNumber, out var baseLocation) ? baseLocation : null;
        }

        protected virtual void PreInit()
        {
        }

        ~BaseLocation()
        {
            locationList.Remove(LocationNumber);
        }

        public void Log(string txt)
        {
            if (!DoLog)
                return;
            Console.WriteLine("{0}, Loc{1}, {2}", DateTime.Now.TimeOfDay, LocationNumber, txt);
        }

        private void CSD_UTC_OnConditionalStatusChanged(Conditional obj)
        {
            Log($"\t{obj.Name} => {obj.Status}");
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

        protected void Csd_OnStateChanged(CSD obj)
        {
            switch (obj.State)
            {
                case CSD_STATE.IDLE:
                case CSD_STATE.LOADING:
                case CSD_STATE.OCCUPIED:
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
            lock (SendAllowedLock)
            {
                if (!SendAllowed)
                    return;
                RequestWmsDirection(barcode);
            }

            NdwConnectCommunicator.BarcodeScannedUpdate(LocationNumber, barcode);
        }

        protected virtual void OnNoRead()
        {
            lock (SendAllowedLock)
            {
                if (!SendAllowed)
                    return;
                NotifyWmsNoread();
            }

            NdwConnectCommunicator.BarcodeScannedUpdate(LocationNumber, "NoRead");
        }

        protected void MessageSent()
        {
            lastRequestSent = DateTime.Now;
        }

        private void NotifyWmsNoread()
        {
            Log($"Notidy WMS NoRead: (loc:{LocationNumber})");
            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AnmeldungPackstück(0U, 0U, Encoding.ASCII.GetBytes("NoRead"), 0U, 0U, LocationNumber)));
            MessageSent();
        }

        protected virtual void RequestWmsDirection(string barcode)
        {
            Log($"Requesting direction for barcode:{barcode} (loc:{LocationNumber})");
            WmsCommunicator.Send(BaseMessage.MessageToByteArray(new AnmeldungPackstück(0U, 0U, Encoding.ASCII.GetBytes(barcode), 0U, 0U, LocationNumber)));
            MessageSent();
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
            var barcode = Encoding.ASCII.GetString((message as AnweisungPackstück).Barcode).TrimEnd(new char[1]);
            WmsSetDirection(barcode, (message as AnweisungPackstück).Direction, (message as AnweisungPackstück).Value1);
            NdwConnectCommunicator.DirectionRequestedUpdate(LocationNumber, barcode, (message as AnweisungPackstück).Direction);
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