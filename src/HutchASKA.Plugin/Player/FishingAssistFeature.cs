extern alias UnityCore;

using BepInEx.Logging;
using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.Player;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SSSGame;
using SSSGame.Combat;
using FishingRoutine = SSSGame.Combat.FishingMeleeObject.__FishingRoutine_d__75;
using NativeObject = UnityCore::UnityEngine.Object;

namespace HutchASKA.Plugin.Player;

internal sealed class FishingAssistFeature(IPlayerContext players) : NativeFeature("player.fishing", "Fishing Assists")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".fishing");
    private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("HutchASKA.Fishing");
    private static FishingAssistFeature? instance;
    private readonly HashSet<StepScope> ownedSteps = new();
    private readonly HashSet<PullScope> ownedPulls = new();
    private CastState? cast;
    private int casts, bites, weightedBites, pulls, reports;
    private (float Bite, float Rare, bool Anywhere, bool Easy, int Casts, int Bites, int Weights, int Pulls)? published;
    public MultiplierSetting BiteSpeed { get; } = new(1, 4);
    public MultiplierSetting RareWeight { get; } = new(1, 50);
    public bool RareAnywhere { get; set; }
    public bool EasyCatch { get; set; }

    private sealed class CastState(FishingRoutine routine, FishingMeleeObject rod, int playerIdentity)
    {
        public FishingRoutine Routine { get; } = routine;
        public FishingMeleeObject Rod { get; } = rod;
        public int PlayerIdentity { get; } = playerIdentity;
        public bool Initialized { get; set; }
        public float NativeWait { get; set; }
        public float AppliedWait { get; set; }
        public float EscapeScale { get; set; } = 1;
        public StepScope? Scope { get; set; }
    }

    private sealed class StepScope(FishingAssistFeature owner)
    {
        public FishingAssistFeature Owner { get; } = owner;
        public CastState? Cast { get; set; }
        public bool HadFish { get; set; }
        public FishableItemsConfig? Original { get; set; }
        public FishableItemsConfig? Replacement { get; set; }
        public bool Active { get; set; }

        public void Release()
        {
            var rod = Cast?.Rod;
            if (rod != null && Replacement != null && rod.FishableItems == Replacement)
                rod.FishableItems = Original;
            if (Replacement != null) NativeObject.Destroy(Replacement);
            Replacement = null;
            Original = null;
            Owner.ownedSteps.Remove(this);
        }
    }

    private sealed class PullScope(FishingAssistFeature owner, FishingMeleeObject rod)
    {
        public FishingAssistFeature Owner { get; } = owner;
        public FishingMeleeObject Rod { get; } = rod;
        public float Success { get; } = rod.fishSuccessRate;
        public AttributeCustomizationSelector? Selector { get; } = rod.fishSuccessRateCustomizationData;
        public bool Applied { get; set; }
        public bool Active { get; set; } = true;

        public void Release()
        {
            if (Applied && Rod != null)
            {
                if (Rod.fishSuccessRate == 1) Rod.fishSuccessRate = Success;
                if (Rod.fishSuccessRateCustomizationData == null) Rod.fishSuccessRateCustomizationData = Selector;
            }
            Applied = false;
            Owner.ownedPulls.Remove(this);
        }
    }

    public override CompatibilityResult ProbeCompatibility()
    {
        var valid = AccessTools.DeclaredMethod(typeof(FishingRoutine), "MoveNext", Type.EmptyTypes)?.ReturnType == typeof(bool)
            && AccessTools.DeclaredMethod(typeof(FishingMeleeObject), "_OnPullRod", Type.EmptyTypes)?.ReturnType == typeof(void)
            && typeof(FishingRoutine).GetProperty("__4__this")?.PropertyType == typeof(FishingMeleeObject)
            && new[] { "_waitTarget_5__2", "_waitTime_5__3", "_fishEscape_5__6" }
                .All(name => typeof(FishingRoutine).GetProperty(name) is { CanWrite: true, PropertyType: var type } && type == typeof(float))
            && new[] { "FishableItems", "fishSuccessRate", "fishSuccessRateCustomizationData" }
                .All(name => typeof(FishingMeleeObject).GetProperty(name)?.CanWrite == true)
            && new[] { "FishableItems", "baseFishes", "bonusChanceForSpecialFish" }
                .All(name => typeof(FishableItemsConfig).GetProperty(name)?.CanWrite == true)
            && typeof(Fishable).GetProperty("chance")?.PropertyType == typeof(float)
            && typeof(FishableBait).GetProperty("chanceModifier")?.PropertyType == typeof(float);
        return valid ? CompatibilityResult.Compatible()
            : CompatibilityResult.Incompatible("The current fishing coroutine, catch or weight API is unavailable.");
    }

    public override bool TryEnable()
    {
        instance = this;
        casts = bites = weightedBites = pulls = reports = 0;
        published = null;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(FishingRoutine), "MoveNext"),
            prefix: new HarmonyMethod(typeof(FishingAssistFeature), nameof(StepPrefix)),
            postfix: new HarmonyMethod(typeof(FishingAssistFeature), nameof(StepPostfix)),
            finalizer: new HarmonyMethod(typeof(FishingAssistFeature), nameof(StepFinalizer)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(FishingMeleeObject), "_OnPullRod"),
            prefix: new HarmonyMethod(typeof(FishingAssistFeature), nameof(PullPrefix)),
            finalizer: new HarmonyMethod(typeof(FishingAssistFeature), nameof(PullFinalizer)));
        PublishStatus();
        return base.TryEnable();
    }

    private bool IsLocal(FishingMeleeObject? rod, out int playerIdentity)
    {
        playerIdentity = 0;
        if (rod == null || !rod._equipped || rod.CurrentUseContext != FishingMeleeObject.UseContext.MeleeObject
            || rod._session == null || !rod._session.isMaster
            || !players.TryGetLocalPlayer(out var player) || player!.Geometry == null
            || rod._chrGeo != player.Geometry) return false;
        playerIdentity = player.GetInstanceID();
        return true;
    }

    private static bool HasFish(FishingMeleeObject rod) => rod.FishingItem?.GetContainer()?.GetAllUsedSlotsCount() > 0;

    private StepScope? BeginStep(FishingRoutine routine)
    {
        var rod = routine.__4__this;
        if (!IsLocal(rod, out var playerIdentity)) return null;
        if (cast is null || cast.Routine.Pointer != routine.Pointer || cast.PlayerIdentity != playerIdentity)
        {
            RestoreCast();
            cast = new(routine, rod, playerIdentity);
            casts = Math.Min(casts + 1, 999999);
        }
        var scope = cast.Scope ??= new(this) { Cast = cast };
        if (scope.Active) throw new InvalidOperationException("Fishing coroutine was entered recursively.");
        scope.Active = true;
        ownedSteps.Add(scope);
        return scope;
    }

    private void PrepareStep(StepScope scope)
    {
        var current = scope.Cast!;
        var routine = current.Routine;
        var rod = current.Rod;
        scope.HadFish = HasFish(rod);
        if (routine.__1__state != 0)
        {
            InitializeTimer(current);
            ApplyTimers(current, BiteSpeed.Value, scope.HadFish && EasyCatch ? 4 : 1);
        }
        if (!scope.HadFish) current.EscapeScale = 1;
        if ((RareWeight.Value == 1 && !RareAnywhere) || routine.__1__state != 1 || scope.HadFish
            || rod.sinker == null || !rod.sinker.IsInWater || rod._baitEquipPoint?._item == null
            || routine._waitTime_5__3 + UnityCore::UnityEngine.Time.deltaTime < routine._waitTarget_5__2) return;
        PrepareWeights(scope, rod);
    }

    private void PrepareWeights(StepScope scope, FishingMeleeObject rod)
    {
        var original = rod.FishableItems;
        if (original == null || original.FishableItems == null || original.baseFishes == null)
            throw new InvalidOperationException("Fishing selection configuration is missing.");
        if (original.FishableItems.Count > 256 || original.baseFishes.Count > 256)
            throw new InvalidOperationException("Fishing selection exceeds the supported bounded list size.");
        FishingAssistMath.EffectiveWeight(0, 0, original.bonusChanceForSpecialFish);
        var entries = new Il2CppSystem.Collections.Generic.List<Fishable>();
        double maximumTotal = 0;
        for (var i = 0; i < original.FishableItems.Count; i++)
        {
            var source = original.FishableItems[i];
            if (source == null || source.info == null || source.BaitsList == null || source.BaitsList.Count > 128)
                throw new InvalidOperationException("Fishing weight or bait configuration is invalid.");
            var special = !original.baseFishes.Contains(source.info);
            var baits = special ? new Il2CppSystem.Collections.Generic.List<FishableBait>() : source.BaitsList;
            var chance = FishingAssistMath.ScaleWeight(source.chance, special, RareWeight.Value);
            var largestBaitBonus = 0f;
            for (var j = 0; j < source.BaitsList.Count; j++)
            {
                var bait = source.BaitsList[j];
                if (bait == null) throw new InvalidOperationException("Fishing bait entry is missing.");
                var bonus = FishingAssistMath.ScaleWeight(bait.chanceModifier, special, RareWeight.Value);
                FishingAssistMath.EffectiveWeight(chance, bonus, original.bonusChanceForSpecialFish);
                largestBaitBonus = Math.Max(largestBaitBonus, bonus);
                if (special) baits.Add(new FishableBait { BaitInfo = bait.BaitInfo, chanceModifier = bonus });
            }
            maximumTotal += FishingAssistMath.EffectiveWeight(chance, largestBaitBonus, original.bonusChanceForSpecialFish);
            if (maximumTotal > float.MaxValue)
                throw new InvalidOperationException("Fishing weight total exceeds the native selection range.");
            if (!special) { entries.Add(source); continue; }
            entries.Add(new Fishable
            {
                info = source.info, chance = chance, ExpertiseGain = source.ExpertiseGain,
                BaitsList = baits, biomeAvailabilty = source.biomeAvailabilty
            });
        }
        scope.Original = original;
        scope.Replacement = NativeObject.Instantiate(original).Cast<FishableItemsConfig>();
        scope.Replacement.FishableItems = entries;
        if (RareAnywhere)
        {
            var populationIndependent = new Il2CppSystem.Collections.Generic.List<SandSailorStudio.Inventory.ItemInfo>();
            for (var i = 0; i < original.baseFishes.Count; i++)
            {
                var item = original.baseFishes[i];
                if (item != null && !populationIndependent.Contains(item)) populationIndependent.Add(item);
            }
            for (var i = 0; i < original.FishableItems.Count; i++)
            {
                var source = original.FishableItems[i];
                var nativeBase = original.baseFishes.Contains(source.info);
                if (FishingAssistMath.UseBaseFishEligibility(nativeBase, true)
                    && !populationIndependent.Contains(source.info)) populationIndependent.Add(source.info);
            }
            scope.Replacement.baseFishes = populationIndependent;
        }
        rod.FishableItems = scope.Replacement;
        weightedBites = Math.Min(weightedBites + 1, 999999);
    }

    private static void InitializeTimer(CastState current)
    {
        if (current.Initialized) return;
        current.NativeWait = current.AppliedWait = current.Routine._waitTarget_5__2;
        current.Initialized = true;
    }

    private static void ApplyTimers(CastState current, float speed, float escapeScale)
    {
        var routine = current.Routine;
        // A changed target belongs to the game or another mod, so adopt it as a fresh baseline.
        if (routine._waitTarget_5__2 != current.AppliedWait)
            current.NativeWait = current.AppliedWait = routine._waitTarget_5__2;
        var target = FishingAssistMath.WaitTarget(current.NativeWait, speed);
        if (current.AppliedWait > 0 && target != current.AppliedWait)
            routine._waitTime_5__3 = FishingAssistMath.RescaleTimer(Math.Max(0, routine._waitTime_5__3), current.AppliedWait, target);
        if (routine._waitTarget_5__2 != target) routine._waitTarget_5__2 = target;
        current.AppliedWait = target;
        if (current.EscapeScale != escapeScale && routine._fishEscape_5__6 > 0)
            routine._fishEscape_5__6 = FishingAssistMath.RescaleTimer(routine._fishEscape_5__6, current.EscapeScale, escapeScale);
        current.EscapeScale = escapeScale;
    }

    private void FinishStep(StepScope scope, bool running)
    {
        var current = scope.Cast;
        if (current is null || !IsLocal(current.Rod, out var player) || current.PlayerIdentity != player) return;
        if (!running) { RestoreCast(); return; }
        InitializeTimer(current);
        var hooked = HasFish(current.Rod);
        if (!scope.HadFish && hooked)
        {
            current.EscapeScale = 1;
            bites = Math.Min(bites + 1, 999999);
            if (reports < 12)
            {
                reports++;
                Log.LogInfo($"Bite {bites}: wait={BiteSpeed.Value:0.#}x, rare weight={RareWeight.Value:0.#}x, rare anywhere={RareAnywhere}, easy catch={EasyCatch}; native fish/bait processing completed.");
            }
        }
        ApplyTimers(current, BiteSpeed.Value, hooked && EasyCatch ? 4 : 1);
        PublishStatus();
    }

    private static void StepPrefix(FishingRoutine __instance, out StepScope? __state)
    {
        var feature = instance;
        StepScope? scope = null;
        feature?.Hosted?.TryExecute(() =>
        {
            scope = feature.BeginStep(__instance);
            if (scope is not null) feature.PrepareStep(scope);
        });
        __state = scope;
    }

    private static void StepPostfix(bool __result, StepScope? __state)
    {
        if (__state is { } scope) scope.Owner.Hosted?.TryExecute(() => scope.Owner.FinishStep(scope, __result));
    }

    private static Exception? StepFinalizer(Exception? __exception, StepScope? __state)
    {
        if (__state is not null) __state.Active = false;
        try { __state?.Release(); }
        catch (Exception error)
        {
            Log.LogError($"Fishing selection cleanup failed: {error}");
            __state?.Owner.Hosted?.Disable();
            return __exception is null ? error : new AggregateException(__exception, error);
        }
        return __exception;
    }

    private static void PullPrefix(FishingMeleeObject __instance, out PullScope? __state)
    {
        PullScope? scope = null;
        var feature = instance;
        feature?.Hosted?.TryExecute(() =>
        {
            if (!feature.EasyCatch || !feature.IsLocal(__instance, out _) || !HasFish(__instance)) return;
            scope = new(feature, __instance) { Applied = true };
            feature.ownedPulls.Add(scope);
            __instance.fishSuccessRate = 1;
            __instance.fishSuccessRateCustomizationData = null;
            feature.pulls = Math.Min(feature.pulls + 1, 999999);
            feature.PublishStatus();
        });
        __state = scope;
    }

    private static Exception? PullFinalizer(Exception? __exception, PullScope? __state)
    {
        if (__state is not null) __state.Active = false;
        try { __state?.Release(); }
        catch (Exception error)
        {
            Log.LogError($"Fishing catch cleanup failed: {error}");
            __state?.Owner.Hosted?.Disable();
            return __exception is null ? error : new AggregateException(__exception, error);
        }
        return __exception;
    }

    private void RestoreCast()
    {
        var current = cast;
        if (current is null) return;
        if (current.Initialized && IsLocal(current.Rod, out var player) && current.PlayerIdentity == player)
            ApplyTimers(current, 1, 1);
        cast = null;
    }

    private void PublishStatus()
    {
        var values = (BiteSpeed.Value, RareWeight.Value, RareAnywhere, EasyCatch, casts, bites, weightedBites, pulls);
        if (published == values) return;
        published = values;
        StatusReason = $"Bite {BiteSpeed.Value:0.#}x; rare weight {RareWeight.Value:0.#}x; rare anywhere {(RareAnywhere ? "on" : "off")}; easy catch {(EasyCatch ? "on" : "off")}. Casts {casts}, bites {bites}, weighted selections {weightedBites}, assisted pulls {pulls}.";
    }

    public override void Tick()
    {
        if (cast is { Initialized: true } current)
        {
            if (!IsLocal(current.Rod, out var player) || current.PlayerIdentity != player) cast = null;
            else if (current.Rod._fishingRoutine == null) RestoreCast();
            else ApplyTimers(current, BiteSpeed.Value, EasyCatch && HasFish(current.Rod) ? 4 : 1);
        }
        PublishStatus();
    }

    public override void Disable()
    {
        harmony.UnpatchSelf();
        instance = null;
        foreach (var scope in ownedSteps.ToArray()) if (!scope.Active) scope.Release();
        foreach (var scope in ownedPulls.ToArray()) if (!scope.Active) scope.Release();
        RestoreCast();
        if (ownedSteps.Count != 0 || ownedPulls.Count != 0)
            throw new InvalidOperationException("Fishing cleanup is waiting for an active native callback.");
        StatusReason = null;
        base.Disable();
    }

    public override void Reset()
    {
        Disable();
        BiteSpeed.Reset();
        RareWeight.Reset();
        RareAnywhere = false;
        EasyCatch = false;
    }
}
