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
using UnityEngine;

namespace HutchASKA.Plugin;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BasePlugin
{
    internal IPlayerContext Players { get; } = new AskaPlayerContext();
    internal IWorldContext World { get; } = new AskaWorldContext();
    public const string PluginGuid = "com.jfhutchi.hutchaska";
    public const string PluginName = "HutchASKA";
    public const string PluginVersion = "0.1.0";

    public override void Load()
    {
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
        var config = new RuntimeConfiguration(Config, host, movement, gameSpeed);
        var hotkeys = new HotkeyManager(Config, host, guard, config);
        var bepinexAssembly = typeof(BasePlugin).Assembly;
        var bepinexVersion = bepinexAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? bepinexAssembly.GetName().Version?.ToString() ?? "Unavailable";
        var versions = new RuntimeVersions(Application.version, Application.unityVersion, bepinexVersion);
        var window = new TrainerWindow(host, guard, versions, config, movement, gameSpeed);
        // BepInEx registers the IL2CPP type and attaches it to its persistent manager object.
        AddComponent<TrainerBehaviour>().Initialize(host, window, hotkeys, config, guard, menuInput);
        Log.LogInfo($"{PluginName} {PluginVersion}; ASKA {versions.Game}; Unity {versions.Unity}; BepInEx {versions.BepInEx}");
        Log.LogInfo($"Session: {guard.Mode}; {guard.Decision.Reason}. Gameplay features default off.");
    }
}
