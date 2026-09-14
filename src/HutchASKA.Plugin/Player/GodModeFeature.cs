using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using HutchASKA.Plugin.Player.Patches;
using SSSGame;
using SSSGame.Combat;

namespace HutchASKA.Plugin.Player;

internal sealed class GodModeFeature(IPlayerContext players) : NativeFeature("player.god", "God Mode")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".god");
    public override CompatibilityResult ProbeCompatibility()
    {
        var target = AccessTools.DeclaredMethod(typeof(PlayerCharacter), "TakeDamage", new[] { typeof(DamageData) });
        return target?.ReturnType == typeof(void) ? CompatibilityResult.Compatible()
            : CompatibilityResult.Incompatible("Expected void PlayerCharacter.TakeDamage(DamageData) is missing.");
    }
    public override bool TryEnable()
    {
        PlayerDamagePatch.Feature = this;
        harmony.Patch(AccessTools.DeclaredMethod(typeof(PlayerCharacter), "TakeDamage", new[] { typeof(DamageData) }),
            prefix: new HarmonyMethod(typeof(PlayerDamagePatch), nameof(PlayerDamagePatch.Prefix)));
        return base.TryEnable();
    }
    public bool SuppressDamage(PlayerCharacter target)
    {
        var local = false;
        return Hosted?.TryExecute(() => local = players.TryGetLocalPlayer(out var player) && player == target) == true && local;
    }
    public override void Disable() { harmony.UnpatchSelf(); base.Disable(); }
}
