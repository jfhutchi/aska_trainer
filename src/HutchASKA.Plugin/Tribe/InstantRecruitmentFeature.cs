using HutchASKA.Core.Features;
using HutchASKA.Plugin.Infrastructure;

namespace HutchASKA.Plugin.Tribe;

internal sealed class InstantRecruitmentFeature() : NativeFeature("tribe.recruitment", "Instant Normal Recruitment")
{
    public override CompatibilityResult ProbeCompatibility() => CompatibilityResult.Incompatible(
        "Normal recruitment timer units, completion and rearm are unverified; no timer or spawning changes are applied.");
}
