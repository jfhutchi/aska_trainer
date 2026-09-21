namespace HutchASKA.Core.Player;

public static class SkillGainMath
{
    public static float ScaleAward(float amount, float multiplier)
    {
        if (!float.IsFinite(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!float.IsFinite(multiplier) || multiplier < 1 || multiplier > 5)
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        if (amount <= 0) return amount;
        var result = amount * multiplier;
        if (!float.IsFinite(result)) throw new InvalidOperationException("Skill experience award overflowed.");
        return result;
    }
}
