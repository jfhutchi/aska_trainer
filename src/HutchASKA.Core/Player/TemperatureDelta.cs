namespace HutchASKA.Core.Player;

public static class TemperatureDelta
{
    public static float PreserveWarmth(float delta)
    {
        Validate(delta);
        return Math.Max(0, delta);
    }

    public static float PreventFrost(float delta)
    {
        Validate(delta);
        return Math.Min(0, delta);
    }

    private static void Validate(float delta)
    {
        if (!float.IsFinite(delta))
            throw new ArgumentOutOfRangeException(nameof(delta), "Native temperature change is not finite.");
    }
}
