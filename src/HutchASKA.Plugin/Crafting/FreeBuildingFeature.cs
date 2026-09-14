using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Crafting;

internal sealed class FreeBuildingFeature() : NativeFeature("building.free", "Free Building")
{
    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(
        "A construction-only supply/consumption hook preserving native completion is not verified.");
}
