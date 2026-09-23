using HarmonyLib;
using HutchASKA.Core.Crafting;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using HutchASKA.Plugin.Tribe;
using SSSGame;
using PlayerSession = SSSGame.PlayerCraftInteractionConfig.PlayerCraftInteractionSession;
using VillagerSession = SSSGame.VillagerCraftInteractionConfig.VillagerCraftSession;

namespace HutchASKA.Plugin.Crafting;

internal sealed class BenchCraftSpeedFeature(IPlayerContext players, ITribeContext tribe)
    : NativeFeature("crafting.benchspeed", "Bench Craft Speed")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".benchcraftspeed");
    private readonly ITribeContext tribeContext = tribe;
    private static BenchCraftSpeedFeature? active;
    private int boostedPlayer, boostedTribe;
    private long nextStatusAt;
    public MultiplierSetting Multiplier { get; } = new(1, 3);

    private readonly record struct ProgressState(IntPtr Session, IntPtr Interaction, float Before);

    public override CompatibilityResult ProbeCompatibility() =>
        AccessTools.DeclaredMethod(typeof(PlayerSession), "Update", Type.EmptyTypes)?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(VillagerSession), "Update", Type.EmptyTypes)?.ReturnType == typeof(void)
        && typeof(PlayerSession).GetProperty("_volumeProgress")?.CanWrite == true
        && typeof(PlayerSession).GetProperty("_volumeTarget")?.PropertyType == typeof(float)
        && typeof(VillagerSession).GetProperty("_volumeProgress")?.CanWrite == true
        && typeof(VillagerSession).GetProperty("_volumeTarget")?.PropertyType == typeof(float)
        && typeof(PlayerSession).GetProperty("CraftInteraction")?.PropertyType == typeof(CraftInteraction)
        && typeof(VillagerSession).GetProperty("CraftInteraction")?.PropertyType == typeof(CraftInteraction)
        && typeof(VillagerSession).GetProperty("Villager")?.PropertyType == typeof(Villager)
        && typeof(CraftInteraction).GetProperty("craftStationHost")?.PropertyType == typeof(CraftingStation)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Player or villager bench-crafting progress API is unavailable.");

    public override bool TryEnable()
    {
        try
        {
            harmony.Patch(AccessTools.DeclaredMethod(typeof(PlayerSession), "Update"),
                prefix: new HarmonyMethod(typeof(BenchCraftSpeedFeature), nameof(PlayerPrefix)),
                postfix: new HarmonyMethod(typeof(BenchCraftSpeedFeature), nameof(PlayerPostfix)));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(VillagerSession), "Update"),
                prefix: new HarmonyMethod(typeof(BenchCraftSpeedFeature), nameof(VillagerPrefix)),
                postfix: new HarmonyMethod(typeof(BenchCraftSpeedFeature), nameof(VillagerPostfix)));
            boostedPlayer = boostedTribe = 0;
            nextStatusAt = 0;
            active = this;
            PublishStatus();
            return base.TryEnable();
        }
        catch
        {
            harmony.UnpatchSelf();
            if (active == this) active = null;
            throw;
        }
    }

    private static bool IsCurrentBench(CraftInteraction? interaction)
    {
        if (interaction is null || !interaction) return false;
        return interaction.isActiveAndEnabled && interaction.craftStationHost;
    }

    private bool IsLocalPlayer(PlayerSession session)
    {
        if (!players.TryGetLocalPlayer(out var player)) return false;
        var agent = session.Agent?.TryCast<PlayerInteractionAgent>();
        return agent != null && agent.GetCharacter() == player;
    }

    private static void PlayerPrefix(PlayerSession __instance, out ProgressState __state)
    {
        __state = default;
        var feature = active;
        if (feature is null || feature.Multiplier.Value != 3 || __instance is null) return;
        var state = default(ProgressState);
        feature.Hosted?.TryExecute(() =>
        {
            var interaction = __instance.CraftInteraction;
            if (__instance.SessionState != InteractionSessionState.RUNNING
                || !IsCurrentBench(interaction) || !feature.IsLocalPlayer(__instance)) return;
            state = new(__instance.Pointer, interaction.Pointer, __instance._volumeProgress);
        });
        __state = state;
    }

    private static void VillagerPrefix(VillagerSession __instance, out ProgressState __state)
    {
        __state = default;
        var feature = active;
        if (feature is null || feature.Multiplier.Value != 3 || __instance is null) return;
        var state = default(ProgressState);
        feature.Hosted?.TryExecute(() =>
        {
            var interaction = __instance.CraftInteraction;
            if (__instance.SessionState != InteractionSessionState.RUNNING
                || !IsCurrentBench(interaction) || __instance.Villager is null
                || !feature.tribeContext.IsCurrentVillager(__instance.Villager)) return;
            state = new(__instance.Pointer, interaction.Pointer, __instance._volumeProgress);
        });
        __state = state;
    }

    private static void PlayerPostfix(PlayerSession __instance, ProgressState __state)
    {
        var feature = active;
        if (feature is null || __state.Session == IntPtr.Zero || __instance is null
            || __instance.Pointer != __state.Session) return;
        feature.Hosted?.TryExecute(() =>
        {
            if (__instance.SessionState != InteractionSessionState.RUNNING
                || __instance.CraftInteraction?.Pointer != __state.Interaction) return;
            var native = __instance._volumeProgress;
            var boosted = BenchCraftProgress.TripleIncrement(__state.Before, native, __instance._volumeTarget);
            if (boosted <= native) return;
            __instance._volumeProgress = boosted;
            feature.boostedPlayer = Math.Min(feature.boostedPlayer + 1, 999999);
        });
    }

    private static void VillagerPostfix(VillagerSession __instance, ProgressState __state)
    {
        var feature = active;
        if (feature is null || __state.Session == IntPtr.Zero || __instance is null
            || __instance.Pointer != __state.Session) return;
        feature.Hosted?.TryExecute(() =>
        {
            if (__instance.SessionState != InteractionSessionState.RUNNING
                || __instance.CraftInteraction?.Pointer != __state.Interaction) return;
            var native = __instance._volumeProgress;
            var boosted = BenchCraftProgress.TripleIncrement(__state.Before, native, __instance._volumeTarget);
            if (boosted <= native) return;
            __instance._volumeProgress = boosted;
            feature.boostedTribe = Math.Min(feature.boostedTribe + 1, 999999);
        });
    }

    private void PublishStatus()
    {
        StatusReason = $"3x current bench work; player updates {boostedPlayer}, tribe updates {boostedTribe}. Native recipe completion and materials remain in control.";
    }

    public override void Tick()
    {
        var now = Environment.TickCount64;
        if (now < nextStatusAt) return;
        nextStatusAt = now + 1000;
        PublishStatus();
    }

    public override void Disable()
    {
        active = null;
        harmony.UnpatchSelf();
        StatusReason = null;
        base.Disable();
    }

    public override void Reset() { Disable(); Multiplier.Reset(); }
}
