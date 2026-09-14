namespace HutchASKA.Core.World;

public static class TimeAdjustment
{
    // Callers must supply a verified period in the same units as current and delta.
    public static double Wrap(double current, double delta, double period)
    {
        if (!double.IsFinite(current) || !double.IsFinite(delta) || !double.IsFinite(period) || period <= 0)
            throw new ArgumentOutOfRangeException(nameof(period));
        var wrapped = ((current % period) + (delta % period)) % period;
        return wrapped < 0 ? wrapped + period : wrapped;
    }
}
