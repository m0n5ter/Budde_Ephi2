// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.RemoteUtc
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ephi.Core.Helping;
using Ephi.Core.Helping.General;
using Ephi.Core.Network;
using Ephi.Core.UTC.ConditionalStatements;
using log4net;

namespace Ephi.Core.UTC;

public class RemoteUtc : SustainedExpectedClient
{
    private readonly List<Conditional> conditionals = new();
    public readonly UtcConfiguration Configuration;
    private readonly Dictionary<PIN, InPin> inPins = new();
    private readonly List<byte> lastProcessedIndices = new();
    private readonly Dictionary<PIN, OutPin> outPins = new();

    private readonly Dictionary<CONDITIONAL_STATE, UTC_FUNCTION> pendingJobsCheck = new()
    {
        [CONDITIONAL_STATE.CANCEL_REQUESTED] = UTC_FUNCTION.C_DEACTIVATE,
        [CONDITIONAL_STATE.RUN_REQUESTED] = UTC_FUNCTION.C_ACTIVATE,
        [CONDITIONAL_STATE.DELETE_REQUESTED] = UTC_FUNCTION.C_DELETE
    };

    private readonly UtcServer server;
    private volatile byte currentConditionalIndex;
    private volatile byte currentLastConditionalIndex = byte.MaxValue;
    private DelayedEvent delayedUnoperational;
    private byte firmwareVersionMajor;
    private byte firmwareVersionMinor;
    private bool HandShakeCompleted;
    private byte hardwareVersionMajor;
    private byte hardwareVersionMinor;
    private int holdUpdates;
    private byte interfaceVersionMajor;
    private byte interfaceVersionMinor;
    private DateTime lastCheckState = DateTime.MinValue;
    private DateTime lastMsgProcessing = DateTime.MinValue;
    private DateTime lastPendingCheck = DateTime.MinValue;
    private DateTime lastValidationCheck = DateTime.MinValue;
    private HWValues local;
    private volatile bool operational;
    private volatile bool pinStatesDirty;
    private volatile UTC_STATUS RealStatus;
    private HWValues remote;
    private int remoteMismatchCycleCount;
    private DateTime remoteStateMismatchSince = DateTime.MinValue;
    private bool RemoteStateReceived;
    private volatile UTC_STATUS status;
    private DateTime statusChangedAt = DateTime.Now;

    protected internal RemoteUtc(UtcServer server, string logName)
        : base(logName)
    {
        this.server = server;
        Configuration = new UtcConfiguration();
        Init();
    }

    protected internal RemoteUtc(UtcServer server, ILog log)
        : base(log)
    {
        this.server = server;
        Configuration = new UtcConfiguration();
        Init();
    }

    public string HardwareVersion => $"{hardwareVersionMajor}.{hardwareVersionMinor}";

    public string FirmwareVersion => $"{firmwareVersionMajor}.{firmwareVersionMinor}";

    public string InterfaceVersion => $"{interfaceVersionMajor}.{interfaceVersionMinor}";

    public bool HardEmergency => remote.hardEmergency;

    public bool SoftEmergency
    {
        get => local.softEmergency;
        set
        {
            if (local.softEmergency == value)
                return;
            local.softEmergency = value;
            SendCurrentState();
            SoftEmrgChanged();
        }
    }

    public bool EmergencyStop
    {
        get => HardEmergency || SoftEmergency;
        set => SoftEmergency = value;
    }

    public bool CommunicationTimedOut
    {
        get
        {
            if (Configuration.HeartbeatTimeoutMs == 0 || ConnectionState != CONNECTION_STATE.CONNECTED || ConnectionStateAge < TimeSpan.FromSeconds(2.0))
                return false;
            var timeSpan = TimeSpan.FromMilliseconds(500.0);
            return ReceivedAge.Age > TimeSpan.FromMilliseconds(Configuration.HeartbeatTimeoutMs * 2) + timeSpan;
        }
    }

    private bool RemoteStateRequestDue => RemoteStateMismatch && DateTime.Now.Subtract(remoteStateMismatchSince) > TimeSpan.FromMilliseconds(500.0);

    private bool RemoteStateMismatch
    {
        get => remoteStateMismatchSince != DateTime.MinValue;
        set
        {
            value = value || !RemoteStateReceived;
            if (value)
            {
                if (remoteStateMismatchSince == DateTime.MinValue)
                    remoteStateMismatchSince = DateTime.Now;
            }
            else
            {
                remoteStateMismatchSince = DateTime.MinValue;
                remoteMismatchCycleCount = 0;
                HandShakeCompleted = true;
            }

            CheckStatus();
        }
    }

    public UTC_STATUS Status
    {
        get => status;
        set
        {
            if (RealStatus == value)
                return;
            RealStatus = value;
            try
            {
                if (UseDelayStatusUpdate(value))
                {
                    if (value == UTC_STATUS.DISPOSING)
                        return;
                    delayedUnoperational?.Start();
                }
                else
                {
                    SetStatus(value);
                }
            }
            finally
            {
                ApplyState();
            }
        }
    }

    public uint DelayedUnoperational_ms
    {
        get
        {
            var delayedUnoperational = this.delayedUnoperational;
            return delayedUnoperational?.Delay_ms ?? 0U;
        }
        set
        {
            if ((int)DelayedUnoperational_ms == (int)value)
                return;
            if (delayedUnoperational != null)
            {
                delayedUnoperational.Dispose();
                delayedUnoperational = null;
            }

            if (value <= 0U)
                return;
            delayedUnoperational = new DelayedEvent(value, DelayedUnoperationalElapsed);
        }
    }

    protected TimeSpan StatusAge => DateTime.Now.Subtract(statusChangedAt);

    public bool Operational
    {
        get => operational;
        private set
        {
            if (operational == value)
                return;
            operational = value;
            OperationalChanged();
            var operationalChanged = OnOperationalChanged;
            operationalChanged?.Invoke(this);
        }
    }

    public string StringVersions => $"\nHardware version: {HardwareVersion} \t\nFirmware version: {FirmwareVersion} \t\nInterface version: {InterfaceVersion}";

    public bool HoldUpdates
    {
        get => holdUpdates > 0;
        set
        {
            holdUpdates = Math.Max(0, holdUpdates + (value ? 1 : -1));
            if (HoldUpdates || !pinStatesDirty)
                return;
            UpdatePinStates(true);
        }
    }

    public Conditional[] Conditionals => conditionals.ToArray();

    private byte NewConditionalIndex
    {
        get
        {
            if (currentConditionalIndex >= currentLastConditionalIndex)
                throw new ArgumentException("Too many conditionals for this UTC");
            return currentConditionalIndex++;
        }
    }

    private byte NewLastConditionalIndex
    {
        get
        {
            if (currentLastConditionalIndex <= currentConditionalIndex)
                throw new ArgumentException("Too many conditionals for this UTC");
            return currentLastConditionalIndex--;
        }
    }

    public int BinarySize
    {
        get { return conditionals.Select(c => c.Bytes.Length).Sum(); }
    }

    private bool ConditionalsNeedValidation
    {
        get
        {
            lock (conditionals)
            {
                return conditionals.Any(m => m.Status == CONDITIONAL_STATE.READY_FOR_UPLOAD || m.Status == CONDITIONAL_STATE.ERROR);
            }
        }
    }

    private void Init()
    {
        OnPinChanges += PinsChanged;
        WorkCycleInterval = TimeSpan.FromMilliseconds(300.0);
        local = new HWValues();
        remote = new HWValues();
        HeartbeatTimeout = TimeSpan.MaxValue;
        server.RegisterClient(this);
    }

    ~RemoteUtc()
    {
        CleanUp();
    }

    public bool HardwareVersionAtOrBeyond(byte major, byte minor)
    {
        return VersionAtOrBeyond(major, minor, hardwareVersionMajor, hardwareVersionMinor);
    }

    public bool FirmwareVersionAtOrBeyond(byte major, byte minor)
    {
        return VersionAtOrBeyond(major, minor, firmwareVersionMajor, firmwareVersionMinor);
    }

    public bool InterfaceVersionAtOrBeyond(byte major, byte minor)
    {
        return VersionAtOrBeyond(major, minor, interfaceVersionMajor, interfaceVersionMinor);
    }

    private bool VersionAtOrBeyond(byte major, byte minor, byte refMajor, byte refMinor)
    {
        if (refMajor < major)
            return false;
        return refMajor > major || refMinor >= minor;
    }

    protected override void DoYourWork()
    {
        try
        {
            if (ConnectionState != CONNECTION_STATE.CONNECTED || RealStatus == UTC_STATUS.DISPOSING)
                return;
            if (CommunicationTimedOut)
            {
                log.Error("Missed too many heartbeats. Disconnecting");
                SoftDisconnect();
            }
            else
            {
                UpdatePinStates(false);
                ValidateConditionals();
                ValidatePendingJobs();
            }
        }
        finally
        {
            base.DoYourWork();
        }
    }

    private void ResetRemoteStateMismatch()
    {
        if (!RemoteStateMismatch)
            return;
        remoteStateMismatchSince = DateTime.Now;
    }

    private void ApplyState()
    {
        switch (RealStatus)
        {
            case UTC_STATUS.AWAITING_INITIALIZATION:
                lastValidationCheck = DateTime.MinValue;
                ValidateConditionals();
                break;
            case UTC_STATUS.OPERATIONAL:
                PinsDidChange();
                break;
        }
    }

    private bool UseDelayStatusUpdate(UTC_STATUS forValue)
    {
        if (Status != UTC_STATUS.OPERATIONAL || DelayedUnoperational_ms <= 0U)
            return false;
        return Helpers.Contains(forValue, UTC_STATUS.AWAITING_HANDSHAKE, UTC_STATUS.AWAITING_INITIALIZATION, UTC_STATUS.OFF_LINE);
    }

    private void DelayedUnoperationalElapsed()
    {
        SetStatus(RealStatus);
    }

    private void SetStatus(UTC_STATUS value)
    {
        if (status == value)
            return;
        status = value;
        statusChangedAt = DateTime.Now;
        Operational = Status == UTC_STATUS.OPERATIONAL;
        var onStatusChanged = OnStatusChanged;
        onStatusChanged?.Invoke(this);
        StatusChanged();
    }

    private void CheckStatus()
    {
        if (RealStatus == UTC_STATUS.DISPOSING)
            return;
        if (ConnectionState != CONNECTION_STATE.CONNECTED)
        {
            Status = UTC_STATUS.OFF_LINE;
        }
        else
        {
            lock (conditionals)
            {
                if (conditionals.Any(s => s.Status == CONDITIONAL_STATE.ERROR))
                {
                    Status = UTC_STATUS.ERROR;
                    return;
                }

                if (!HandShakeCompleted)
                {
                    Status = UTC_STATUS.AWAITING_HANDSHAKE;
                    return;
                }

                if (conditionals.Any(s => s.StateUnconfirmed))
                {
                    Status = UTC_STATUS.AWAITING_INITIALIZATION;
                    return;
                }

                if (conditionals.Any(s =>
                        Helpers.Contains(s.Status, CONDITIONAL_STATE.ERROR, CONDITIONAL_STATE.UNDER_CONSTRUCTION, CONDITIONAL_STATE.VALIDATION_PENDING, CONDITIONAL_STATE.READY_FOR_UPLOAD)))
                {
                    Status = UTC_STATUS.AWAITING_INITIALIZATION;
                    return;
                }
            }

            Status = UTC_STATUS.OPERATIONAL;
        }
    }

    public event Action<RemoteUtc> OnStatusChanged;

    protected virtual void OperationalChanged()
    {
    }

    public event Action<RemoteUtc> OnOperationalChanged;

    protected virtual void StatusChanged()
    {
    }

    public event Action<RemoteUtc> OnHardEmrgChanged;

    protected virtual void HardEmrgChanged()
    {
        var onHardEmrgChanged = OnHardEmrgChanged;
        onHardEmrgChanged?.Invoke(this);
    }

    public event Action<RemoteUtc> OnSoftEmrgChanged;

    protected virtual void SoftEmrgChanged()
    {
        var onSoftEmrgChanged = OnSoftEmrgChanged;
        onSoftEmrgChanged?.Invoke(this);
    }

    public void LogVersions()
    {
        log.Info(StringVersions);
    }

    public override void CleanUp()
    {
        base.CleanUp();
        Status = UTC_STATUS.DISPOSING;
        while (ConnectionState == CONNECTION_STATE.CONNECTED && !SendQueueEmpty && StatusAge < TimeSpan.FromSeconds(1.0))
            Thread.Sleep(99);
        Disconnect();
        server.UnRegisterClient(this);
        OnPinChanges -= PinsChanged;
        OnPinChanges = null;
        OnStatusChanged = null;
        OnConditionalStatusChanged = null;
        inPins.Values.ToList().ForEach(p => p.Dispose());
        outPins.Values.ToList().ForEach(p => p.Dispose());
        inPins.Clear();
        outPins.Clear();
    }

    private void UpdatePinStates(bool forced)
    {
        if (HoldUpdates)
        {
            pinStatesDirty = true;
        }
        else
        {
            pinStatesDirty = false;
            if (!RemoteStateReceived || (!forced && lastCheckState.Add(TimeSpan.FromMilliseconds(400.0)) > DateTime.Now))
                return;
            lastCheckState = DateTime.Now;
            var num1 = local.CompareAndEqualize(remote);
            var flag1 = (num1 & 8U) > 0U;
            var num2 = (num1 & 4U) > 0U ? 1 : 0;
            var flag2 = (num1 & 2U) > 0U;
            var flag3 = (num1 & 16U) > 0U;
            var flag4 = flag3 || (num1 & 128U) > 0U;
            if ((num2 | (flag2 ? 1 : 0) | (flag4 ? 1 : 0)) != 0)
            {
                RemoteStateMismatch = true;
                if (RemoteStateReceived && ++remoteMismatchCycleCount < 3)
                    SendCurrentState();
                else if (RemoteStateRequestDue)
                    RequestCurrentState();
                if (flag3)
                    HardEmrgChanged();
            }
            else
            {
                RemoteStateMismatch = false;
                if (RemoteStateRequestDue)
                    RequestCurrentState();
            }

            if (flag1)
                SendCurrentInputOverrideState();
            if ((num2 | (flag2 ? 1 : 0) | (flag1 ? 1 : 0)) == 0)
                return;
            PinsDidChange();
        }
    }

    private byte[] ProcessAcknowledges(FunctionMessage fm, byte[] payload)
    {
        if (fm.Function != UTC_FUNCTION.CONFIRM || payload.Length < 1)
            return payload;
        var indices = Protocol.GetIndices(ref payload);
        indices?.ToList().ForEach(i => Acknowledged(i));
        return payload;
    }

    protected override void MessageReceived(byte[] payload)
    {
        if (DateTime.Now.Subtract(lastMsgProcessing) > TimeSpan.FromSeconds(2.0))
            lastProcessedIndices.Clear();
        base.MessageReceived(payload);
        var flag1 = false;
        var flag2 = false;
        var flag3 = false;
        var flag4 = false;
        var source1 = new List<byte>();
        FunctionMessage header;
        while ((header = Protocol.FindHeader(ref payload)) != null)
            if (header.Function == UTC_FUNCTION.CONFIRM)
            {
                payload = ProcessAcknowledges(header, payload);
            }
            else if (!source1.Contains(header.MsgId) && !lastProcessedIndices.Contains(header.MsgId))
            {
                var function = header.Function;
                if ((uint)function <= 17U)
                    switch (function)
                    {
                        case UTC_FUNCTION.VERSION:
                            if (payload.Length >= 6)
                            {
                                hardwareVersionMajor = payload[0];
                                hardwareVersionMinor = payload[1];
                                firmwareVersionMajor = payload[2];
                                firmwareVersionMinor = payload[3];
                                interfaceVersionMajor = payload[4];
                                interfaceVersionMinor = payload[5];
                                payload = payload.Skip(6).ToArray();
                                LogVersions();
                                break;
                            }

                            continue;
                        case UTC_FUNCTION.REQUEST_STATE:
                            flag3 = true;
                            break;
                    }
                else if ((uint)function <= 32U)
                    switch (function)
                    {
                        case UTC_FUNCTION.CURRENT_STATE:
                            if (payload.Length >= 11)
                            {
                                RemoteStateReceived = true;
                                remoteMismatchCycleCount = 0;
                                var flag5 = remote.SetStateFromPayload(ref payload);
                                if (!remote.Equals(local))
                                {
                                    flag1 = true;
                                    break;
                                }

                                if (flag5) flag2 = true;
                                break;
                            }

                            continue;
                        case UTC_FUNCTION.INPUT_UPDATE:
                            if (payload.Length >= 6)
                            {
                                remote.inputOverrides.SetStateFromPayload(ref payload);
                                flag2 = true;
                                break;
                            }

                            continue;
                    }
                else
                    switch (function)
                    {
                        case UTC_FUNCTION.CONDITIONAL_FORCED_UPDATE:
                            if (payload.Length >= 12)
                            {
                                var hwOverrides = new HWOverrides();
                                var source2 = new HWOverrides();
                                hwOverrides.SetStateFromPayload(ref payload);
                                source2.SetStateFromPayload(ref payload);
                                var flag6 = hwOverrides.CompareAndAdd(local.requestedOutputData) | flag4;
                                hwOverrides.CompareAndAdd(remote.requestedOutputData);
                                flag4 = local.inputOverrides.CompareAndCombine(source2) | flag6;
                                remote.inputOverrides.CompareAndCombine(source2);
                                if (flag4) flag2 = true;
                                break;
                            }

                            continue;
                        case UTC_FUNCTION.C_STATE_UPDATE:
                            if (payload.Length >= 3)
                            {
                                if (Enum.IsDefined(typeof(CONDITIONAL_REMOTE_STATE), payload[0]))
                                {
                                    var status = (CONDITIONAL_REMOTE_STATE)payload[0];
                                    var indices = Protocol.GetIndices(ref payload, 1);
                                    if (indices != null && indices.Length != 0)
                                    {
                                        foreach (var index in indices)
                                            ConditionalStateChanged(status, index);
                                        if (InterfaceVersionAtOrBeyond(2, 4))
                                        {
                                            SendMessage(UTC_FUNCTION.C_STATE_UPDATE, new byte[2]
                                            {
                                                (byte)status,
                                                (byte)indices.Length
                                            }.Concat(indices).ToArray());
                                            continue;
                                        }

                                        break;
                                    }

                                    continue;
                                }

                                break;
                            }

                            continue;
                        case UTC_FUNCTION.C_CURRENT_COUNT:
                            if (payload.Length >= 1)
                            {
                                ushort num = payload[0];
                                payload = payload.Skip(1).ToArray();
                                lock (conditionals)
                                {
                                    if (num == conditionals.Count)
                                    {
                                        SendMessage(UTC_FUNCTION.C_REQ_STATES);
                                        break;
                                    }

                                    InvalidateConditionals();
                                    break;
                                }
                            }

                            continue;
                        case UTC_FUNCTION.C_MEASURE_RESULT:
                            if (payload.Length >= 6 && Find(payload[0]) is ConditionalMeasurement conditionalMeasurement)
                            {
                                var array = new byte[1].Concat(payload.Skip(2).Take(3)).ToArray();
                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(array);
                                var result = new MeasurementResult(BitConverter.ToInt32(array, 0), (payload[1] & 128) > 0, (payload[1] & 64) > 0, payload[1] & 63);
                                conditionalMeasurement.RaiseOnMeasurementReceived(result);
                                payload = payload.Skip(6).ToArray();
                                break;
                            }

                            continue;
                    }

                source1.Add(header.MsgId);
                lastProcessedIndices.Add(header.MsgId);
                while (lastProcessedIndices.Count > 10)
                    lastProcessedIndices.RemoveAt(0);
            }

        var list = source1.Distinct().ToList();
        list.Insert(0, (byte)list.Count);
        if (list.Count > 1)
            SendMessage(UTC_FUNCTION.CONFIRM, list.ToArray());
        if (flag1)
            UpdatePinStates(true);
        else if (flag3)
            SendCurrentState();
        if (flag4)
            SendCurrentInputOverrideState();
        if (flag2)
            PinsDidChange();
        lastMsgProcessing = DateTime.Now;
    }

    protected override void ConnectionStateChanged()
    {
        try
        {
            RemoteStateMismatch = true;
            HandShakeCompleted = false;
            RemoteStateReceived = false;
            log.InfoFormat("State changed to: {0}", Formatting.TitleCase(ConnectionState));
            if (ConnectionState != CONNECTION_STATE.CONNECTED)
            {
                ClearResendQueue();
                if (ConnectionState == CONNECTION_STATE.DISCONNECTED)
                    log.Error("Got disconnected");
                if (!Configuration.CancelConditionalsOnDisconnect)
                    return;
                lock (conditionals)
                {
                    conditionals.FindAll(s => s.IsRunningOrAboutToBe).ForEach(s => s.Status = CONDITIONAL_STATE.CANCEL_REQUESTED);
                }
            }
            else
            {
                SendCurrentState();
                SendCurrentInputOverrideState();
                SendUtcConfiguration();
                if (ConditionalsNeedValidation)
                    InvalidateConditionals();
                else
                    lock (conditionals)
                    {
                        if (conditionals.Count <= 0)
                            return;
                        conditionals.ForEach(c => c.StateUnconfirmed = true);
                        SendMessage(UTC_FUNCTION.C_REQ_COUNT);
                    }
            }
        }
        finally
        {
            CheckStatus();
            base.ConnectionStateChanged();
        }
    }

    private void SendCurrentInputOverrideState()
    {
        lastCheckState = DateTime.Now;
        SendMessage(UTC_FUNCTION.INPUT_UPDATE, local.inputOverrides.GetStatePayload);
    }

    private void SendCurrentState()
    {
        ResetRemoteStateMismatch();
        lastCheckState = DateTime.Now;
        SendMessage(UTC_FUNCTION.CURRENT_STATE, local.GetStatePayload);
    }

    private void RequestCurrentState()
    {
        ResetRemoteStateMismatch();
        remoteMismatchCycleCount = 0;
        SendMessage(UTC_FUNCTION.REQUEST_STATE);
    }

    public void SendUtcConfiguration()
    {
        var num1 = Convert.ToByte(Math.Min(byte.MaxValue, Configuration.HeartbeatTimeoutMs / 10));
        var num2 = Convert.ToByte(Configuration.Mask);
        SendMessage(UTC_FUNCTION.SET_HEARTBEAT_TIMEOUT, new byte[1]
        {
            num1
        });
        SendMessage(UTC_FUNCTION.SET_CONFIGURATION, new byte[1]
        {
            num2
        });
    }

    private void SendMessage(UTC_FUNCTION function, byte[] payload = null)
    {
        var functionMessage = new FunctionMessage(function)
        {
            content = payload ?? new byte[0]
        };
        if (Helpers.Contains(function, UTC_FUNCTION.CONFIRM, UTC_FUNCTION.C_STATE_UPDATE))
            Send(functionMessage.GenerateMessage);
        else
            Send(functionMessage.GenerateMessage, functionMessage.MsgId);
    }

    private void Cancel(byte ackId)
    {
        Acknowledged(ackId);
    }

    private void ConditionalStateChanged(CONDITIONAL_REMOTE_STATE status, byte index)
    {
        Conditional cnd;
        if ((cnd = Find(index)) == null)
        {
            log.ErrorFormat("{0} received for not-existing index: {1}", status, index);
        }
        else
        {
            cnd.StateUnconfirmed = false;
            if (cnd.Status == CONDITIONAL_STATE.DELETE_REQUESTED)
            {
                if (status != CONDITIONAL_REMOTE_STATE.DELETED)
                    return;
                ApplyValidState(cnd, CONDITIONAL_REMOTE_STATE.DELETED);
                lock (conditionals)
                {
                    conditionals.Remove(cnd);
                }

                cnd.Dispose();
            }
            else
            {
                switch (status)
                {
                    case CONDITIONAL_REMOTE_STATE.VALID:
                        if (cnd.Status == CONDITIONAL_STATE.VALIDATION_PENDING)
                        {
                            ApplyValidState(cnd, status);
                            break;
                        }

                        if (cnd.IsRunningOrAboutToBe)
                        {
                            log.WarnFormat("Conditional '{0}' confirmed to be {1} while in state {2}. Restarting!", cnd.Name, Formatting.TitleCase(status), Formatting.TitleCase(cnd.Status));
                            cnd.Run();
                        }

                        break;
                    case CONDITIONAL_REMOTE_STATE.RUNNING:
                    case CONDITIONAL_REMOTE_STATE.RUNNING_PRECONDITIONS:
                        if (cnd.InherentlyIsRunningOrAboutToBe || cnd.InherentlyRunningPermanent)
                        {
                            ApplyValidState(cnd, status);
                            break;
                        }

                        log.WarnFormat("Conditional '{0}' confirmed to be {1} while in state {2}. Cancelling!", cnd.Name, Formatting.TitleCase(status), Formatting.TitleCase(cnd.Status));
                        cnd.Cancel(true);
                        break;
                    case CONDITIONAL_REMOTE_STATE.CANCELLED:
                        if (!cnd.InherentlyIsCancelledOrAboutToBe && !cnd.InherentlyIsRunningOrAboutToBe)
                            if (!cnd.InherentlyContainsState(CONDITIONAL_STATE.TIMED_OUT))
                            {
                                if (cnd.IsRunningOrAboutToBe)
                                {
                                    log.WarnFormat("Conditional '{0}' confirmed to be {1} while in state {2}. {3}", cnd.Name, Formatting.TitleCase(status), Formatting.TitleCase(cnd.Status),
                                        cnd.IsRunningOrAboutToBe ? "Restarting!" : (object)"");
                                    cnd.Run(true);
                                    break;
                                }

                                ApplyValidState(cnd, status);
                                break;
                            }

                        ApplyValidState(cnd, status);
                        break;
                    case CONDITIONAL_REMOTE_STATE.DELETED:
                        log.WarnFormat("Conditional '{0}' confirmed to be {1} while in state {2}. Uploading!", cnd.Name, Formatting.TitleCase(status), Formatting.TitleCase(cnd.Status));
                        cnd.Status = CONDITIONAL_STATE.READY_FOR_UPLOAD;
                        break;
                    default:
                        ApplyValidState(cnd, status);
                        break;
                }

                CheckStatus();
            }
        }
    }

    private void ValidatePendingJobs()
    {
        if (RealStatus != UTC_STATUS.OPERATIONAL || lastPendingCheck.AddMilliseconds(200.0) > DateTime.Now)
            return;
        lastPendingCheck = DateTime.Now;
        lock (conditionals)
        {
            foreach (var keyValuePair in pendingJobsCheck)
            {
                var kvp = keyValuePair;
                var list = conditionals.FindAll(m => m.Status == kvp.Key && m.StatusAge > TimeSpan.FromSeconds(1.0)).Select(m =>
                {
                    m.ResetStateAge();
                    return m.Index;
                }).Distinct().ToList();
                list.Insert(0, (byte)list.Count);
                if (list.Count > 1)
                    SendMessage(kvp.Value, list.ToArray());
            }
        }
    }

    private void ValidateConditionals()
    {
        CheckStatus();
        if (RealStatus != UTC_STATUS.AWAITING_INITIALIZATION || lastValidationCheck.AddMilliseconds(200.0) > DateTime.Now)
            return;
        lock (conditionals)
        {
            var count = Math.Max(10, conditionals.FindAll(m => m.Status == CONDITIONAL_STATE.VALIDATION_PENDING).Count());
            conditionals.FindAll(m => m.Status == CONDITIONAL_STATE.VALIDATION_PENDING && m.StatusAge > TimeSpan.FromSeconds(5.0)).ForEach(m => m.Status = CONDITIONAL_STATE.READY_FOR_UPLOAD);
            conditionals.FindAll(m => m.Status == CONDITIONAL_STATE.UNDER_CONSTRUCTION && m.StatusAge > TimeSpan.FromMilliseconds(10.0)).ForEach(m => m.Status = CONDITIONAL_STATE.READY_FOR_UPLOAD);
            var list = conditionals.FindAll(m => m.Status == CONDITIONAL_STATE.READY_FOR_UPLOAD || m.Status == CONDITIONAL_STATE.ERROR).Where(m => !(m is ConditionalContainer)).Take(count).ToList();
            if (list.Count == 0)
                list = conditionals.FindAll(m => m.Status == CONDITIONAL_STATE.READY_FOR_UPLOAD || m.Status == CONDITIONAL_STATE.ERROR).OfType<ConditionalContainer>().Take<Conditional>(count)
                    .ToList();
            list.ForEach(m =>
            {
                if (!InterfaceVersionAtOrBeyond(2, 4) && m.MakeFunction == UTC_FUNCTION.C_SET_MEASUREMENT)
                {
                    log.Error("This UTC firmware does not support ConditionalMeasurement types");
                    m.Status = CONDITIONAL_STATE.ERROR;
                }
                else
                {
                    m.Status = CONDITIONAL_STATE.VALIDATION_PENDING;
                    try
                    {
                        SendMessage(m.MakeFunction, m.Bytes);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error uploading conditional {m.Name}", ex);
                        throw;
                    }
                }
            });
        }

        lastValidationCheck = DateTime.Now;
    }

    protected void InvalidateConditionals()
    {
        lock (conditionals)
        {
            conditionals.FindAll(c => !c.Initializing).ForEach(m => m.Status = CONDITIONAL_STATE.UNDER_CONSTRUCTION);
        }

        CheckStatus();
        lastValidationCheck = DateTime.MinValue;
    }

    internal Conditional Find(byte index)
    {
        lock (conditionals)
        {
            return conditionals.FirstOrDefault(s => s.Index == index);
        }
    }

    private void ApplyValidState(Conditional cnd, CONDITIONAL_REMOTE_STATE status)
    {
        log.InfoFormat("{0}\t{1}", $"{Formatting.TitleCase(cnd.Status)} => {Formatting.TitleCase(status)}".PadRight(35), cnd.Name);
        cnd.Status = (CONDITIONAL_STATE)status;
    }

    public void ResetCounter()
    {
        currentConditionalIndex = 0;
        currentLastConditionalIndex = byte.MaxValue;
    }

    public ConditionalStatement MakeConditionalStatement(
        string name,
        OUTPUT_ENFORCEMENT enforcement,
        RUN_MODE runMode = RUN_MODE.ON_DEMAND,
        RUN_PRIORITY prio = RUN_PRIORITY.NORMAL)
    {
        return AddConditional(new ConditionalStatement(prio == RUN_PRIORITY.NORMAL ? NewConditionalIndex : NewLastConditionalIndex, this, enforcement, runMode, name));
    }

    public ConditionalMeasurement MakeConditionalMeasurement(
        string name,
        RUN_MODE runMode = RUN_MODE.ON_DEMAND,
        RUN_PRIORITY prio = RUN_PRIORITY.NORMAL)
    {
        return AddConditional(new ConditionalMeasurement(prio == RUN_PRIORITY.NORMAL ? NewConditionalIndex : NewLastConditionalIndex, this, runMode, name));
    }

    public ConditionalMacro MakeConditionalMacro(string name, RUN_MODE runMode = RUN_MODE.ON_DEMAND, RUN_PRIORITY prio = RUN_PRIORITY.NORMAL)
    {
        return AddConditional(new ConditionalMacro(prio == RUN_PRIORITY.NORMAL ? NewConditionalIndex : NewLastConditionalIndex, this, runMode, name));
    }

    public ConditionalBatch MakeConditionalBatch(string name, RUN_MODE runMode = RUN_MODE.ON_DEMAND, RUN_PRIORITY prio = RUN_PRIORITY.NORMAL)
    {
        return AddConditional(new ConditionalBatch(prio == RUN_PRIORITY.NORMAL ? NewConditionalIndex : NewLastConditionalIndex, this, runMode, name));
    }

    private T AddConditional<T>(T conditional) where T : Conditional
    {
        conditional.OnStateChanged += st => RaiseOnConditionalStatusChanged(st);
        lock (conditionals)
        {
            conditionals.Add(conditional);
        }

        return conditional;
    }

    internal void RunConditional(Conditional conditional, TimeSpan timeout)
    {
        RunConditionals(new Conditional[1] { conditional }, timeout);
    }

    internal void RunConditionals(Conditional[] conditionals, TimeSpan timeout)
    {
        if (conditionals.Length == 0)
            return;
        string.Join(", ", conditionals.Select(c => c.Name));
        if (timeout == TimeSpan.MinValue)
        {
            var list = conditionals.Select(c => c.Index).ToList();
            list.Insert(0, (byte)list.Count());
            SendMessage(UTC_FUNCTION.C_ACTIVATE, list.ToArray());
            Array.ForEach(conditionals, c => c.Status = CONDITIONAL_STATE.RUN_REQUESTED);
        }
        else
        {
            var ms100 = Convert.ToByte(Math.Min(byte.MaxValue, Math.Round((int)Math.Min(timeout.TotalMilliseconds, 25500.0) / 100.0)));
            Array.ForEach(conditionals, c =>
            {
                c.Status = CONDITIONAL_STATE.RUN_REQUESTED;
                SendMessage(UTC_FUNCTION.C_ACTIVATE_TIMEOUT, new byte[2]
                {
                    c.Index,
                    ms100
                });
            });
        }
    }

    internal void CancelConditional(Conditional conditional)
    {
        CancelConditionals(new Conditional[1]
        {
            conditional
        });
    }

    internal void CancelConditionals(Conditional[] conditionals)
    {
        SendBatch(conditionals, UTC_FUNCTION.C_DEACTIVATE, CONDITIONAL_STATE.CANCEL_REQUESTED, "Cancel");
    }

    public void CancelConditionals(byte[] indices)
    {
        SendBatch(indices, UTC_FUNCTION.C_DEACTIVATE, CONDITIONAL_STATE.CANCEL_REQUESTED, "Cancel");
    }

    public virtual void CancelAllConditionals()
    {
        lock (conditionals)
        {
            conditionals.FindAll(c => c.IsRunningOrAboutToBe).ForEach(c => c.Status = CONDITIONAL_STATE.CANCEL_REQUESTED);
        }

        SendMessage(UTC_FUNCTION.C_DEACTIVATE_ALL);
    }

    internal void DeleteConditional(Conditional conditional)
    {
        DeleteConditionals(new Conditional[1]
        {
            conditional
        });
    }

    internal void DeleteConditionals(Conditional[] conditionals)
    {
        SendBatch(conditionals, UTC_FUNCTION.C_DELETE, CONDITIONAL_STATE.DELETE_REQUESTED, "Delete");
    }

    public void DeleteConditionals(byte[] indices)
    {
        SendBatch(indices, UTC_FUNCTION.C_DELETE, CONDITIONAL_STATE.DELETE_REQUESTED, "Delete");
    }

    public virtual void DeleteAllConditionals()
    {
        SendMessage(UTC_FUNCTION.C_DELETE_ALL);
        lock (conditionals)
        {
            conditionals.ForEach(s => s.Dispose());
            conditionals.Clear();
            ResetCounter();
        }
    }

    private void SendBatch(
        byte[] indices,
        UTC_FUNCTION function,
        CONDITIONAL_STATE state,
        string action)
    {
        var illegal = new List<byte>();
        Conditional fnd;
        var array = indices.Select(idx =>
        {
            if ((fnd = Find(idx)) != null)
                return fnd;
            illegal.Add(idx);
            return null;
        }).Where(c => c != null).ToArray();
        if (illegal.Count > 0)
            log.ErrorFormat("No conditional found for this index / these indices: {0}", string.Join(", ", illegal.Select(c => c.ToString("X2"))));
        if (array.Length == 0)
            log.InfoFormat("{0} necessary for none of the indices", action);
        else
            SendBatch(array, function, state, action);
    }

    private void SendBatch(
        Conditional[] conditionals,
        UTC_FUNCTION function,
        CONDITIONAL_STATE state,
        string action)
    {
        if (conditionals.Length == 0)
            return;
        string.Join(", ", conditionals.Select(c => c.Name));
        var list = conditionals.Select(c => c.Index).ToList();
        list.Insert(0, (byte)list.Count());
        SendMessage(function, list.ToArray());
        Array.ForEach(conditionals, c => c.Status = state);
    }

    public virtual void ResetTimeoutsAndErrors()
    {
        lock (conditionals)
        {
            conditionals.FindAll(s => Helpers.Contains(s.Status, CONDITIONAL_STATE.TIMED_OUT, CONDITIONAL_STATE.ERROR)).ForEach(s => s.Status = CONDITIONAL_STATE.VALID);
        }
    }

    public event Action<Conditional> OnConditionalStatusChanged;

    private void RaiseOnConditionalStatusChanged(Conditional c)
    {
        AsyncEvents.Raise(OnConditionalStatusChanged, ex => log.Error(nameof(RaiseOnConditionalStatusChanged), ex), c);
    }

    private void PinsDidChange()
    {
        if (RealStatus != UTC_STATUS.OPERATIONAL)
            return;
        lock (inPins)
        {
            inPins.Values.ToList().ForEach(p => p.CheckPinChanged());
        }

        lock (outPins)
        {
            outPins.Values.ToList().ForEach(p => p.CheckPinChanged());
        }

        RaiseOnPinChanges();
    }

    public void SetPin(BasePin pin, LEVEL pinState)
    {
        if (pin.IsDummy)
            return;
        SetPin(pin.Pin, pinState, pin is InPin ? PIN_DIRECTION.IN : PIN_DIRECTION.OUT);
    }

    public void SetPin(PIN pin, LEVEL pinState, PIN_DIRECTION dir = PIN_DIRECTION.OUT)
    {
        if ((dir == PIN_DIRECTION.IN ? local.inputOverrides.SetOverride(pin, pinState) ? 1 : 0 : local.requestedOutputData.SetPin(pin, pinState) ? 1 : 0) == 0)
            return;
        UpdatePinStates(true);
    }

    public void ActivateAll(PIN_DIRECTION dir = PIN_DIRECTION.OUT)
    {
        HoldUpdates = true;
        try
        {
            Array.ForEach(GetRegisteredPins(dir), pin => pin.Activate());
        }
        finally
        {
            HoldUpdates = false;
        }
    }

    public void DeactivateAll(PIN_DIRECTION dir)
    {
        HoldUpdates = true;
        try
        {
            Array.ForEach(GetRegisteredPins(dir), pin => pin.Deactivate());
        }
        finally
        {
            HoldUpdates = false;
        }
    }

    private BasePin[] GetRegisteredPins(PIN_DIRECTION dir)
    {
        switch (dir)
        {
            case PIN_DIRECTION.OUT:
                lock (outPins)
                {
                    return outPins.Values.ToArray();
                }
            case PIN_DIRECTION.IN:
                lock (inPins)
                {
                    return inPins.Values.ToArray();
                }
            default:
                return new BasePin[0];
        }
    }

    public void UnsetOverride(InPin pin)
    {
        if (pin.IsDummy)
            return;
        UnsetOverride(pin.Pin);
    }

    public void UnsetOverride(PIN pin)
    {
        if (!local.inputOverrides.UnSetOverride(pin))
            return;
        UpdatePinStates(true);
    }

    public void UnsetOverrideAll()
    {
        local.inputOverrides.UnSetOverrideAll();
    }

    public LEVEL GetPin(BasePin pin)
    {
        return !(pin is InPin) ? GetPinOut(pin.Pin) : GetPinIn(pin.Pin);
    }

    public LEVEL GetPinIn(PIN pinNumber)
    {
        return RealStatus == UTC_STATUS.OPERATIONAL ? remote.inputData.GetPin(pinNumber) : LEVEL.UNDETERMINED;
    }

    public LEVEL GetPinOut(PIN pinNumber)
    {
        return RealStatus == UTC_STATUS.OPERATIONAL ? remote.realOutputData.GetPin(pinNumber) : LEVEL.UNDETERMINED;
    }

    public LEVEL GetPinRequested(BasePin pin)
    {
        return !(pin is InPin) ? GetPinOutRequested(pin.Pin) : GetPinInRequested(pin.Pin);
    }

    public LEVEL GetPinOutRequested(PIN pinNumber)
    {
        return remote.requestedOutputData.GetPin(pinNumber);
    }

    public LEVEL GetPinInRequested(PIN pinNumber)
    {
        return remote.inputOverrides.GetPinOverride(pinNumber);
    }

    public InPin MakeIn(PIN pin, LEVEL activeState = LEVEL.HIGH)
    {
        lock (inPins)
        {
            if (!inPins.ContainsKey(pin))
                inPins.Add(pin, InPin.Make(this, pin, activeState));
            return inPins[pin];
        }
    }

    public OutPin MakeOut(PIN pin, LEVEL activeState = LEVEL.HIGH)
    {
        lock (outPins)
        {
            if (!outPins.ContainsKey(pin))
                outPins.Add(pin, OutPin.Make(this, pin, activeState));
            return outPins[pin];
        }
    }

    public event Action<RemoteUtc> OnPinChanges;

    private void RaiseOnPinChanges()
    {
        AsyncEvents.Raise(OnPinChanges, this);
    }

    protected virtual void PinsChanged(RemoteUtc utc)
    {
    }
}