using System.Reflection;
using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using HutchASKA.Plugin.Player.Patches;
using SSSGame;
using SSSGame.Controllers;

namespace HutchASKA.Plugin.Player;

internal sealed class InfiniteStaminaFeature(IPlayerContext players) : NativeFeature("player.stamina", "Infinite Stamina")
{
    private readonly Harmony harmony = new(Plugin.PluginGuid + ".stamina");
    private static MethodInfo? Drain(Type type) => AccessTools.DeclaredMethod(type, "DrainStamina", new[] { typeof(float), typeof(DrainStaminaUsage) });
    private static MethodInfo? TryDrain() => AccessTools.DeclaredMethod(typeof(CharacterMovement), "TryDrainStamina", new[] { typeof(float), typeof(bool) });
    public override CompatibilityResult ProbeCompatibility() =>
        Drain(typeof(Character))?.ReturnType == typeof(void) && Drain(typeof(CharacterMovement))?.ReturnType == typeof(void)
        && TryDrain()?.ReturnType == typeof(bool) ? CompatibilityResult.Compatible()
            : CompatibilityResult.Incompatible("Expected Character/CharacterMovement stamina drain signatures are missing.");
    public override bool TryEnable()
    {
        StaminaDrainPatch.Feature = this;
        harmony.Patch(Drain(typeof(Character)), prefix: new HarmonyMethod(typeof(StaminaDrainPatch), nameof(StaminaDrainPatch.CharacterPrefix)));
        harmony.Patch(Drain(typeof(CharacterMovement)), prefix: new HarmonyMethod(typeof(StaminaDrainPatch), nameof(StaminaDrainPatch.MovementPrefix)));
        harmony.Patch(TryDrain(), prefix: new HarmonyMethod(typeof(StaminaDrainPatch), nameof(StaminaDrainPatch.TryPrefix)));
        return base.TryEnable();
    }
    internal bool IsLocal(Character character)
    {
        var local = false;
        return Hosted?.TryExecute(() => local = players.TryGetLocalPlayer(out var player) && player == character) == true && local;
    }
    internal bool IsLocal(CharacterMovement movement)
    {
        var local = false;
        return Hosted?.TryExecute(() => local = players.TryGetLocalPlayer(out var player) && player!.GetCharacterMovement() == movement) == true && local;
    }
    public override void Disable() { harmony.UnpatchSelf(); base.Disable(); }
}
