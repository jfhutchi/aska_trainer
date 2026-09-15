namespace HutchASKA.Core.Compatibility;

/// <summary>Caches discovery, never liveness or uniqueness; invalidate on object creation/destruction.</summary>
public sealed class LiveObjectIndex<T>(Func<IReadOnlyList<T>> discover, Func<T, bool> isActive) where T : class
{
    private IReadOnlyList<T>? candidates;

    public void Invalidate() => candidates = null;

    public T? FindUnique()
    {
        candidates ??= discover();
        T? match = null;
        foreach (var candidate in candidates)
        {
            if (!isActive(candidate)) continue;
            if (match is not null) return null;
            match = candidate;
        }
        return match;
    }
}
