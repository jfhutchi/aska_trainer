namespace HutchASKA.Core.Input;

public sealed class HotkeyMap
{
    private readonly Dictionary<string, string> keys = new(StringComparer.OrdinalIgnoreCase);
    public string Get(string id) => keys.TryGetValue(id, out var key) ? key : "";
    public IReadOnlyList<TrainerHotkey> Snapshot() => keys.Select(k => new TrainerHotkey(k.Key, k.Value)).ToArray();
    public void Set(string id, string key)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Feature ID is required.", nameof(id));
        key = key.Trim().ToUpperInvariant();
        if (key.Length > 0 && keys.Any(k => !StringComparer.OrdinalIgnoreCase.Equals(k.Key, id) && k.Value == key))
            throw new InvalidOperationException($"Hotkey '{key}' is already assigned.");
        keys[id] = key;
    }
    public static HotkeyMap CreateDefaults()
    {
        var map = new HotkeyMap();
        map.Set("menu", "F8");
        map.Set("god-mode", "F1");
        map.Set("stamina", "F2");
        map.Set("freeze-time", "F5");
        return map;
    }
}
