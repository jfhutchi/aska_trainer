namespace HutchASKA.Core.Items;

/// <summary>Authorizes one consumed-item decrement only after native use effects completed.</summary>
public sealed class ConsumableRetentionScope(long itemIdentity, long containerIdentity)
{
    private bool effectsApplied;
    private bool retained;

    public void EffectsApplied(long item)
    {
        if (item == itemIdentity) effectsApplied = true;
    }

    public bool TryRetain(long item, long container, int amount, bool defaultContext)
    {
        if (retained || !effectsApplied || itemIdentity == 0 || containerIdentity == 0
            || item != itemIdentity || container != containerIdentity || amount != 1 || !defaultContext)
            return false;
        retained = true;
        return true;
    }
}
