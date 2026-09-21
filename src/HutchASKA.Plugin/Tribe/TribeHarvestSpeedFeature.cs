extern alias UnityCore;

using BepInEx.Logging;
using HarmonyLib;
using System.Diagnostics;
using HutchASKA.Core.Features;
using HutchASKA.Core.Tribe;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Attributes;
using SSSGame;
using SSSGame.Combat;
using GatherSession = SSSGame.VillagerGatherInteractionConfig.VillagerGatherInteractionSession;
using HarvestSession = SSSGame.VillagerHarvestInteractionConfig.VillagerHarvestInteractionSession;

namespace HutchASKA.Plugin.Tribe;

internal sealed class TribeHarvestSpeedFeature(ITribeContext tribe) : NativeFeature("tribe.harvest", "Harvest Speed")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".tribeharvest");
    private static readonly ManualLogSource DiagnosticLog = BepInEx.Logging.Logger.CreateLogSource("HutchASKA.TribeHarvest");
    private static TribeHarvestSpeedFeature? instance;
    [ThreadStatic] private static HarvestScope? harvestScope;
    [ThreadStatic] private static GatherScope? gatherScope;
    private int harvestEvents, gatherTicks;
    private float reportedMultiplier;
    private long nextReport;
    public MultiplierSetting Multiplier { get; } = new(1, 5);

    private sealed class HarvestScope(TribeHarvestSpeedFeature owner, HarvestSession session, HarvestInteraction target)
    {
        public TribeHarvestSpeedFeature Owner { get; } = owner;
        public HarvestSession Session { get; } = session;
        public HarvestInteraction Target { get; } = target;
        public bool Consumed { get; set; }
    }

    private sealed class GatherScope(TribeHarvestSpeedFeature owner, GatherSession session, InteractionMoveset moveset, IntPtr attributes)
    {
        public TribeHarvestSpeedFeature Owner { get; } = owner;
        public GatherSession Session { get; } = session;
        public InteractionMoveset Moveset { get; } = moveset;
        public IntPtr Attributes { get; } = attributes;
        public bool Consumed { get; set; }
    }

    private sealed record DamageWrite(TribeHarvestSpeedFeature Owner, DamageData Data, float Native, float Applied);

    public override CompatibilityResult ProbeCompatibility() =>
        AccessTools.DeclaredMethod(typeof(HarvestSession), "_DealSimulatedDamage", Type.EmptyTypes)?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(HarvestInteraction), "TakeDamage", new[] { typeof(DamageData) })?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(GatherSession), "Update", Type.EmptyTypes)?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(InteractionMoveset), "GetAttributeBonus", new[] { typeof(IPropertyContainer) })?.ReturnType == typeof(float)
        && typeof(HarvestSession).GetProperty("_harvestTime")?.PropertyType == typeof(float)
        && typeof(HarvestSession).GetProperty("_villager")?.PropertyType == typeof(Villager)
        && typeof(GatherSession).GetProperty("_duration")?.CanWrite == true
        && typeof(GatherSession).GetProperty("IsUse")?.PropertyType == typeof(bool)
        && typeof(GatherInteraction).GetProperty("Moveset")?.PropertyType == typeof(InteractionMoveset)
        && typeof(DamageData).GetProperty("baseDamage")?.CanWrite == true
        && typeof(DamageData).GetProperty("damageMultiplier")?.PropertyType == typeof(float)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Villager harvest damage or gathering work API is unavailable.");

    public override bool TryEnable()
    {
        instance = this;
        harvestEvents = gatherTicks = 0;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(HarvestSession), "_DealSimulatedDamage"),
            prefix: new HarmonyMethod(typeof(TribeHarvestSpeedFeature), nameof(HarvestPrefix)),
            finalizer: new HarmonyMethod(typeof(TribeHarvestSpeedFeature), nameof(HarvestFinalizer)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(HarvestInteraction), "TakeDamage", new[] { typeof(DamageData) }),
            prefix: new HarmonyMethod(typeof(TribeHarvestSpeedFeature), nameof(DamagePrefix)),
            finalizer: new HarmonyMethod(typeof(TribeHarvestSpeedFeature), nameof(DamageFinalizer)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(GatherSession), "Update"),
            prefix: new HarmonyMethod(typeof(TribeHarvestSpeedFeature), nameof(GatherPrefix)),
            finalizer: new HarmonyMethod(typeof(TribeHarvestSpeedFeature), nameof(GatherFinalizer)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(InteractionMoveset), "GetAttributeBonus"),
            postfix: new HarmonyMethod(typeof(TribeHarvestSpeedFeature), nameof(AttributeBonusPostfix)));
        PublishStatus();
        return base.TryEnable();
    }

    private bool Owns(InteractionSession session) => Multiplier.Value > 1
        && session.SessionState == InteractionSessionState.RUNNING && session.Agent != null
        && tribe.IsCurrentVillager(session.Agent);

    private static void HarvestPrefix(HarvestSession __instance, out HarvestScope? __state)
    {
        __state = harvestScope;
        harvestScope = null;
        var feature = instance;
        feature?.Hosted?.TryExecute(() =>
        {
            if (!feature.Owns(__instance) || __instance._harvestTime <= 0) return;
            var target = __instance.HarvestInteraction;
            if (!target || !target.isActiveAndEnabled || __instance._villager == null
                || __instance._villager.Pointer != __instance.Agent.Pointer) return;
            harvestScope = new(feature, __instance, target);
        });
    }

    private static Exception? HarvestFinalizer(Exception? __exception, HarvestScope? __state)
    {
        harvestScope = __state;
        return __exception;
    }

    private static void DamagePrefix(HarvestInteraction __instance, DamageData __0, out DamageWrite? __state)
    {
        __state = null;
        var scope = harvestScope;
        if (scope is null || scope.Consumed || __0 == null || scope.Target.Pointer != __instance.Pointer) return;
        // One freshly created DamageData is submitted by each native simulated harvest event.
        scope.Consumed = true;
        DamageWrite? write = null;
        scope.Owner.Hosted?.TryExecute(() =>
        {
            if (!scope.Owner.Owns(scope.Session) || !__instance.isActiveAndEnabled
                || scope.Session.HarvestInteraction?.Pointer != __instance.Pointer
                || __0.damageDealer == null
                || __0.result != 0 || __0.damageFlags != 0) return;
            var transform = scope.Session.Agent.GetTransform();
            var dealer = transform == null ? null : transform.GetComponent<IDamageDealer>();
            if (dealer == null || dealer.Pointer != __0.damageDealer.Pointer) return;
            var moveset = __instance.GetAttackMoveset();
            if (!moveset || scope.Session._moveset == null || moveset.Pointer != scope.Session._moveset.Pointer) return;
            var properties = __0.damageDealer.TryCast<IPropertyContainer>();
            if (properties == null) return;
            var weaponDamage = 0f;
            if (moveset.requiredEqippmentCategory)
            {
                if (__0.weapon == null) return;
                var damageProperty = 1002;
                if (!__0.weapon.TryGetPropertyValue(ref damageProperty, out weaponDamage)
                    || weaponDamage != __0.baseDamage) return;
            }
            else if (__0.weapon != null) return;
            var native = __0.baseDamage;
            var applied = TribeWorkSpeedMath.HarvestBaseDamage(native, weaponDamage,
                moveset.GetAttributeBonus(properties), moveset.damageMultiplier, __0.damageMultiplier, scope.Owner.Multiplier.Value);
            if (applied == native) return;
            write = new(scope.Owner, __0, native, applied);
            __0.baseDamage = applied;
            scope.Owner.harvestEvents = Math.Min(scope.Owner.harvestEvents + 1, 999999);
        });
        __state = write;
    }

    private static Exception? DamageFinalizer(Exception? __exception, DamageWrite? __state)
    {
        if (__state is null) return __exception;
        try
        {
            // Cleanup is independent of the execution gate: only this callback owns this write.
            if (__state.Data.baseDamage == __state.Applied) __state.Data.baseDamage = __state.Native;
        }
        catch (Exception error)
        {
            DiagnosticLog.LogError($"Tribe harvest damage cleanup failed: {error}");
            __state.Owner.Hosted?.Disable();
            return __exception is null ? error : new AggregateException(__exception, error);
        }
        return __exception;
    }

    private static void GatherPrefix(GatherSession __instance, out GatherScope? __state)
    {
        __state = gatherScope;
        gatherScope = null;
        var feature = instance;
        feature?.Hosted?.TryExecute(() =>
        {
            if (!feature.Owns(__instance) || __instance.IsUse || __instance.ForceUse || __instance._duration <= 0) return;
            var target = __instance.GatherInteraction;
            if (!target || !target.isActiveAndEnabled || target._session == null || !target._session.isMaster) return;
            var moveset = target.Moveset;
            if (!moveset) moveset = __instance.GatherConfig?.defaultMoveset;
            var attributes = __instance.Agent.GetAttributes();
            if (moveset == null || !moveset || attributes == null || !attributes) return;
            gatherScope = new(feature, __instance, moveset, attributes.Pointer);
        });
    }

    private static void AttributeBonusPostfix(InteractionMoveset __instance, IPropertyContainer __0, float __result)
    {
        var scope = gatherScope;
        if (scope is null || scope.Consumed || __instance.Pointer != scope.Moveset.Pointer
            || __0 == null || __0.Pointer != scope.Attributes) return;
        scope.Consumed = true;
        scope.Owner.Hosted?.TryExecute(() =>
        {
            if (!scope.Owner.Owns(scope.Session) || scope.Session.IsUse || scope.Session.ForceUse) return;
            scope.Session._duration = TribeWorkSpeedMath.GatherRemaining(scope.Session._duration,
                __instance.baseUnarmedDamage, __result, __instance.damageMultiplier,
                UnityCore::UnityEngine.Time.deltaTime, scope.Owner.Multiplier.Value);
            scope.Owner.gatherTicks = Math.Min(scope.Owner.gatherTicks + 1, 999999);
        });
    }

    private static Exception? GatherFinalizer(Exception? __exception, GatherScope? __state)
    {
        gatherScope = __state;
        return __exception;
    }

    private void PublishStatus()
    {
        reportedMultiplier = Multiplier.Value;
        StatusReason = $"{reportedMultiplier:0.#}x tribe work: {harvestEvents} harvest events; {gatherTicks} gather ticks. Native loot and completion remain active.";
        nextReport = Stopwatch.GetTimestamp() + Stopwatch.Frequency;
    }

    public override void Tick()
    {
        if (Multiplier.Value != reportedMultiplier || Stopwatch.GetTimestamp() >= nextReport) PublishStatus();
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        instance = null;
        StatusReason = null;
        base.Disable();
    }

    public override void Reset() { Disable(); Multiplier.Reset(); }
}
