using HutchASKA.Core.Features;
using HutchASKA.Core.Player;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Player;

internal sealed class TerrainLevelingFeature() : NativeFeature("player.terrain", "Leveling Area")
{
    private const string UnavailableReason =
        "Expanded leveling is disabled after a preview crash. Use the game's normal leveling area.";

    public TerrainAreaSetting Size { get; } = new();

    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(UnavailableReason);

    public override bool TryEnable()
    {
        StatusReason = UnavailableReason;
        return false;
    }

    public override void Reset() { Disable(); Size.Reset(); }
}
