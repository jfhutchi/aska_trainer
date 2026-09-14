using BepInEx.Logging;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.UI;

namespace HutchASKA.Plugin.Infrastructure;

internal sealed class DiagnosticsService(FeatureHost host, SinglePlayerGuard guard, RuntimeConfiguration config,
    RuntimeVersions versions, Func<string?> tribeStatus, ManualLogSource log)
{
    public RuntimeVersions Versions { get; } = versions;
    public IReadOnlyList<ITrainerFeature> Features => host.Registry.Snapshot();
    public string SessionStatus => $"{guard.Mode}; allowed: {guard.Decision.Allowed}. {guard.Decision.Reason}";
    public string? TribeStatus => tribeStatus();

    public string ResetAll() => Execute(config.ResetAll, "Gameplay features reset; controller multipliers restored to 1x and editor state cleared.");
    public string ReloadConfiguration() => Execute(config.Reload, "Configuration reloaded. Gameplay features remain off; enabled-state restoration is only considered on the next launch.");
    public string RescanCompatibility() => Execute(config.Rescan, "Compatibility rescanned after disabling features. Runtime-faulted features remain faulted until restart; no patches were re-enabled.");

    private string Execute(Action command, string success)
    {
        try
        {
            command();
            return host.HasPendingCleanup ? "Native restoration remains pending. Inspect the feature error in Diagnostics; retry Reset All after the cause is resolved." : success;
        }
        catch (Exception error)
        {
            // UI command boundary: a config/diagnostics failure must not break the trainer window.
            log.LogError($"Trainer diagnostics command: {error}");
            return error.Message;
        }
    }
}
