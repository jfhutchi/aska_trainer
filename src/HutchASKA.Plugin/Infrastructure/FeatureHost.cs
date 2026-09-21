using BepInEx.Logging;
using HutchASKA.Core.Features;

namespace HutchASKA.Plugin.Infrastructure;

public enum DiagnosticVerbosity { ErrorsOnly, Normal, Verbose }

public sealed class FeatureHost(SinglePlayerGuard guard, ManualLogSource log)
{
    public FeatureRegistry Registry { get; } = new();
    public DiagnosticVerbosity Verbosity { get; set; } = DiagnosticVerbosity.Normal;
    public bool HasPendingCleanup => Registry.Snapshot().OfType<HostedFeature>().Any(feature => feature.HasPendingCleanup);

    public HostedFeature Register(ITrainerFeature feature)
    {
        var hosted = new HostedFeature(feature, () => guard.Decision,
            (operation, error) => log.LogError($"{operation}: {error}"));
        Registry.Register(hosted);
        var compatibility = hosted.ProbeCompatibility();
        if (!compatibility.IsCompatible && Verbosity != DiagnosticVerbosity.ErrorsOnly)
            log.LogWarning($"{hosted.Id} incompatible: {compatibility.Reason}");
        return hosted;
    }

    public void Tick()
    {
        foreach (var feature in Registry.Snapshot()) feature.Tick();
    }

    public void DisableAll()
    {
        foreach (var feature in Registry.Snapshot()) feature.Disable();
    }

    public void RescanCompatibility()
    {
        DisableAll();
        foreach (var feature in Registry.Snapshot().Cast<HostedFeature>())
        {
            var compatibility = feature.RefreshCompatibility();
            if (!compatibility.IsCompatible && Verbosity != DiagnosticVerbosity.ErrorsOnly)
                log.LogWarning($"{feature.Id}: {feature.State}; {feature.StatusReason}");
            Trace($"Compatibility rescan: {feature.Id}: {feature.State}");
        }
    }

    public void Trace(string message)
    {
        if (Verbosity == DiagnosticVerbosity.Verbose) log.LogInfo(message);
    }
}
