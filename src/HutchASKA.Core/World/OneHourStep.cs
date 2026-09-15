namespace HutchASKA.Core.World;

public static class OneHourStep
{
    public static bool TryGetTarget(float currentHour, int direction, out float nativeTarget, out string? error)
    {
        nativeTarget = currentHour;
        if (direction is not (-1 or 1))
        {
            error = "Choose a one-hour forward or backward adjustment.";
            return false;
        }
        if (!float.IsFinite(currentHour) || currentHour < 0 || currentHour >= 24)
        {
            error = "The current native clock hour is unavailable or invalid.";
            return false;
        }
        if (currentHour + direction < 0)
        {
            error = "Cannot move back across midnight on this ASKA build.";
            return false;
        }
        // Keep values >=24: the verified native setter uses them to advance the day.
        nativeTarget = currentHour + direction;
        error = null;
        return true;
    }
}
