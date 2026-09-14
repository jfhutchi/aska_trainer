using HutchASKA.Core.Compatibility;

namespace HutchASKA.Core.Features;

/// <summary>Owns session gating and failure isolation around one feature's native operations.</summary>
public sealed class HostedFeature : ITrainerFeature
{
    private readonly ITrainerFeature feature;
    private readonly Func<SinglePlayerDecision> decision;
    private readonly Action<string, Exception> reportError;
    private readonly FeatureCircuitBreaker breaker = new(3);
    private readonly FeatureExecutionGuard execution;
    private CompatibilityResult? compatibility;
    private bool active;
    private bool cleanupPending;

    public HostedFeature(ITrainerFeature feature, Func<SinglePlayerDecision> decision, Action<string, Exception> reportError)
    {
        this.feature = feature;
        this.decision = decision;
        this.reportError = reportError;
        execution = new FeatureExecutionGuard(breaker);
    }

    public string Id => feature.Id;
    public string DisplayName => feature.DisplayName;
    public FeatureState State { get; private set; }
    public string? StatusReason { get; private set; }

    public CompatibilityResult ProbeCompatibility()
    {
        if (compatibility is not null) return compatibility;
        if (!Run("Compatibility probe", () => compatibility = feature.ProbeCompatibility()))
            compatibility = CompatibilityResult.Incompatible(StatusReason!);
        if (!compatibility!.IsCompatible)
            SetState(FeatureState.Incompatible, compatibility.Reason);
        return compatibility;
    }

    public bool TryEnable()
    {
        if (State is FeatureState.Faulted or FeatureState.Incompatible) return false;
        if (!ProbeCompatibility().IsCompatible) return false;
        var gate = decision();
        if (!gate.Allowed)
        {
            Block(gate.Reason);
            return false;
        }
        if (active) return true;
        var enabled = false;
        if (!Run("Enable", () => enabled = feature.TryEnable()))
        {
            // Enable may have partially applied native state before throwing.
            active = true;
            Stop(FeatureState.Faulted, StatusReason);
            return false;
        }
        active = enabled;
        SetState(enabled ? FeatureState.Enabled : feature.State, feature.StatusReason);
        return enabled;
    }

    public void Tick()
    {
        TryExecute(feature.Tick);
    }

    public bool TryExecute(Action action)
    {
        if (!active || State != FeatureState.Enabled) return false;
        var gate = decision();
        if (!gate.Allowed) { Block(gate.Reason); return false; }
        var success = Run("Runtime action", action);
        if (!success && breaker.IsOpen)
            Stop(FeatureState.Faulted, StatusReason);
        return success;
    }

    public void Disable()
    {
        var terminal = State is FeatureState.Faulted or FeatureState.Incompatible;
        Stop(terminal ? State : FeatureState.Disabled, terminal ? StatusReason : null);
    }

    public void Reset()
    {
        Disable();
        if (State is FeatureState.Faulted or FeatureState.Incompatible) return;
        if (!Run("Reset", feature.Reset)) SetState(FeatureState.Faulted, StatusReason);
    }

    private void Block(string? reason) => Stop(FeatureState.Blocked, reason);

    private void Stop(FeatureState state, string? reason)
    {
        SetState(state, reason);
        if (!active && !cleanupPending) return;
        active = false;
        cleanupPending = true;
        // Cleanup must run even after the runtime breaker opens, once per activation.
        var cleanupBreaker = new FeatureCircuitBreaker(1);
        if (!new FeatureExecutionGuard(cleanupBreaker).TryRun(feature.Disable))
        {
            var error = cleanupBreaker.LastError!;
            SetState(FeatureState.Faulted, $"Restore native state: {error.Message}");
            reportError($"{Id}: restore native state", error);
        }
        else cleanupPending = false;
    }

    private bool Run(string operation, Action action)
    {
        if (execution.TryRun(action)) return true;
        var error = breaker.LastError!;
        StatusReason = $"{operation}: {error.Message}";
        reportError($"{Id}: {operation}", error);
        return false;
    }

    private void SetState(FeatureState state, string? reason)
    {
        State = state;
        StatusReason = reason;
    }
}
