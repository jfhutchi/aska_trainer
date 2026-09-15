using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Tribe;

internal sealed class FreezeAgingFeature() : NativeFeature("tribe.aging", "Freeze Aging")
{
    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(
        "The verified lifetime countdown applies only to golems; ordinary villager aging has no confirmed native hook.");
}
