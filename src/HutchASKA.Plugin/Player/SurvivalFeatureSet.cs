using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.Attributes;
using SSSGame;

namespace HutchASKA.Plugin.Player;

internal sealed class SurvivalFeatureSet(IPlayerContext players, bool food)
    : NativeFeature(food ? "player.hunger" : "player.thirst", food ? "Infinite Hunger" : "Infinite Thirst")
{
    public override CompatibilityResult ProbeCompatibility()
    {
        var type = typeof(VariableAttribute);
        return typeof(PlayerCharacter).GetMethod("GetPlayerSurvival", Type.EmptyTypes) is not null
            && typeof(CharacterSurvival).GetProperty(food ? "_foodVAttr" : "_waterVAttr")?.PropertyType == type
            && type.GetProperty("min")?.PropertyType == typeof(float)
            && type.GetProperty("max")?.PropertyType == typeof(float)
            && type.GetMethod("SetValue", new[] { typeof(float) })?.ReturnType == typeof(void)
            ? CompatibilityResult.Compatible() : CompatibilityResult.Incompatible("Native survival attribute access is unavailable.");
    }
    public override void Tick()
    {
        if (!players.TryGetLocalPlayer(out var player)) return;
        var survival = player!.GetPlayerSurvival();
        if (!survival) return;
        var attribute = food ? survival._foodVAttr : survival._waterVAttr;
        if (attribute is null) throw new InvalidOperationException("Local survival attribute is unavailable.");
        VariableAttributeController.Fill(attribute);
    }
}
