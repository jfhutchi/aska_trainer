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
    public const string PluginVersion = "0.1.8";

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
        var temperature = new TemperatureImmunityFeature(Players);
        temperature.Hosted = host.Register(temperature);
        var movement = new MovementSpeedFeature(Players);
        movement.Hosted = host.Register(movement);
        var harvesting = new HarvestSpeedFeature(Players);
        harvesting.Hosted = host.Register(harvesting);
        var fishing = new FishingAssistFeature(Players);
        fishing.Hosted = host.Register(fishing);
        var mushrooms = new MushroomRegrowthFeature();
        mushrooms.Hosted = host.Register(mushrooms);
        var buildSpeed = new BuildSpeedFeature(Players);
        buildSpeed.Hosted = host.Register(buildSpeed);
        var terrain = new TerrainLevelingFeature();
        terrain.Hosted = host.Register(terrain);
        host.Register(new WorldTimeFeature(World));
        var fuel = new InfiniteFuelFeature();
        fuel.Hosted = host.Register(fuel);
        var timeStep = new TimeStepFeature(World);
        timeStep.Hosted = host.Register(timeStep);
        var gameSpeed = new GameSpeedFeature();
        host.Register(gameSpeed);
        var menuInput = host.Register(new MenuInputFeature());
        var durability = new InfiniteDurabilityFeature();
        durability.Hosted = host.Register(durability);
        var spoilage = new NoSpoilageFeature();
        spoilage.Hosted = host.Register(spoilage);
        var retainItems = new RetainItemsOnUseFeature(Players);
        retainItems.Hosted = host.Register(retainItems);
        var catalog = new AskaItemCatalog();
        catalog.Hosted = host.Register(catalog);
        var give = new GiveItemFeature(new InventoryService(Players, guard));
        give.Hosted = host.Register(give);
        var crafting = new FreeCraftingFeature(Players);
        crafting.Hosted = host.Register(crafting);
        var building = new FreeBuildingFeature(guard);
        building.Hosted = host.Register(building);
        var repairs = new FreeRepairsFeature();
        repairs.Hosted = host.Register(repairs);
        var tribe = new AskaTribeContext(Players, guard, error => Log.LogError($"Tribe discovery: {error}"));
        var needs = new TribeNeedsCoordinator(tribe);
        var tribeGod = new VillagerGodModeFeature(tribe);
        tribeGod.Hosted = host.Register(tribeGod);
        host.Register(new TribeNeedsFeature(needs, "tribe.food", "No Hunger (Tribe)", new(FoodFraction: 1)));
        host.Register(new TribeNeedsFeature(needs, "tribe.water", "No Thirst (Tribe)", new(WaterFraction: 1)));
        host.Register(new TribeNeedsFeature(needs, "tribe.energy", "Infinite Energy (Tribe)", new(EnergyFraction: 1)));
        host.Register(new TribeNeedsFeature(needs, "tribe.rest", "Full Rest (Tribe)", new(RestFraction: 1)));
        host.Register(new TribeNeedsFeature(needs, "tribe.happiness", "Max Happiness (Tribe)", new(HappinessFraction: 1)));
        var tribeTemperature = new TribeTemperatureFeature(tribe);
        tribeTemperature.Hosted = host.Register(tribeTemperature);
        host.Register(new FreezeAgingFeature());
        var recruitment = new InstantRecruitmentFeature(World);
        recruitment.Hosted = host.Register(recruitment);
        var reroll = new RecruitRerollFeature(guard);
        reroll.Hosted = host.Register(reroll);
        var healTribe = new TribeRestoreFeature(tribe, true);
        healTribe.Hosted = host.Register(healTribe);
        var restoreTribe = new TribeRestoreFeature(tribe, false);
        restoreTribe.Hosted = host.Register(restoreTribe);
        var editor = new VillagerEditorService(tribe);
        editor.Hosted = host.Register(editor);
        var config = new RuntimeConfiguration(Config, host, movement, gameSpeed, harvesting, buildSpeed, terrain, fishing, mushrooms);
        var hotkeys = new HotkeyManager(Config, host, guard, config);
        var bepinexAssembly = typeof(BasePlugin).Assembly;
        var bepinexVersion = bepinexAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? bepinexAssembly.GetName().Version?.ToString() ?? "Unavailable";
        var versions = new RuntimeVersions(Application.version, Application.unityVersion, bepinexVersion,
            SteamBuildReader.Detect(error => Log.LogWarning($"Steam build detection: {error}")));
        var diagnostics = new DiagnosticsService(host, guard, config, versions, () => tribe.LastError, Log);
        var window = new TrainerWindow(host, guard, versions, config, movement, gameSpeed, timeStep, harvesting, buildSpeed, terrain, fishing, mushrooms, catalog, give, editor, healTribe, restoreTribe, reroll, diagnostics,
            error => Log.LogError($"Trainer rendering failed; the menu is disabled until restart. Cursor and menu input will be released. {error}"));
        // BepInEx registers the IL2CPP type and attaches it to its persistent manager object.
        AddComponent<TrainerBehaviour>().Initialize(host, window, hotkeys, config, guard, menuInput);
        Log.LogInfo($"{PluginName} {PluginVersion}; ASKA application {versions.Game}; Steam build {versions.SteamBuild}; Unity {versions.Unity}; BepInEx {versions.BepInEx}");
        Log.LogInfo($"Session: {guard.Mode}; {guard.Decision.Reason}. Gameplay features default off.");
    }
}
