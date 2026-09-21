namespace HutchASKA.Core.Player;

public readonly record struct BuildWorkCoefficients(float BaseWork, float AttributeMultiplier)
{
    public BuildWorkCoefficients Scale(float multiplier)
    {
        if (!float.IsFinite(BaseWork) || !float.IsFinite(AttributeMultiplier))
            throw new ArgumentOutOfRangeException(nameof(BaseWork), "Native building coefficients must be finite.");
        if (!float.IsFinite(multiplier) || multiplier < 1 || multiplier > 4)
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        var baseline = BaseWork * multiplier;
        var attributeMultiplier = AttributeMultiplier * multiplier;
        if (!float.IsFinite(baseline) || !float.IsFinite(attributeMultiplier))
            throw new InvalidOperationException("Scaled building coefficients are not finite.");
        return new(baseline, attributeMultiplier);
    }
}
