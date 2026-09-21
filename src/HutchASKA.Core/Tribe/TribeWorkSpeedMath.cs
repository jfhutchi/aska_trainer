namespace HutchASKA.Core.Tribe;

public static class TribeWorkSpeedMath
{
    public static float ScaleCoefficient(float value, float multiplier)
    {
        ValidateMultiplier(multiplier);
        RequireFinite(value);
        return Checked(value * multiplier);
    }

    public static float HarvestBaseDamage(float baseline, float weaponDamage, float attributeBonus,
        float attributeMultiplier, float nativeDamageMultiplier, float multiplier)
    {
        ValidateMultiplier(multiplier);
        RequireFinite(baseline);
        RequireFinite(weaponDamage);
        RequireFinite(attributeBonus);
        RequireFinite(attributeMultiplier);
        RequireFinite(nativeDamageMultiplier);
        // The native multiplier also controls proficiency and tool wear; preserve it.
        var nativeWork = Checked((1 + attributeBonus * attributeMultiplier) * weaponDamage * nativeDamageMultiplier + baseline);
        if (nativeWork <= 0) return baseline;
        return Checked(baseline + (multiplier - 1) * nativeWork);
    }

    public static float GatherRemaining(float remaining, float baseline, float attributeBonus,
        float attributeMultiplier, float deltaTime, float multiplier)
    {
        ValidateMultiplier(multiplier);
        RequireFinite(remaining);
        RequireFinite(baseline);
        RequireFinite(attributeBonus);
        RequireFinite(attributeMultiplier);
        RequireFinite(deltaTime);
        if (deltaTime < 0) throw new ArgumentOutOfRangeException(nameof(deltaTime));
        var rate = Checked(baseline + attributeBonus * attributeMultiplier);
        if (remaining <= 0 || rate <= 0) return remaining;
        // Native Update still subtracts one normal tick and decides when a charge completes.
        return Checked(remaining - Checked(rate * deltaTime * (multiplier - 1)));
    }

    private static void ValidateMultiplier(float multiplier)
    {
        if (!float.IsFinite(multiplier) || multiplier < 1 || multiplier > 5)
            throw new ArgumentOutOfRangeException(nameof(multiplier));
    }

    private static void RequireFinite(float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static float Checked(float value) => float.IsFinite(value) ? value
        : throw new InvalidOperationException("Scaled tribe work is not finite.");
}
