namespace HutchASKA.Core.Features;

public static class FeatureReset
{
    public static void ResetAll(IEnumerable<ITrainerFeature> features, params MultiplierSetting[] multipliers)
    {
        foreach (var feature in features) feature.Reset();
        // Controller settings are pure state, including when native feature cleanup remains faulted.
        foreach (var multiplier in multipliers) multiplier.Reset();
    }
}
