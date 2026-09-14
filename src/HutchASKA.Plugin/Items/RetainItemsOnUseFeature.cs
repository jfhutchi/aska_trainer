using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Items;

internal sealed class RetainItemsOnUseFeature() : NativeFeature("items.retain", "Add Items On Use (retain stack)")
{
    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(
        "A use-only decrement scope that preserves native effects and last-stack removal is not verified.");
}
