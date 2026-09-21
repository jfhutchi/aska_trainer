using BepInEx.Logging;
using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.Player;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using HutchASKA.Plugin.Tribe;
using SSSGame;
using NativeAttribute = SandSailorStudio.Attributes.Attribute;

namespace HutchASKA.Plugin.Player;

internal sealed class SkillGainFeature(IPlayerContext players, ITribeContext tribe, bool forTribe)
    : NativeFeature(forTribe ? "tribe.skills" : "player.skills", forTribe ? "Tribe Skill Gain" : "Player Skill Gain")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + (forTribe ? ".tribe.skills" : ".player.skills"));
    private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("HutchASKA.Skills");
    private static SkillGainFeature? playerFeature, tribeFeature;
    private int awards;
    private (float Multiplier, int Awards)? published;
    public MultiplierSetting Multiplier { get; } = new(1, 5);

    private static System.Reflection.MethodInfo? AwardMethod() => AccessTools.DeclaredMethod(
        typeof(InteractionMoveset), "AwardProfficiencyPoints", new[] { typeof(IInteractionAgent), typeof(float), typeof(NativeAttribute) });

    public override CompatibilityResult ProbeCompatibility() => AwardMethod()?.ReturnType == typeof(void)
        && AccessTools.DeclaredMethod(typeof(PlayerInteractionAgent), "GetCharacter", Type.EmptyTypes)?.ReturnType == typeof(PlayerCharacter)
        && typeof(IInteractionAgent).GetProperty("HasAuthority")?.PropertyType == typeof(bool)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("The normal skill experience award API is unavailable.");

    public override bool TryEnable()
    {
        if (forTribe) tribeFeature = this; else playerFeature = this;
        awards = 0;
        published = null;
        harmony.Patch(AwardMethod(), prefix: new HarmonyMethod(typeof(SkillGainFeature),
            forTribe ? nameof(TribePrefix) : nameof(PlayerPrefix)));
        PublishStatus();
        return base.TryEnable();
    }

    private float Scale(IInteractionAgent agent, float amount)
    {
        if (Multiplier.Value == 1 || amount <= 0 || agent is null || !agent.HasAuthority) return amount;
        if (forTribe)
        {
            var villager = agent.TryCast<Villager>();
            if (villager == null || !tribe.IsCurrentVillager(villager)) return amount;
        }
        else
        {
            var playerAgent = agent.TryCast<PlayerInteractionAgent>();
            if (playerAgent == null || !players.TryGetLocalPlayer(out var player)
                || playerAgent.GetCharacter() != player) return amount;
        }
        var scaled = SkillGainMath.ScaleAward(amount, Multiplier.Value);
        awards = Math.Min(awards + 1, 999999);
        if (awards <= 8) Log.LogInfo($"{DisplayName}: {amount:0.###} -> {scaled:0.###} award input ({Multiplier.Value:0}x); native bonuses and caps still apply.");
        return scaled;
    }

    private static void Apply(SkillGainFeature? feature, IInteractionAgent agent, ref float amount)
    {
        if (feature is null) return;
        var original = amount;
        var result = original;
        if (feature.Hosted?.TryExecute(() => result = feature.Scale(agent, original)) == true) amount = result;
    }

    // The original amount is by value. Only this call's incoming award changes.
    private static void PlayerPrefix(IInteractionAgent __0, ref float __1) => Apply(playerFeature, __0, ref __1);
    private static void TribePrefix(IInteractionAgent __0, ref float __1) => Apply(tribeFeature, __0, ref __1);

    private void PublishStatus()
    {
        var current = (Multiplier.Value, awards);
        if (published == current) return;
        published = current;
        StatusReason = $"{Multiplier.Value:0}x earned skill experience; {awards} boosted award inputs. Native level-up and cap checks remain active.";
    }

    public override void Tick() => PublishStatus();

    public override void Disable()
    {
        harmony.UnpatchSelf();
        if (forTribe) tribeFeature = null; else playerFeature = null;
        StatusReason = null;
        base.Disable();
    }

    public override void Reset() { Disable(); Multiplier.Reset(); }
}
