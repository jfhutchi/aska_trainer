namespace HutchASKA.Core.Player;

/// <summary>The additive weight and timer contracts used by ASKA's fishing coroutine.</summary>
public static class FishingAssistMath
{
    public static int NormalizeRareWeightPreset(int value) => value is 20 or 30 or 40 or 50 ? value : 1;

    public static bool UseBaseFishEligibility(bool nativeBaseFish, bool rareAnywhere) =>
        nativeBaseFish || rareAnywhere;

    public static float ScaleWeight(float native, bool special, float multiplier)
    {
        if (multiplier is not (1 or 20 or 30 or 40 or 50))
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        ValidateNonnegative(native);
        return Checked(special ? native * multiplier : native);
    }

    public static float EffectiveWeight(float chance, float baitBonus, float specialBonus)
    {
        ValidateNonnegative(chance);
        ValidateNonnegative(baitBonus);
        ValidateNonnegative(specialBonus);
        return Checked(chance + baitBonus + chance * specialBonus);
    }

    public static float WaitTarget(float native, float speed)
    {
        ValidateNonnegative(native);
        ValidateMultiplier(speed);
        return native / speed;
    }

    public static float RescaleTimer(float remainingOrElapsed, float previousScale, float nextScale)
    {
        ValidateNonnegative(remainingOrElapsed);
        if (!float.IsFinite(previousScale) || previousScale <= 0
            || !float.IsFinite(nextScale) || nextScale <= 0)
            throw new ArgumentOutOfRangeException(nameof(previousScale));
        return Checked((float)((double)remainingOrElapsed / previousScale * nextScale));
    }

    private static void ValidateMultiplier(float value)
    {
        if (!float.IsFinite(value) || value < 1 || value > 4)
            throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static void ValidateNonnegative(float value)
    {
        if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static float Checked(float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value), "Fishing value overflowed.");
        return value;
    }
}
