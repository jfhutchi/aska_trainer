using HutchASKA.Core.Features;

namespace HutchASKA.Core.Input;

public static class FeatureInputActions
{
    public static bool TryToggle(ITrainerFeature feature)
    {
        if (feature.State is FeatureState.Faulted or FeatureState.Incompatible) return false;
        if (feature.State != FeatureState.Enabled) return feature.TryEnable();
        feature.Disable();
        return true;
    }

    public static void EnsureEnabled(ITrainerFeature feature)
    {
        // Blocked is recoverable after session discovery; terminal failures require a restart.
        if (feature.State is FeatureState.Disabled or FeatureState.Blocked) feature.TryEnable();
    }
}
