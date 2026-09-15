using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Crafting;

internal sealed class FreeRepairsFeature() : NativeFeature("repairs.free", "Free Repairs")
{
    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(
        "Repair requirements are identified, but native repair-container consumption and restoration are not yet implemented.");
}
