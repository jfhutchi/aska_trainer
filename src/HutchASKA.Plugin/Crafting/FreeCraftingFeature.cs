using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Crafting;

internal sealed class FreeCraftingFeature() : NativeFeature("crafting.free", "Ignore Crafting Materials")
{
    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(
        "Material-only requirements and consumption have not been separated from native unlock/completion checks.");
}
