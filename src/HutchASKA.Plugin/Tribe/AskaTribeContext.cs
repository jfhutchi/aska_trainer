using System.Reflection;
using HarmonyLib;
using HutchASKA.Core.Features;
using HutchASKA.Core.Player;
using HutchASKA.Core.Tribe;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using Il2CppInterop.Runtime.InteropTypes;
using SandSailorStudio.Attributes;
using SSSGame;
using SSSGame.Combat;

namespace HutchASKA.Plugin.Tribe;

internal sealed class AskaTribeContext(IPlayerContext players, SinglePlayerGuard guard, Action<Exception> reportError) : ITribeContext, ITribeContextStatus
{
    private int nativeFailures;
    private string? nativeFault;
    public string? LastError { get; private set; }
    public bool HasNativeFailure { get; private set; }
    internal const string AgeUnavailable = "Age is unavailable: the native attribute measures remaining lifetime; safe age conversion/editing is unverified.";
    internal const string WarmthUnavailable = "Warmth is read-only: a safe native temperature range has not been verified.";
    internal static MethodInfo? DamageTarget() => AccessTools.DeclaredMethod(typeof(Villager), "TakeDamage", new[] { typeof(DamageData) });
    internal static CompatibilityResult ProbeCompatibility() =>
        typeof(PopulationManager).GetMethod("GetPopulation", Type.EmptyTypes) is not null
        && typeof(Villager).GetMethod("GetGuid", Type.EmptyTypes)?.ReturnType == typeof(string)
        && typeof(Villager).GetMethod("GetSurvival", Type.EmptyTypes)?.ReturnType == typeof(VillagerSurvival)
        && typeof(VariableAttribute).GetMethod("SetValue", new[] { typeof(float) })?.ReturnType == typeof(void)
        ? CompatibilityResult.Compatible() : CompatibilityResult.Incompatible("Required registered-villager identity or attribute API is unavailable.");

    public IReadOnlyList<string> GetCurrentVillagerIds()
    {
        try
        {
            var ids = ResolvePopulation().Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            LastError = null; HasNativeFailure = false;
            return ids;
        }
        catch (TribeUnavailableException error) { Unavailable(error); return Array.Empty<string>(); }
        catch (Exception error) { Fault(error); throw; }
    }

    public bool TrySnapshot(string stableId, out VillagerSnapshot? snapshot, out string? error)
    {
        snapshot = null;
        try
        {
            var villager = Resolve(stableId);
            var survival = villager.GetSurvival();
            if (!survival) throw new TribeUnavailableException("Villager survival is unavailable.");
            snapshot = new(stableId, villager.GetName() ?? stableId,
                Fraction(villager._healthVAttr, villager.MaxHealth), Fraction(survival._foodVAttr),
                Fraction(survival._waterVAttr), Fraction(survival._warmthVAttr), Fraction(survival._energyVAttr),
                Fraction(survival._restVariableAttribute), Fraction(villager._happinessVAttr, villager.HappinessCap), null);
            error = LastError = null; HasNativeFailure = false;
            return true;
        }
        catch (TribeUnavailableException failure) { error = failure.Message; Unavailable(failure); return false; }
        catch (Exception failure) { error = failure.Message; Fault(failure); return false; }
    }

    public bool TryApply(string stableId, VillagerEditRequest request, out string? error)
    {
        try
        {
            request = request.Clamp();
            if (request.Age is not null) throw new TribeUnavailableException(AgeUnavailable);
            if (request.WarmthFraction is not null) throw new TribeUnavailableException(WarmthUnavailable);
            var villager = Resolve(stableId);
            if (request.IsEmpty) { error = null; return true; }
            var survival = villager.GetSurvival();
            if (!survival) throw new TribeUnavailableException("Villager survival is unavailable.");
            // Validate every requested native range before making the first write.
            var writes = new List<(VariableAttribute Attribute, float Value)>();
            Add(writes, villager._healthVAttr, request.HealthFraction, villager.MaxHealth);
            Add(writes, survival._foodVAttr, request.FoodFraction);
            Add(writes, survival._waterVAttr, request.WaterFraction);
            Add(writes, survival._energyVAttr, request.EnergyFraction);
            Add(writes, survival._restVariableAttribute, request.RestFraction);
            Add(writes, villager._happinessVAttr, request.HappinessFraction, villager.HappinessCap);
            foreach (var write in writes)
            {
                RequireSession();
                if (!villager || villager.IsDead || !villager.HasAuthority)
                    throw new TribeUnavailableException("Villager became unavailable during editing; some earlier fields may have applied.");
                write.Attribute.SetValue(write.Value);
            }
            error = LastError = null; HasNativeFailure = false;
            return true;
        }
        catch (TribeUnavailableException failure) { error = failure.Message; Unavailable(failure); return false; }
        catch (ArgumentOutOfRangeException failure) { error = failure.Message; Unavailable(failure); return false; }
        catch (Exception failure) { error = failure.Message; Fault(failure); return false; }
    }

    public bool TryHeal(string stableId, out string? error) => TryApply(stableId, new(HealthFraction: 1), out error);

    public bool IsCurrentVillager(object candidate)
    {
        try
        {
            var villager = candidate is Il2CppObjectBase native ? native.TryCast<Villager>() : null;
            if (!villager) return false;
            var id = villager!.GetGuid();
            return !string.IsNullOrWhiteSpace(id) && ResolvePopulation().TryGetValue(id, out var current) && current == villager;
        }
        catch (TribeUnavailableException error) { Unavailable(error); return false; }
        catch (Exception error) { Fault(error); throw; }
    }

    private Villager Resolve(string id) => ResolvePopulation().TryGetValue(id, out var villager)
        ? villager : throw new TribeUnavailableException("Villager is no longer a live, owned member of the current tribe. Refresh the list.");

    private Dictionary<string, Villager> ResolvePopulation()
    {
        RequireSession();
        if (nativeFault is not null) throw new InvalidOperationException(nativeFault);
        if (!players.TryGetLocalPlayer(out var player)) throw new TribeUnavailableException("Local player is unavailable.");
        var population = GameObjectResolver.FindUnique<PopulationManager>();
        var settlement = GameObjectResolver.FindUnique<Settlement>();
        if (!population || population!.IsLoading || !settlement)
            throw new TribeUnavailableException("Current population or settlement is unavailable or ambiguous.");
        var result = new Dictionary<string, Villager>(StringComparer.Ordinal);
        var ambiguous = new HashSet<string>(StringComparer.Ordinal);
        var registered = population.GetPopulation();
        if (registered is null) throw new TribeUnavailableException("Registered population is unavailable.");
        foreach (var villager in registered)
        {
            if (!villager || villager.IsDead || !villager.HasAuthority || villager.teamId != player!.TeamId
                || villager._guestStation || villager.GetSettlement() != settlement) continue;
            var id = villager.GetGuid();
            if (string.IsNullOrWhiteSpace(id) || ambiguous.Contains(id)) continue;
            if (!result.TryAdd(id, villager)) { result.Remove(id); ambiguous.Add(id); }
        }
        return result;
    }

    private void RequireSession()
    {
        var decision = guard.Refresh();
        if (!decision.Allowed) throw new TribeUnavailableException(decision.Reason ?? "Single-player state is unavailable.");
    }

    private static (float Min, float Max) Range(VariableAttribute? attribute, float? cap)
    {
        if (attribute is null) throw new InvalidOperationException("Native villager attribute is unavailable.");
        var min = attribute.min;
        var max = attribute.max;
        if (cap is { } limit)
        {
            if (!float.IsFinite(limit)) throw new InvalidOperationException("Native attribute cap is not finite.");
            max = Math.Min(max, limit);
        }
        if (!float.IsFinite(min) || !float.IsFinite(max) || max <= min)
            throw new InvalidOperationException("Native villager attribute range is unavailable or invalid.");
        return (min, max);
    }

    private static float Fraction(VariableAttribute attribute, float? cap = null)
    {
        var (min, max) = Range(attribute, cap);
        var value = attribute.GetValue();
        if (!float.IsFinite(value)) throw new InvalidOperationException("Native attribute value is not finite.");
        return (float)Math.Clamp(((double)value - min) / ((double)max - min), 0, 1);
    }

    private static void Add(List<(VariableAttribute, float)> writes, VariableAttribute attribute, float? fraction, float? cap = null)
    {
        if (fraction is null) return;
        var (min, max) = Range(attribute, cap);
        writes.Add((attribute, AttributeMath.ValueAtFraction(min, max, fraction.Value)));
    }

    private void Unavailable(Exception error)
    {
        LastError = error.Message;
        HasNativeFailure = false;
    }

    private void Fault(Exception error)
    {
        HasNativeFailure = true;
        if (nativeFault is null)
        {
            nativeFailures++;
            reportError(error);
            if (nativeFailures >= 3) nativeFault = "Tribe adapter stopped after native errors; restart ASKA after correcting the error.";
        }
        LastError = nativeFault ?? error.Message;
    }

    private sealed class TribeUnavailableException(string message) : Exception(message);
}
