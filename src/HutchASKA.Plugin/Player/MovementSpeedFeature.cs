using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Attributes;
using SSSGame.Controllers;
using NativeAttribute = SandSailorStudio.Attributes.Attribute;

namespace HutchASKA.Plugin.Player;

internal sealed class MovementSpeedFeature(IPlayerContext players) : NativeFeature("player.movement", "Movement Speed")
{
    public MultiplierSetting Multiplier { get; } = new(1, 5);
    private AttributeModifier? ownedModifier;
    private IntPtr attributeIdentity;
    private int playerIdentity;
    private float applied;

    public override CompatibilityResult ProbeCompatibility() =>
        typeof(CharacterMovement).GetProperty("_moveMultiAttr")?.PropertyType == typeof(NativeAttribute)
        && typeof(NativeAttribute).GetMethod("AddModifier", new[] { typeof(AttributeModifier) }) is not null
        && typeof(NativeAttribute).GetMethod("RemoveModifier", new[] { typeof(AttributeModifier) }) is not null
        && typeof(AttributeModifier).GetConstructor(new[] { typeof(float), typeof(ModifierOperation) }) is not null
        ? CompatibilityResult.Compatible() : CompatibilityResult.Incompatible("Native movement modifier API is unavailable.");

    public override void Tick()
    {
        if (Multiplier.Value == 1) { Restore(); return; }
        if (!players.TryGetLocalPlayer(out var player))
        {
            if (ownedModifier is not null) throw new InvalidOperationException("Local player unloaded while movement modifier was active.");
            return;
        }
        var attribute = player!.GetCharacterMovement()?._moveMultiAttr
            ?? throw new InvalidOperationException("Local movement attribute is unavailable.");
        if (ownedModifier is not null && (player.GetInstanceID() != playerIdentity || attribute.Pointer != attributeIdentity))
            throw new InvalidOperationException("Movement target changed; disable and re-enable for the current player.");
        if (ownedModifier is not null && applied == Multiplier.Value) return;
        Restore();
        playerIdentity = player.GetInstanceID();
        attributeIdentity = attribute.Pointer;
        applied = Multiplier.Value;
        // An owned modifier preserves the live native baseline and other game modifiers.
        ownedModifier = new AttributeModifier(applied, ModifierOperation.MULTIPLY);
        attribute.AddModifier(ownedModifier);
    }

    private void Restore()
    {
        if (ownedModifier is null) return;
        var modifier = ownedModifier;
        ownedModifier = null;
        if (!players.TryGetLocalPlayer(out var player) || player!.GetInstanceID() != playerIdentity)
            throw new InvalidOperationException("Movement target unloaded; its modifier cannot be safely restored through a stale wrapper.");
        var attribute = player.GetCharacterMovement()?._moveMultiAttr;
        if (attribute is null || attribute.Pointer != attributeIdentity)
            throw new InvalidOperationException("Movement attribute replaced; old native target is no longer safe to access.");
        attribute.RemoveModifier(modifier);
    }
    public override void Disable() { Restore(); base.Disable(); }
    public override void Reset() { Disable(); Multiplier.Reset(); }
}
