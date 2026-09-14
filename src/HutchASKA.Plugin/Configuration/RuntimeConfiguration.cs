using BepInEx.Configuration;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;
using HutchASKA.Plugin.Player;
using HutchASKA.Plugin.World;

namespace HutchASKA.Plugin.Configuration;

internal sealed class RuntimeConfiguration
{
    private readonly FeatureHost host;
    private readonly Dictionary<string, ConfigEntry<bool>> enabled = new();
    private bool restored;
    public ConfigEntry<bool> RestoreStates { get; }
    public ConfigEntry<float> Movement { get; }
    public ConfigEntry<float> GameSpeed { get; }
    public RuntimeConfiguration(ConfigFile config, FeatureHost host, MovementSpeedFeature movement, GameSpeedFeature speed)
    {
        this.host = host;
        RestoreStates = config.Bind("General", "RestoreEnabledStatesOnLaunch", false, "Opt in to restoring previously selected cheats after single-player confirmation.");
        Movement = config.Bind("Player", "MovementMultiplier", 1f, "Movement multiplier, 1 to 5.");
        GameSpeed = config.Bind("World", "GameSpeedMultiplier", 1f, "Game speed multiplier, 0.5 to 5.");
        movement.Multiplier.Value = Movement.Value;
        speed.Multiplier.Value = GameSpeed.Value;
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
        foreach (var feature in host.Registry.Snapshot().Where(f => !f.Id.StartsWith("ui.", StringComparison.Ordinal)))
        {
            feature.Reset();
            Remember(feature);
        }
        Movement.Value = 1;
        GameSpeed.Value = 1;
    }
}
