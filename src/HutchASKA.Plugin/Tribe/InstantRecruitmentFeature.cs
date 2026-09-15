using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SSSGame;

namespace HutchASKA.Plugin.Tribe;

internal sealed class InstantRecruitmentFeature(IWorldContext world) : NativeFeature("tribe.recruitment", "Instant Normal Recruitment")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".recruitment");
    private readonly IWorldContext worldContext = world;
    private static InstantRecruitmentFeature? instance;
    [ThreadStatic] private static IntPtr completingOutlet;

    public override CompatibilityResult ProbeCompatibility() =>
        AccessTools.DeclaredMethod(typeof(VillagerOutlet), "_OnWeatherChanged", new[] { typeof(WeatherEventData) }) is not null
        && typeof(VillagerOutlet).GetProperty("_NetworkedVillagerTimerEnd")?.PropertyType == typeof(float)
        && typeof(VillagerOutlet).GetProperty("_SpawnPending")?.PropertyType == typeof(Fusion.NetworkBool)
        && typeof(VillagerOutlet).GetProperty("OwnerStructure") is not null
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Normal recruitment deadline/completion API is unavailable.");

    public override bool TryEnable()
    {
        instance = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(VillagerOutlet), "_OnWeatherChanged"),
            prefix: new HarmonyMethod(typeof(InstantRecruitmentFeature), nameof(WeatherPrefix)),
            finalizer: new HarmonyMethod(typeof(InstantRecruitmentFeature), nameof(WeatherFinalizer)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(VillagerOutlet), "_NetworkedVillagerTimerEnd"),
            postfix: new HarmonyMethod(typeof(InstantRecruitmentFeature), nameof(DeadlinePostfix)));
        return base.TryEnable();
    }

    private static bool IsOwnedPendingOutlet(VillagerOutlet outlet)
    {
        if (outlet == null || !outlet.isActiveAndEnabled || outlet.Object == null
            || !outlet.Object.IsValid || !outlet.Object.HasStateAuthority)
            return false;
        var owner = outlet.OwnerStructure;
        var settlement = GameObjectResolver.FindUnique<Settlement>();
        return owner != null && owner.IsValid && owner.IsActive && !owner.IsDead && !owner.Dismantled
            && settlement != null && owner.Settlement == settlement
            && outlet._session != null && outlet._session.isMaster && outlet._SpawnPending;
    }

    private static void WeatherPrefix(VillagerOutlet __instance, out IntPtr __state)
    {
        __state = completingOutlet;
        completingOutlet = IntPtr.Zero;
        var feature = instance;
        feature?.Hosted?.TryExecute(() =>
        {
            if (IsOwnedPendingOutlet(__instance)) completingOutlet = __instance.Pointer;
        });
    }

    private static Exception? WeatherFinalizer(Exception? __exception, IntPtr __state)
    {
        completingOutlet = __state;
        return __exception;
    }

    private static void DeadlinePostfix(VillagerOutlet __instance, ref float __result)
    {
        var feature = instance;
        if (feature is null || completingOutlet != __instance.Pointer) return;
        // The first native operation in _OnWeatherChanged is this deadline read.
        // Consume the scope so events/serialization later in completion see native state.
        completingOutlet = IntPtr.Zero;
        var result = __result;
        if (feature.Hosted?.TryExecute(() =>
        {
            if (!IsOwnedPendingOutlet(__instance) || !feature.worldContext.TryGetWeatherSystem(out var weather)) return;
            // Native completion compares dayOfYear + timeOfDay / 24 with this deadline.
            var now = weather!.dayOfYear + weather.timeOfDay / 24f;
            if (!float.IsFinite(now) || !float.IsFinite(result))
                throw new InvalidOperationException("Recruitment game-time values are not finite.");
            result = Math.Min(result, now);
        }) == true) __result = result;
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        instance = null;
        completingOutlet = IntPtr.Zero;
        base.Disable();
    }
}
