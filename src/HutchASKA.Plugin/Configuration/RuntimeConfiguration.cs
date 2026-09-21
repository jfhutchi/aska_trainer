using BepInEx.Configuration;
using HutchASKA.Core.Features;
using HutchASKA.Core.Player;
using HutchASKA.Plugin.Infrastructure;
using HutchASKA.Plugin.Player;
using HutchASKA.Plugin.World;
using HutchASKA.Plugin.Tribe;

namespace HutchASKA.Plugin.Configuration;

internal sealed class RuntimeConfiguration
{
    private readonly FeatureHost host;
    private readonly ConfigFile config;
    private readonly MovementSpeedFeature movement;
    private readonly GameSpeedFeature speed;
    private readonly HarvestSpeedFeature harvesting;
    private readonly BuildSpeedFeature building;
    private readonly TerrainLevelingFeature terrain;
    private readonly FishingAssistFeature fishing;
    private readonly MushroomRegrowthFeature mushrooms;
    private readonly SkillGainFeature playerSkills, tribeSkills;
    private readonly TribeBuildSpeedFeature tribeBuilding;
    private readonly TribeHarvestSpeedFeature tribeHarvesting;
    private readonly TribeMovementSpeedFeature tribeMovement;
    private readonly Dictionary<string, ConfigEntry<bool>> enabled = new();
    private bool restored;
    public ConfigEntry<bool> RestoreStates { get; }
    public ConfigEntry<float> Movement { get; }
    public ConfigEntry<float> GameSpeed { get; }
    public ConfigEntry<int> HarvestSpeed { get; }
    public ConfigEntry<int> BuildSpeed { get; }
    public ConfigEntry<int> TerrainSize { get; }
    public ConfigEntry<int> FishingBiteSpeed { get; }
    public ConfigEntry<int> FishingRareWeight { get; }
    public ConfigEntry<bool> FishingEasyCatch { get; }
    public ConfigEntry<int> MushroomRegrowth { get; }
    public ConfigEntry<int> PlayerSkillGain { get; }
    public ConfigEntry<int> TribeSkillGain { get; }
    public ConfigEntry<int> TribeBuildSpeed { get; }
    public ConfigEntry<int> TribeHarvestSpeed { get; }
    public ConfigEntry<int> TribeMovementSpeed { get; }
    public ConfigEntry<DiagnosticVerbosity> Verbosity { get; }
    public event Action? TransientStateCleared;
    public RuntimeConfiguration(ConfigFile config, FeatureHost host, MovementSpeedFeature movement, GameSpeedFeature speed, HarvestSpeedFeature harvesting, BuildSpeedFeature building, TerrainLevelingFeature terrain,
        FishingAssistFeature fishing, MushroomRegrowthFeature mushrooms, SkillGainFeature playerSkills,
        SkillGainFeature tribeSkills, TribeBuildSpeedFeature tribeBuilding, TribeHarvestSpeedFeature tribeHarvesting,
        TribeMovementSpeedFeature tribeMovement)
    {
        this.host = host;
        this.config = config;
        this.movement = movement;
        this.speed = speed;
        this.harvesting = harvesting;
        this.building = building;
        this.terrain = terrain;
        this.fishing = fishing;
        this.mushrooms = mushrooms;
        this.playerSkills = playerSkills;
        this.tribeSkills = tribeSkills;
        this.tribeBuilding = tribeBuilding;
        this.tribeHarvesting = tribeHarvesting;
        this.tribeMovement = tribeMovement;
        RestoreStates = config.Bind("General", "RestoreEnabledStatesOnLaunch", false, "Opt in to restoring previously selected cheats after single-player confirmation.");
        Movement = config.Bind("Player", "MovementMultiplier", 1f, "Movement multiplier, 1 to 5.");
        GameSpeed = config.Bind("World", "GameSpeedMultiplier", 1f, "Game speed multiplier, 0.5 to 5.");
        HarvestSpeed = config.Bind("Player", "HarvestSpeedMultiplier", 1, "Player harvesting speed preset: 1 (normal), 2, 3 or 4. Does not change global game speed.");
        BuildSpeed = config.Bind("Crafting", "BuildSpeedMultiplier", 1, "Construction work per player hammer stroke: 1 (normal), 2, 3 or 4. Material requirements are separate.");
        TerrainSize = config.Bind("Crafting", "TerrainLevelingSize", 5, "Maximum terrain leveling plan side in grid tiles: 5 to 20. Applies to new leveling plans.");
        FishingBiteSpeed = config.Bind("Fishing", "BiteSpeed", 1, "Bite wait speed: 1 (normal), 2 or 4.");
        FishingRareWeight = config.Bind("Fishing", "RareFishWeight", 1, "Eligible rare-fish selection weight: 1 (normal), 20, 30, 40 or 50. Other values, including legacy 2 and 4, reset to normal. Not a guaranteed catch percentage.");
        FishingEasyCatch = config.Bind("Fishing", "EasyCatch", false, "Longer reaction time and improved valid catches while Fishing Assists is enabled.");
        MushroomRegrowth = config.Bind("Foraging", "MushroomRegrowth", 1, "Unavailable pending autosave-safe timers. Forced to 1 (normal).");
        PlayerSkillGain = config.Bind("Player", "SkillGainMultiplier", 1, "Player earned skill experience: 1 (normal) to 5. Native caps remain.");
        TribeSkillGain = config.Bind("Tribe", "SkillGainMultiplier", 1, "Owned villagers' earned skill experience: 1 (normal) to 5. Native caps remain.");
        TribeBuildSpeed = config.Bind("Tribe", "BuildSpeedMultiplier", 1, "Owned villagers' construction work: 1 (normal) to 5.");
        TribeHarvestSpeed = config.Bind("Tribe", "HarvestSpeedMultiplier", 1, "Owned villagers' gathering and tool-harvesting work: 1 (normal) to 5.");
        TribeMovementSpeed = config.Bind("Tribe", "MovementSpeedMultiplier", 1, "Owned villagers' ordinary ground movement: 1 (normal) to 5. Special traversal remains native.");
        Verbosity = config.Bind("Diagnostics", "LoggingVerbosity", DiagnosticVerbosity.Normal, "Trainer diagnostics only: ErrorsOnly, Normal or Verbose. Errors are always logged.");
        Verbosity.SettingChanged += (_, _) => host.Verbosity = Verbosity.Value;
        host.Verbosity = Verbosity.Value;
        movement.Multiplier.Value = Movement.Value;
        speed.Multiplier.Value = GameSpeed.Value;
        harvesting.Multiplier.Value = Math.Clamp(HarvestSpeed.Value, 1, 4);
        building.Multiplier.Value = Math.Clamp(BuildSpeed.Value, 1, 4);
        terrain.Size.Value = Math.Clamp(TerrainSize.Value, 5, 20);
        LoadAssistSettings();
        // Weather selections are session-only, even when other cheats are restored on launch.
        foreach (var feature in host.Registry.Snapshot().Where(f => !f.Id.StartsWith("ui.", StringComparison.Ordinal) && f.Id != "world.weather"))
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
        FeatureReset.ResetAll(gameplay, movement.Multiplier, speed.Multiplier, harvesting.Multiplier, building.Multiplier,
            fishing.BiteSpeed, fishing.RareWeight, mushrooms.Multiplier, playerSkills.Multiplier,
            tribeSkills.Multiplier, tribeBuilding.Multiplier, tribeHarvesting.Multiplier, tribeMovement.Multiplier);
        foreach (var feature in gameplay) Remember(feature);
        Movement.Value = 1;
        GameSpeed.Value = 1;
        HarvestSpeed.Value = 1;
        BuildSpeed.Value = 1;
        terrain.Size.Value = 5;
        TerrainSize.Value = 5;
        FishingBiteSpeed.Value = 1;
        FishingRareWeight.Value = 1;
        FishingEasyCatch.Value = false;
        fishing.EasyCatch = false;
        MushroomRegrowth.Value = 1;
        PlayerSkillGain.Value = TribeSkillGain.Value = TribeBuildSpeed.Value = TribeHarvestSpeed.Value = 1;
        TribeMovementSpeed.Value = 1;
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
        building.Multiplier.Value = Math.Clamp(BuildSpeed.Value, 1, 4);
        terrain.Size.Value = Math.Clamp(TerrainSize.Value, 5, 20);
        LoadAssistSettings();
        host.Verbosity = Verbosity.Value;
        host.Trace("Configuration reloaded. Gameplay features remain off.");
    }

    public void Rescan()
    {
        restored = true;
        host.RescanCompatibility();
        TransientStateCleared?.Invoke();
    }

    private void LoadAssistSettings()
    {
        fishing.BiteSpeed.Value = AssistPreset(FishingBiteSpeed.Value);
        FishingRareWeight.Value = FishingAssistMath.NormalizeRareWeightPreset(FishingRareWeight.Value);
        fishing.RareWeight.Value = FishingRareWeight.Value;
        fishing.EasyCatch = FishingEasyCatch.Value;
        mushrooms.Multiplier.Reset();
        MushroomRegrowth.Value = 1;
        playerSkills.Multiplier.Value = Math.Clamp(PlayerSkillGain.Value, 1, 5);
        tribeSkills.Multiplier.Value = Math.Clamp(TribeSkillGain.Value, 1, 5);
        tribeBuilding.Multiplier.Value = Math.Clamp(TribeBuildSpeed.Value, 1, 5);
        tribeHarvesting.Multiplier.Value = Math.Clamp(TribeHarvestSpeed.Value, 1, 5);
        tribeMovement.Multiplier.Value = Math.Clamp(TribeMovementSpeed.Value, 1, 5);
    }

    private static int AssistPreset(int value) => value >= 4 ? 4 : value >= 2 ? 2 : 1;
}
