// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.Conditional
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Collections.Generic;
using System.Linq;
using Ephi.Core.Helping;
using log4net;

namespace Ephi.Core.UTC.ConditionalStatements;

public abstract class Conditional
{
    private static readonly ILog log = LogManager.GetLogger("Conditionals");
    private readonly List<ConditionalContainer> owners;
    protected readonly RemoteUtc parent;
    private readonly object RaisePinChangedLock = new();
    private readonly Queue<CONDITIONAL_STATE> unhandledStates = new();
    private volatile bool onStateChangedHandling;
    private DateTime stateChanged = DateTime.Now;
    private CONDITIONAL_STATE status;
    private bool validationUnconfirmed = true;

    public Conditional(byte index, RemoteUtc parent, RUN_MODE runMode, string name)
    {
        this.parent = parent;
        owners = new List<ConditionalContainer>();
        Index = index;
        Name = name;
        RunMode = runMode;
        Status = CONDITIONAL_STATE.UNDER_CONSTRUCTION;
        IsPrecondition = false;
        globalTimeout = null;
    }

    public RemoteUtc Utc => parent;

    public bool IsPrecondition { get; private set; }

    public bool HasFinishedStateUpdateDelay { get; private set; }

    public bool StateUnconfirmed
    {
        get => validationUnconfirmed || Initializing;
        set => validationUnconfirmed = value;
    }

    public bool Initializing => Helpers.Contains(status, CONDITIONAL_STATE.READY_FOR_UPLOAD, CONDITIONAL_STATE.VALIDATION_PENDING, CONDITIONAL_STATE.UNDER_CONSTRUCTION);

    public byte Index { get; }

    public string Name { get; }

    public RUN_MODE RunMode { get; }

    public abstract byte[] Bytes { get; }

    public abstract UTC_FUNCTION MakeFunction { get; }

    public Timeout globalTimeout { get; private set; }

    public byte[] GlobalTimeoutBytes => globalTimeout?.Bytes(INPUT_CONDITION_TYPE.GLOBAL_TIMEOUT) ?? new byte[0];

    public CONDITIONAL_STATE LastStatus { get; private set; }

    public virtual CONDITIONAL_STATE Status
    {
        get => status;
        set
        {
            LastStatus = value;
            AsyncSetState(value);
        }
    }

    public TimeSpan StatusAge => DateTime.Now.Subtract(stateChanged);

    public bool IsRunningOrAboutToBe
    {
        get
        {
            lock (unhandledStates)
            {
                if (RunMode != RUN_MODE.PERMANENTLY)
                    if (!Helpers.Contains(Status, CONDITIONAL_STATE.RUN_REQUESTED, CONDITIONAL_STATE.RUNNING_PRECONDITIONS, CONDITIONAL_STATE.RUNNING))
                        return unhandledStates.Contains(CONDITIONAL_STATE.RUN_REQUESTED);
                return true;
            }
        }
    }

    public bool InherentlyIsRunningOrAboutToBe
    {
        get
        {
            if (IsRunningOrAboutToBe)
                return true;
            lock (owners)
            {
                return owners.Any(o => o.InherentlyIsRunningOrAboutToBe);
            }
        }
    }

    public bool InherentlyRunningPermanent
    {
        get
        {
            if (RunMode == RUN_MODE.PERMANENTLY)
                return true;
            lock (owners)
            {
                return owners.Any(o => o.InherentlyRunningPermanent);
            }
        }
    }

    public bool IsRunning => Helpers.Contains(Status, CONDITIONAL_STATE.CANCEL_REQUESTED, CONDITIONAL_STATE.RUNNING);

    public bool IsNotRunning => !IsRunningOrAboutToBe;

    public bool IsCancelled => LastStatus == CONDITIONAL_STATE.CANCELLED;

    public bool IsCancelledOrAboutToBe
    {
        get
        {
            if (RunMode == RUN_MODE.PERMANENTLY)
                return false;
            lock (unhandledStates)
            {
                if (RunMode != RUN_MODE.PERMANENTLY)
                    if (!Helpers.Contains(Status, CONDITIONAL_STATE.CANCEL_REQUESTED, CONDITIONAL_STATE.CANCELLED))
                        return unhandledStates.Contains(CONDITIONAL_STATE.CANCEL_REQUESTED) || unhandledStates.Contains(CONDITIONAL_STATE.CANCELLED);
                return true;
            }
        }
    }

    public bool InherentlyIsCancelledOrAboutToBe
    {
        get
        {
            if (IsCancelledOrAboutToBe)
                return true;
            lock (owners)
            {
                return owners.Any(o => o.InherentlyIsCancelledOrAboutToBe);
            }
        }
    }

    public bool IsFinished => LastStatus == CONDITIONAL_STATE.FINISHED;

    public bool IsTimedOut => LastStatus == CONDITIONAL_STATE.TIMED_OUT;

    public string IndexName => $"[{Index:D2}] {Name}";

    public string IndexNameStatus => $"{IndexName} ({Status}-{(StateUnconfirmed ? "Unc" : (object)"C")}onfirmed)";

    protected byte ConditionalInterpretationByte
    {
        get
        {
            var interpretationByte = Convert.ToByte((int)RunMode << 7);
            if (IsPrecondition)
                interpretationByte |= Convert.ToByte(64);
            if (HasFinishedStateUpdateDelay)
                interpretationByte |= Convert.ToByte(8);
            return interpretationByte;
        }
    }

    ~Conditional()
    {
        Dispose();
    }

    public void Dispose()
    {
        OnStateChanged = null;
    }

    internal void AddOwner(ConditionalContainer owner)
    {
        lock (owners)
        {
            if (owner == this || owners.Contains(owner))
                return;
            owners.Add(owner);
        }
    }

    protected T AddGlobalTimeout<T>(TIMEOUT_RANGE range, ushort multiplier) where T : Conditional
    {
        globalTimeout = globalTimeout == null ? Timeout<Timeout>.Make(range, multiplier) : throw new ArgumentException("Global timeout was already set for this conditional");
        Invalidate();
        return (T)this;
    }

    protected T MakePrecondition<T>() where T : Conditional
    {
        IsPrecondition = true;
        Invalidate();
        return (T)this;
    }

    protected T DelayFinishedStateUpdMsg<T>() where T : Conditional
    {
        HasFinishedStateUpdateDelay = true;
        Invalidate();
        return (T)this;
    }

    public void ResetTimeoutandError()
    {
        if (!Helpers.Contains(Status, CONDITIONAL_STATE.TIMED_OUT, CONDITIONAL_STATE.ERROR))
            return;
        Status = CONDITIONAL_STATE.VALID;
    }

    public event Action<Conditional> OnStateChanged;

    internal void ResetStateAge()
    {
        stateChanged = DateTime.Now;
    }

    public bool InherentlyContainsState(params CONDITIONAL_STATE[] states)
    {
        if (Helpers.Contains(Status, states))
            return true;
        lock (owners)
        {
            return owners.Any(o => o.InherentlyContainsState(states));
        }
    }

    public event Action<Conditional, CONDITIONAL_STATE> OnStateAboutToChange;

    private void AsyncSetState(CONDITIONAL_STATE newValue)
    {
        var stateAboutToChange = OnStateAboutToChange;
        stateAboutToChange?.Invoke(this, newValue);
        lock (unhandledStates)
        {
            unhandledStates.Enqueue(newValue);
            if (onStateChangedHandling)
                return;
            onStateChangedHandling = true;
        }

        var action = () =>
        {
            while (onStateChangedHandling)
                try
                {
                    lock (unhandledStates)
                    {
                        if (unhandledStates.Count == 0)
                        {
                            onStateChangedHandling = false;
                            break;
                        }

                        newValue = unhandledStates.Peek();
                    }

                    try
                    {
                        if (status != newValue)
                        {
                            if (RunMode == RUN_MODE.PERMANENTLY)
                                if (newValue == CONDITIONAL_STATE.CANCEL_REQUESTED)
                                    continue;
                            if (status == CONDITIONAL_STATE.DELETE_REQUESTED)
                                if (newValue != CONDITIONAL_STATE.DELETED)
                                    continue;
                            log.InfoFormat("{0} state change {1} => {2}", string.IsNullOrWhiteSpace(Name) ? "???" : (object)Name, status, newValue);
                            status = newValue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    finally
                    {
                        lock (unhandledStates)
                        {
                            newValue = unhandledStates.Dequeue();
                        }
                    }

                    stateChanged = DateTime.Now;
                    var onStateChanged = OnStateChanged;
                    onStateChanged?.Invoke(this);
                }
                catch (Exception ex)
                {
                    log.Error("RaiseOnStateChanged.1", ex);
                }
        };
        try
        {
            action.BeginInvoke(action.EndInvoke, null);
        }
        catch (Exception ex)
        {
            log.Error("RaiseOnStateChanged.2", ex);
        }
    }

    public void Invalidate()
    {
        Status = CONDITIONAL_STATE.UNDER_CONSTRUCTION;
        stateChanged = DateTime.Now;
    }

    public bool Run(bool forced = false)
    {
        return Run(TimeSpan.MinValue, forced);
    }

    public bool Run(TimeSpan timeout, bool forced = false)
    {
        if (parent == null)
            return false;
        switch (Status)
        {
            case CONDITIONAL_STATE.UNDER_CONSTRUCTION:
            case CONDITIONAL_STATE.READY_FOR_UPLOAD:
            case CONDITIONAL_STATE.VALIDATION_PENDING:
                return false;
            default:
                if (IsRunningOrAboutToBe && !forced)
                    return true;
                parent.RunConditional(this, timeout);
                return IsRunningOrAboutToBe;
        }
    }

    public bool Cancel(bool forced = false)
    {
        if (!forced && !IsRunningOrAboutToBe)
            return false;
        parent.CancelConditional(this);
        return true;
    }

    public void Delete()
    {
        parent.DeleteConditional(this);
    }

    public bool ReadyForStart(bool timeoutIsReady = true)
    {
        if (RunMode == RUN_MODE.PERMANENTLY)
            return false;
        if (Status == CONDITIONAL_STATE.TIMED_OUT)
            return timeoutIsReady;
        return Helpers.Contains(Status, CONDITIONAL_STATE.CANCEL_REQUESTED, CONDITIONAL_STATE.CANCELLED, CONDITIONAL_STATE.VALID, CONDITIONAL_STATE.FINISHED);
    }

    public override string ToString()
    {
        return IndexNameStatus;
    }
}