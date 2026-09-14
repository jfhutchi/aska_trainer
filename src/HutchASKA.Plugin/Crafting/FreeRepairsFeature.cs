using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Crafting;

internal sealed class FreeRepairsFeature() : NativeFeature("repairs.free", "Free Repairs")
{
    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(
        "A repair-only supply/consumption hook preserving native completion is not verified.");
}
