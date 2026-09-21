using BepInEx.Logging;
using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.World;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Inventory;
using SSSGame;
using SSSGame.Weather;

namespace HutchASKA.Plugin.World;

internal sealed class MushroomRegrowthFeature() : NativeFeature("world.mushrooms", "Mushroom Regrowth")
{
    private const int MaximumResources = 512;
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".mushrooms");
    private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("HutchASKA.Mushrooms");
    private static MushroomRegrowthFeature? instance;
    private readonly Dictionary<int, OwnedSchedule> schedules = new();
    private readonly List<ItemInfo> resources = new(3);
    private int resourceTableCount = -1;
    private WeatherManager? manager;
    private long clockIdentity;
    private int reports;
    public MultiplierSetting Multiplier { get; } = new(1, 4);

    private sealed record OwnedSchedule(ItemInfo Resource, BiomeItemAvailabilityData Data,
        MushroomScheduleLease Lease, float Multiplier);

    public override CompatibilityResult ProbeCompatibility() =>
        AccessTools.DeclaredMethod(typeof(WeatherManager), "HandleDescriptors", Type.EmptyTypes)?.ReturnType == typeof(void)
        && typeof(WeatherManager).GetProperty("_descriptors") is { CanRead: true }
        && typeof(WeatherManager).GetProperty("LastEventData")?.PropertyType == typeof(WeatherEventData)
        && typeof(WeatherManager).GetProperty("_initialized")?.PropertyType == typeof(bool)
        && typeof(BiomeItemAvailabilityData).GetProperty("NextReplenishDate") is { CanRead: true, CanWrite: true }
        && typeof(BiomeItemAvailabilityData).GetProperty("NextReplenishDirty")?.PropertyType == typeof(bool)
        && typeof(BiomeItemAvailabilityData).GetProperty("CurrentDescriptorsState")?.PropertyType == typeof(bool)
        && AccessTools.DeclaredMethod(typeof(AvailabilityProcess), "CheckAll", new[] { typeof(WeatherEventData) })?.ReturnType == typeof(bool)
        && AccessTools.DeclaredMethod(typeof(BiomeResourceInfo), "GetReplenishData", Type.EmptyTypes)?.ReturnType == typeof(ReplenishData)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Verified mushroom weather/replenishment scheduling API is unavailable.");

    public override bool TryEnable()
    {
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(WeatherManager), "HandleDescriptors", Type.EmptyTypes),
            prefix: new HarmonyMethod(typeof(MushroomRegrowthFeature), nameof(BeforeWeatherDispatch)));
        StatusReason = "Rain/season eligibility stays native. Default mushrooms: 2x every game day; 4x every half day while eligible.";
        return base.TryEnable();
    }

    private static void BeforeWeatherDispatch(WeatherManager __instance) =>
        instance?.Hosted?.TryExecute(() => instance.Prepare(__instance));

    private static bool IsReady(WeatherManager candidate, WeatherSystem? weather) =>
        candidate != null && candidate.gameObject.activeInHierarchy && candidate._initialized
        && candidate.LastEventData is not null && candidate._descriptors is not null
        && weather != null && weather.gameObject.activeInHierarchy && weather._initDone
        && weather.Object != null && weather.Object.IsValid && weather.Object.HasStateAuthority
        && weather.session != null && weather.session.isMaster;

    private static bool Matches(ItemInfo resource, BiomeItemAvailabilityData data)
    {
        if (resource == null || !MushroomRegrowthPolicy.IsMushroom(resource.id, resource.name)
            || resource.TryCast<BiomeResourceInfo>() is null || data is null
            || data.itemDescriptors is null || data.itemDescriptors.Count is < 1 or > 32) return false;
        for (var i = 0; i < data.itemDescriptors.Count; i++)
        {
            var descriptor = data.itemDescriptors[i];
            if (descriptor is null || descriptor.itemInfo == null || descriptor.itemInfo.Pointer != resource.Pointer) return false;
        }
        return true;
    }

    private void Prepare(WeatherManager current)
    {
        var weather = WeatherSystem.Instance;
        if (!IsReady(current, weather)) return;
        var clock = weather!.Pointer.ToInt64();
        if (manager == null || manager.Pointer != current.Pointer || clockIdentity != clock)
        {
            schedules.Clear();
            resources.Clear();
            resourceTableCount = -1;
            manager = current;
            clockIdentity = clock;
        }
        if (current._descriptors.Count > MaximumResources)
            throw new InvalidOperationException("Mushroom discovery exceeded the verified resource-table bound.");

        if (resourceTableCount != current._descriptors.Count) Discover(current);
        foreach (var resource in resources)
        {
            if (!current._descriptors.TryGetValue(resource, out var data) || !Matches(resource, data))
            {
                schedules.Remove(resource.id);
                resourceTableCount = -1;
                continue;
            }
            var eligible = data.CurrentDescriptorsState && data.availabilityProcess != null
                && data.availabilityProcess.CheckAll(current.LastEventData);
            var multiplier = Multiplier.Value;
            if (schedules.TryGetValue(resource.id, out var previous))
            {
                var owned = previous.Lease.Owns(current.Pointer.ToInt64(), clock, resource.Pointer.ToInt64(),
                    data.Pointer.ToInt64(), data.NextReplenishDate, data.NextReplenishDirty);
                if (owned && eligible && previous.Multiplier == multiplier) continue;
                if (owned) data.NextReplenishDate = previous.Lease.NativeDue;
                schedules.Remove(resource.id);
            }
            // The native dispatcher recalculates dirty dates itself. After consuming a due
            // date it sets dirty, so the following weather update starts a fresh interval.
            if (!eligible || data.NextReplenishDirty) continue;
            var replenish = resource.Cast<BiomeResourceInfo>().GetReplenishData();
            if (replenish is null) continue;
            var nativeDue = data.NextReplenishDate;
            var target = MushroomRegrowthPolicy.Target(weather.NetworkedCurrentGameTime, nativeDue,
                replenish.replenishFrequencyDays, replenish.replenishWhenAvailable,
                replenish.replenishSeasonStart is { Count: > 0 }, multiplier);
            if (target is not { } due) continue;
            var lease = new MushroomScheduleLease(current.Pointer.ToInt64(), clock, resource.Pointer.ToInt64(),
                data.Pointer.ToInt64(), nativeDue, due);
            schedules[resource.id] = new OwnedSchedule(resource, data, lease, multiplier);
            data.NextReplenishDate = due;
            if (reports < 8)
            {
                reports++;
                Log.LogInfo($"Scheduled {resource.name}: native={nativeDue}, due={due}, multiplier={multiplier}.");
            }
        }
    }

    private void Discover(WeatherManager current)
    {
        resources.Clear();
        var seen = new HashSet<int>();
        foreach (var pair in current._descriptors)
        {
            var resource = pair.Key;
            if (resource == null || resource.id is < 0x01004008 or > 0x0100400a || !Matches(resource, pair.Value)) continue;
            if (!seen.Add(resource.id)) throw new InvalidOperationException("Duplicate mushroom resource identity in the active world.");
            resources.Add(resource);
        }
        foreach (var missing in schedules.Keys.Where(id => !seen.Contains(id)).ToArray()) schedules.Remove(missing);
        resourceTableCount = current._descriptors.Count;
    }

    private void Restore()
    {
        var current = GameObjectResolver.FindUnique<WeatherManager>();
        var weather = WeatherSystem.Instance;
        if (manager != null && current != null && manager.Pointer == current.Pointer && IsReady(current, weather)
            && weather!.Pointer.ToInt64() == clockIdentity)
        {
            foreach (var entry in schedules.Values)
            {
                if (!current._descriptors.TryGetValue(entry.Resource, out var live) || !Matches(entry.Resource, live)) continue;
                if (entry.Lease.Owns(current.Pointer.ToInt64(), clockIdentity, entry.Resource.Pointer.ToInt64(),
                    live.Pointer.ToInt64(), live.NextReplenishDate, live.NextReplenishDirty))
                    live.NextReplenishDate = entry.Lease.NativeDue;
            }
        }
        schedules.Clear();
        resources.Clear();
        resourceTableCount = -1;
        manager = null;
        clockIdentity = 0;
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        instance = null;
        Restore();
        StatusReason = null;
        base.Disable();
    }

    public override void Reset() { Disable(); Multiplier.Reset(); }
}
