namespace HutchASKA.Core.Compatibility;

public enum SinglePlayerTransition
{
    None,
    FirstConfirmed,
    LeftConfirmed,
    ReturnedConfirmed
}

public sealed class SinglePlayerTransitionTracker
{
    private bool observed;
    private bool allowed;
    private bool wasConfirmed;

    public SinglePlayerTransition Observe(bool isAllowed)
    {
        if (!observed)
        {
            observed = true;
            allowed = isAllowed;
            wasConfirmed = isAllowed;
            return isAllowed ? SinglePlayerTransition.FirstConfirmed : SinglePlayerTransition.None;
        }

        if (allowed == isAllowed) return SinglePlayerTransition.None;
        allowed = isAllowed;
        if (!isAllowed) return wasConfirmed ? SinglePlayerTransition.LeftConfirmed : SinglePlayerTransition.None;
        if (wasConfirmed) return SinglePlayerTransition.ReturnedConfirmed;
        wasConfirmed = true;
        return SinglePlayerTransition.FirstConfirmed;
    }
}
