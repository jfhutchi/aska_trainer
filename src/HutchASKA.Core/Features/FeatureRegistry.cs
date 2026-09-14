namespace HutchASKA.Core.Features;

public sealed class FeatureRegistry
{
    private readonly Dictionary<string, ITrainerFeature> byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ITrainerFeature> ordered = new();

    public void Register(ITrainerFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        if (string.IsNullOrWhiteSpace(feature.Id)) throw new ArgumentException("Feature ID is required.", nameof(feature));
        if (!byId.TryAdd(feature.Id, feature)) throw new InvalidOperationException($"Feature '{feature.Id}' is already registered.");
        ordered.Add(feature);
    }

    public ITrainerFeature? Find(string id) => byId.TryGetValue(id, out var feature) ? feature : null;
    public IReadOnlyList<ITrainerFeature> Snapshot() => ordered.ToArray();
}
