namespace HutchASKA.Core.Crafting;

public readonly record struct BuildSupplyState(bool BuildEnabled, bool SupplyEnabled)
{
    public BuildSupplyState Restore(bool completed, bool currentBuild, bool currentSupply) => completed
        ? new(currentBuild, currentSupply)
        : new(currentBuild ? BuildEnabled : currentBuild, !currentSupply ? SupplyEnabled : currentSupply);
}

/// <summary>Limits a virtual supply answer to one read of the exact native container.</summary>
public sealed class SupplyReadScope(long container)
{
    private bool used;
    public bool TryConsume(long candidate)
    {
        if (used || container == 0 || candidate != container) return false;
        used = true;
        return true;
    }
}
