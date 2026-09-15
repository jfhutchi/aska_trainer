using BepInEx.Configuration;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;
using HutchASKA.Plugin.Player;
using HutchASKA.Plugin.World;

namespace HutchASKA.Plugin.Configuration;

internal sealed class RuntimeConfiguration
{
    private readonly FeatureHost host;
    private readonly ConfigFile config;
    private readonly MovementSpeedFeature movement;
    private readonly GameSpeedFeature speed;
    private readonly HarvestSpeedFeature harvesting;
    private readonly Dictionary<string, ConfigEntry<bool>> enabled = new();
    private bool restored;
    public ConfigEntry<bool> RestoreStates { get; }
    public ConfigEntry<float> Movement { get; }
    public ConfigEntry<float> GameSpeed { get; }
    public ConfigEntry<int> HarvestSpeed { get; }
    public ConfigEntry<DiagnosticVerbosity> Verbosity { get; }
    public event Action? TransientStateCleared;
    public RuntimeConfiguration(ConfigFile config, FeatureHost host, MovementSpeedFeature movement, GameSpeedFeature speed, HarvestSpeedFeature harvesting)
    {
        this.host = host;
        this.config = config;
        this.movement = movement;
        this.speed = speed;
        this.harvesting = harvesting;
        RestoreStates = config.Bind("General", "RestoreEnabledStatesOnLaunch", false, "Opt in to restoring previously selected cheats after single-player confirmation.");
        Movement = config.Bind("Player", "MovementMultiplier", 1f, "Movement multiplier, 1 to 5.");
        GameSpeed = config.Bind("World", "GameSpeedMultiplier", 1f, "Game speed multiplier, 0.5 to 5.");
        HarvestSpeed = config.Bind("Player", "HarvestSpeedMultiplier", 1, "Player harvesting speed preset: 1 (normal), 2, 3 or 4. Does not change global game speed.");
        Verbosity = config.Bind("Diagnostics", "LoggingVerbosity", DiagnosticVerbosity.Normal, "Trainer diagnostics only: ErrorsOnly, Normal or Verbose. Errors are always logged.");
        Verbosity.SettingChanged += (_, _) => host.Verbosity = Verbosity.Value;
        host.Verbosity = Verbosity.Value;
        movement.Multiplier.Value = Movement.Value;
        speed.Multiplier.Value = GameSpeed.Value;
        harvesting.Multiplier.Value = Math.Clamp(HarvestSpeed.Value, 1, 4);
        foreach (var feature in host.Registry.Snapshot().Where(f => !f.Id.StartsWith("ui.", StringComparison.Ordinal)))
            enabled[feature.Id] = config.Bind("Enabled", feature.Id, false, "Used only when RestoreEnabledStatesOnLaunch is enabled.");
    }
    public void TryRestore(SinglePlayerGuard guard)
    {
        if (restored || !guard.Refresh().Allowed) return;
        restored = true;
        if (!RestoreStates.Value) return;
        foreach (var pair in enabled) if (pair.Value.Value) host.Registry.Find(pair.Key)?.TryEnable();
    }
    public void Remember(ITrainerFeature feature)
    {
        if (enabled.TryGetValue(feature.Id, out var entry)) entry.Value = feature.State == FeatureState.Enabled;
    }
    public void ResetAll()
    {
        var gameplay = host.Registry.Snapshot().Where(f => !f.Id.StartsWith("ui.", StringComparison.Ordinal)
            || f is HostedFeature { HasPendingCleanup: true }).ToArray();
        FeatureReset.ResetAll(gameplay, movement.Multiplier, speed.Multiplier, harvesting.Multiplier);
        foreach (var feature in gameplay) Remember(feature);
        Movement.Value = 1;
        GameSpeed.Value = 1;
        HarvestSpeed.Value = 1;
        restored = true;
        TransientStateCleared?.Invoke();
        host.Trace("Reset All completed; inspect diagnostics for any remaining native cleanup failure.");
    }

    public void Reload()
    {
        host.DisableAll();
        TransientStateCleared?.Invoke();
        if (host.HasPendingCleanup) throw new InvalidOperationException("Configuration was not reloaded because native cleanup is still pending. Use Reset All and inspect Diagnostics.");
        // Do not save remembered disabled flags before reloading: that would overwrite the user's file edits.
        restored = true;
        config.Reload();
        movement.Multiplier.Value = Movement.Value;
        speed.Multiplier.Value = GameSpeed.Value;
        harvesting.Multiplier.Value = Math.Clamp(HarvestSpeed.Value, 1, 4);
        host.Verbosity = Verbosity.Value;
        host.Trace("Configuration reloaded. Gameplay features remain off.");
    }

    public void Rescan()
    {
        restored = true;
        host.RescanCompatibility();
        TransientStateCleared?.Invoke();
    }
}
