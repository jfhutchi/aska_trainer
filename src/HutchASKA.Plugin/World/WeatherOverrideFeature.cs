using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SSSGame.Weather;

namespace HutchASKA.Plugin.World;

internal enum WeatherChoice { Normal, Clear, Rain, Fog, Overcast, Blizzard }

internal sealed class WeatherOverrideFeature(IWorldContext world) : NativeFeature("world.weather", "Weather")
{
    private enum ApplicationState { Waiting, Applied, SpecialWeather, OtherOverride }
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".weather");
    private static readonly BepInEx.Logging.ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("HutchASKA.Weather");
    private static WeatherOverrideFeature? instance;
    private WeatherSystem? owner;
    private WeatherSystem.Forecast? forecast;
    private ProgressingWeatherConditionConfig? selectedConfig;
    private readonly Dictionary<WeatherChoice, ProgressingWeatherConditionConfig> available = new();
    private (WeatherChoice Choice, ApplicationState State)? published;
    public WeatherChoice Choice { get; private set; }
    public static string Label(WeatherChoice choice) => choice switch
    {
        WeatherChoice.Normal => "Normal Forecast",
        WeatherChoice.Clear => "Clear Skies",
        _ => choice.ToString()
    };

    private static System.Reflection.MethodInfo? ForecastMethod() => AccessTools.DeclaredMethod(
        typeof(WeatherSystem.Forecast), "GetWeatherConditions", new[] { typeof(int), typeof(float) });

    public override CompatibilityResult ProbeCompatibility() => ForecastMethod()?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(WeatherSystem.Forecast), "GetWeatherConditionDebug",
            new[] { typeof(ProgressingWeatherConditionConfig), typeof(float), typeof(float) })?.ReturnType == typeof(void)
        && typeof(WeatherSystem).GetProperty("MustUpdateNextFrame") is { CanRead: true, CanWrite: true, PropertyType: var updateType }
        && updateType == typeof(int)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("The verified temporary weather-selection API is unavailable.");

    public override bool TryEnable()
    {
        if (!world.TryGetWeatherSystem(out var weather) || !Ready(weather!))
        {
            StatusReason = "Weather is still loading. Try again once the world is ready.";
            return false;
        }
        owner = weather;
        forecast = weather!.GetWeatherForecast();
        if (forecast is null)
        {
            owner = null;
            StatusReason = "The current world's forecast is unavailable.";
            return false;
        }
        ReadSeasonChoices(weather);
        instance = this;
        harmony.Patch(ForecastMethod(), postfix: new HarmonyMethod(typeof(WeatherOverrideFeature), nameof(ForecastPostfix)));
        return base.TryEnable();
    }

    public bool TrySelect(WeatherChoice choice, out string? error)
    {
        var hosted = Hosted ?? throw new InvalidOperationException("Weather is not registered.");
        if (choice == WeatherChoice.Normal)
        {
            hosted.Disable();
            error = hosted.HasPendingCleanup ? hosted.StatusReason : null;
            return error is null;
        }
        var wasEnabled = hosted.State == FeatureState.Enabled;
        if (!hosted.TryEnable()) { error = hosted.StatusReason ?? "Weather is unavailable."; return false; }
        string? failure = null;
        var ran = hosted.TryExecute(() =>
        {
            if (!TryUseCurrentWorld())
            {
                failure = StatusReason;
                return;
            }
            if (owner!.debugWeatherEvent || owner.ignoreNormalForecastConditions)
            {
                failure = "Another weather override is active. Return it to normal before choosing weather here.";
                return;
            }
            ProgressingWeatherConditionConfig? config = null;
            if (choice != WeatherChoice.Clear && (!available.TryGetValue(choice, out config) || !config))
            {
                failure = $"{Label(choice)} is not available in this world's loaded weather settings.";
                return;
            }
            selectedConfig = config;
            Choice = choice;
            RequestRefresh();
            Publish(ApplicationState.Waiting);
        });
        error = ran ? failure : hosted.StatusReason ?? "Weather selection failed.";
        if (error is not null && !wasEnabled) hosted.Disable();
        return error is null;
    }

    private static bool Ready(WeatherSystem weather) => weather && weather._initDone && weather._firstInitDone
        && weather.Object != null && weather.Object.IsValid && weather.Object.HasStateAuthority
        && weather.session != null && weather.session.isMaster;

    private bool TryUseCurrentWorld()
    {
        if (owner && world.TryGetWeatherSystem(out var current) && current == owner && Ready(current!)
            && current!.GetWeatherForecast()?.Pointer == forecast?.Pointer) return true;
        // Scene/forecast changes are ordinary lifecycle transitions, not circuit-breaker faults.
        Hosted?.Disable();
        StatusReason = "Weather returned to normal because the world changed or is loading. Select again once ready.";
        return false;
    }

    private void ReadSeasonChoices(WeatherSystem weather)
    {
        available.Clear();
        var seasons = weather.seasons;
        if (seasons is null) return;
        for (var i = 0; i < seasons.Count; i++)
        {
            var events = seasons[i]?.weatherEvents;
            if (events is null) continue;
            for (var j = 0; j < events.Length; j++)
            {
                var config = events[j]?.overrideConfig;
                if (!config) continue;
                // Exact ordinary weather assets; never expose invasion/boss/super-season conditions.
                var choice = config!.name switch
                {
                    "WeatherEvent_HeavyRain" => WeatherChoice.Rain,
                    "WeatherEvent_Fog" => WeatherChoice.Fog,
                    "WeatherEvent_Overcast" => WeatherChoice.Overcast,
                    "WeatherEvent_Blizzard" => WeatherChoice.Blizzard,
                    _ => WeatherChoice.Normal
                };
                if (choice != WeatherChoice.Normal) available.TryAdd(choice, config);
            }
        }
    }

    private static bool OrdinaryWeather(string name) => name is "WeatherEvent_HeavyRain" or "WeatherEvent_Fog"
        or "WeatherEvent_Overcast" or "WeatherEvent_Blizzard" or "WeatherEvent_SpringClouds" or "WeatherEvent_AutumnClouds";

    private static void ForecastPostfix(WeatherSystem.Forecast __instance)
    {
        var feature = instance;
        if (feature is null || feature.Choice == WeatherChoice.Normal || __instance.Pointer != feature.forecast?.Pointer) return;
        feature.Hosted?.TryExecute(() =>
        {
            if (!feature.TryUseCurrentWorld()) return;
            if (feature.owner!.debugWeatherEvent || feature.owner.ignoreNormalForecastConditions)
            {
                feature.Publish(ApplicationState.OtherOverride);
                return;
            }
            var conditions = feature.owner!._weatherConditionsOverrides;
            if (conditions is null) throw new InvalidOperationException("The current weather sample is unavailable.");
            for (var i = 0; i < conditions.Count; i++)
            {
                var config = conditions[i].config;
                if (config && !OrdinaryWeather(config.name))
                {
                    feature.Publish(ApplicationState.SpecialWeather);
                    return;
                }
            }
            if (feature.Choice != WeatherChoice.Clear && !feature.selectedConfig)
                throw new InvalidOperationException("The selected weather configuration was unloaded.");
            // Replace only this update's calculated conditions. Native forecast generation still runs.
            __instance.GetWeatherConditionDebug(feature.selectedConfig!, 1f, .5f);
            feature.Publish(ApplicationState.Applied);
        });
    }

    private void RequestRefresh()
    {
        if (owner && owner!._initDone) owner.MustUpdateNextFrame = 1;
    }

    public override void Tick()
    {
        if (!TryUseCurrentWorld()) return;
        if (owner!.debugWeatherEvent || owner.ignoreNormalForecastConditions)
            Publish(ApplicationState.OtherOverride);
    }

    private void Publish(ApplicationState state)
    {
        var next = (Choice, state);
        if (published == next) return;
        published = next;
        StatusReason = state switch
        {
            ApplicationState.Waiting => $"{Label(Choice)} selected; waiting for the next weather update.",
            ApplicationState.Applied => $"{Label(Choice)} applied. Normal Forecast restores automatic weather.",
            ApplicationState.SpecialWeather => $"{Label(Choice)} paused while special weather is active. Normal event weather remains in control.",
            _ => $"{Label(Choice)} paused while another weather override is active."
        };
        Log.LogInfo(StatusReason);
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        instance = null;
        Choice = WeatherChoice.Normal;
        selectedConfig = null;
        available.Clear();
        published = null;
        // The normal native sample replaces the transient selection, including when time is frozen.
        RequestRefresh();
        owner = null;
        forecast = null;
        StatusReason = null;
        base.Disable();
    }
}
