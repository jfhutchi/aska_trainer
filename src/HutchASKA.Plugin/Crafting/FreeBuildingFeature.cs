using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Crafting;

internal sealed class FreeBuildingFeature() : NativeFeature("building.free", "Free Building")
{
    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(
        "Construction uses a persistent supply container; partial materials and disable/reload restoration are not yet implemented.");
}
