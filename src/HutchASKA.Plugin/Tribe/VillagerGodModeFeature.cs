using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;
using HutchASKA.Plugin.Tribe.Patches;

namespace HutchASKA.Plugin.Tribe;

internal sealed class VillagerGodModeFeature(ITribeContext tribe) : NativeFeature("tribe.god", "Invincible Villagers")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".tribe.god");
    public override CompatibilityResult ProbeCompatibility() => AskaTribeContext.DamageTarget()?.ReturnType == typeof(void)
        ? AskaTribeContext.ProbeCompatibility() : CompatibilityResult.Incompatible("Expected villager damage signature is unavailable.");
    public override bool TryEnable()
    {
        VillagerDamagePatch.Feature = this;
        harmony.Patch(AskaTribeContext.DamageTarget(),
            prefix: new HarmonyMethod(typeof(VillagerDamagePatch), nameof(VillagerDamagePatch.Prefix)));
        return base.TryEnable();
    }
    internal bool SuppressDamage(object candidate)
    {
        var owned = false;
        return Hosted?.TryExecute(() => owned = tribe.IsCurrentVillager(candidate)) == true && owned;
    }
    public override void Disable() { harmony.UnpatchSelf(); base.Disable(); }
}
