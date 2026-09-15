using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HutchASKA.Plugin.Infrastructure;
using HutchASKA.Plugin.UI;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Player;
using HutchASKA.Plugin.World;
using HutchASKA.Plugin.Configuration;
using HutchASKA.Plugin.Input;
using HutchASKA.Plugin.Items;
using HutchASKA.Plugin.Crafting;
using HutchASKA.Plugin.Tribe;
using HutchASKA.Core.Tribe;
using UnityEngine;

namespace HutchASKA.Plugin;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BasePlugin
{
    internal IPlayerContext Players { get; } = new AskaPlayerContext();
    internal IWorldContext World { get; } = new AskaWorldContext();
    public const string PluginGuid = "com.jfhutchi.hutchaska";
    public const string PluginName = "HutchASKA";
    public const string PluginVersion = "0.1.2";

    public override void Load()
    {
        GameObjectResolver.Initialize(message => Log.LogWarning(message));
        var guard = new SinglePlayerGuard(error => Log.LogError($"Session discovery: {error}"));
        var host = new FeatureHost(guard, Log);
        var god = new GodModeFeature(Players);
        god.Hosted = host.Register(god);
        var stamina = new InfiniteStaminaFeature(Players);
        stamina.Hosted = host.Register(stamina);
        host.Register(new SurvivalFeatureSet(Players, true));
        host.Register(new SurvivalFeatureSet(Players, false));
        host.Register(new TemperatureImmunityFeature());
        var movement = new MovementSpeedFeature(Players);
        host.Register(movement);
        host.Register(new WorldTimeFeature(World));
        host.Register(new TimeStepFeature());
        var gameSpeed = new GameSpeedFeature();
        host.Register(gameSpeed);
        var menuInput = host.Register(new MenuInputFeature());
        host.Register(new InfiniteDurabilityFeature());
        host.Register(new NoSpoilageFeature());
        host.Register(new RetainItemsOnUseFeature());
        var catalog = new AskaItemCatalog();
        catalog.Hosted = host.Register(catalog);
        var give = new GiveItemFeature(new InventoryService(Players, guard));
        give.Hosted = host.Register(give);
        host.Register(new FreeCraftingFeature());
        host.Register(new FreeBuildingFeature());
        host.Register(new FreeRepairsFeature());
        var tribe = new AskaTribeContext(Players, guard, error => Log.LogError($"Tribe discovery: {error}"));
        var tribeGod = new VillagerGodModeFeature(tribe);
        tribeGod.Hosted = host.Register(tribeGod);
        host.Register(new TribeNeedsFeature(tribe, "tribe.food", "No Hunger (Tribe)", new(FoodFraction: 1)));
        host.Register(new TribeNeedsFeature(tribe, "tribe.water", "No Thirst (Tribe)", new(WaterFraction: 1)));
        host.Register(new TribeNeedsFeature(tribe, "tribe.energy", "Infinite Energy (Tribe)", new(EnergyFraction: 1)));
        host.Register(new TribeNeedsFeature(tribe, "tribe.rest", "Full Rest (Tribe)", new(RestFraction: 1)));
        host.Register(new TribeNeedsFeature(tribe, "tribe.happiness", "Max Happiness (Tribe)", new(HappinessFraction: 1)));
        host.Register(new TribeUnavailableFeature("tribe.temperature", "Temperature Immunity (Tribe)", AskaTribeContext.WarmthUnavailable));
        host.Register(new TribeUnavailableFeature("tribe.aging", "Freeze Aging", AskaTribeContext.AgeUnavailable));
        host.Register(new InstantRecruitmentFeature());
        var healTribe = new TribeRestoreFeature(tribe, true);
        healTribe.Hosted = host.Register(healTribe);
        var restoreTribe = new TribeRestoreFeature(tribe, false);
        restoreTribe.Hosted = host.Register(restoreTribe);
        var editor = new VillagerEditorService(tribe);
        editor.Hosted = host.Register(editor);
        var config = new RuntimeConfiguration(Config, host, movement, gameSpeed);
        var hotkeys = new HotkeyManager(Config, host, guard, config);
        var bepinexAssembly = typeof(BasePlugin).Assembly;
        var bepinexVersion = bepinexAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? bepinexAssembly.GetName().Version?.ToString() ?? "Unavailable";
        var versions = new RuntimeVersions(Application.version, Application.unityVersion, bepinexVersion,
            SteamBuildReader.Detect(error => Log.LogWarning($"Steam build detection: {error}")));
        var diagnostics = new DiagnosticsService(host, guard, config, versions, () => tribe.LastError, Log);
        var window = new TrainerWindow(host, guard, versions, config, movement, gameSpeed, catalog, give, editor, healTribe, restoreTribe, diagnostics,
            error => Log.LogError($"Trainer rendering failed; the menu is disabled until restart. Cursor and menu input will be released. {error}"));
        // BepInEx registers the IL2CPP type and attaches it to its persistent manager object.
        AddComponent<TrainerBehaviour>().Initialize(host, window, hotkeys, config, guard, menuInput);
        Log.LogInfo($"{PluginName} {PluginVersion}; ASKA application {versions.Game}; Steam build {versions.SteamBuild}; Unity {versions.Unity}; BepInEx {versions.BepInEx}");
        Log.LogInfo($"Session: {guard.Mode}; {guard.Decision.Reason}. Gameplay features default off.");
    }
}
