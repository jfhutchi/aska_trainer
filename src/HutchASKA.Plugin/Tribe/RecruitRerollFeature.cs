using HutchASKA.Core.Features;
using HutchASKA.Core.Tribe;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using SandSailorStudio.UI;
using SSSGame;
using SSSGame.UI;

namespace HutchASKA.Plugin.Tribe;

internal sealed record RecruitChoicePreview(int Id, string Name, string Traits);

internal sealed class RecruitRerollFeature(SinglePlayerGuard guard)
    : NativeActionFeature("tribe.reroll", "Reroll Recruit Traits")
{
    private sealed record Candidate(RecruitChoiceStamp Stamp, Villager.DescriptionData Description,
        RecruitChoicePreview Preview);
    private readonly Dictionary<int, Candidate> candidates = new();
    private Candidate? stagedCandidate;
    private Villager.DescriptionData stagedDescription;

    public override CompatibilityResult ProbeCompatibility() =>
        typeof(PerksManager).GetMethod("GetRandomPerks", new[] { typeof(int),
            typeof(Il2CppSystem.Collections.Generic.List<StatusEffectModifierTable>).MakeByRefType(),
            typeof(SandSailorStudio.RNG.IRandomGenerator) })?.ReturnType == typeof(void)
        && typeof(Villager.PerksData).GetConstructor(new[] { typeof(Il2CppSystem.Collections.Generic.List<StatusEffectModifierTable>) }) is not null
        && typeof(Fusion.NetworkArray<Villager.DescriptionData>).GetMethod("Set", new[] { typeof(int), typeof(Villager.DescriptionData) }) is not null
        && typeof(VillagerOutlet).GetProperty("upcomingVillagerChoices") is not null
        && typeof(VillagerOutlet).GetProperty("_SpawnPending")?.PropertyType == typeof(Fusion.NetworkBool)
        && typeof(PerkModifierTable).GetMethod("IsCompatibleWith", new[] { typeof(PerkModifierTable) })?.ReturnType == typeof(bool)
        ? CompatibilityResult.Compatible()
        : CompatibilityResult.Incompatible("Recruit choices or normal trait generation are unavailable.");

    internal void Clear()
    {
        candidates.Clear();
        stagedCandidate = null;
        stagedDescription = default;
    }

    public override void Reset() { Clear(); base.Reset(); }

    internal bool TryRefresh(out IReadOnlyList<RecruitChoicePreview> choices, out string? error)
    {
        Clear();
        var result = new List<RecruitChoicePreview>();
        var success = Execute(() =>
        {
            var (population, settlement) = RequireContext();
            var outlets = population._villagerOutlets;
            if (outlets is null || outlets.Count > 128)
                throw new RecruitUnavailableException("Recruiting buildings are unavailable. Try again after the village has loaded.");
            for (var i = 0; i < outlets.Count; i++)
            {
                var outlet = outlets[i];
                if (!IsEligible(outlet, settlement)) continue;
                var array = outlet.upcomingVillagerChoices;
                var count = outlet.villagerChoicesCount;
                // The final network slot belongs exclusively to the lost-villager option.
                if (count < 2 || count >= array.Length) continue;
                for (var slot = 0; slot < count; slot++)
                {
                    var data = array.Get(slot);
                    var definition = GetDefinition(population, data);
                    if (definition.isGolem || !Perks(data.perks).IsValid()) continue;
                    var name = definition.GetName(data.nameID);
                    var preview = new RecruitChoicePreview(result.Count,
                        $"{name} (building {i + 1}, choice {slot + 1})", Describe(data.perks));
                    candidates.Add(preview.Id, new(Stamp(population, settlement, outlet, slot, data), data, preview));
                    result.Add(preview);
                }
            }
            if (result.Count == 0)
                throw new RecruitUnavailableException("No ordinary recruits are ready to reroll. This control needs a building that offers multiple ordinary recruits. Finish any active summon, then refresh before choosing your next recruit.");
        }, out error);
        if (!success) { Clear(); result.Clear(); }
        choices = result;
        return success;
    }

    internal bool TryReroll(int id, out RecruitChoicePreview? preview, out string? error)
    {
        stagedCandidate = null;
        RecruitChoicePreview? result = null;
        var success = Execute(() =>
        {
            if (!candidates.TryGetValue(id, out var candidate))
                throw new RecruitUnavailableException("Refresh the recruits and choose one first.");
            var (population, _, _) = Resolve(candidate);
            var definition = GetDefinition(population, candidate.Description);
            var manager = definition.character?.GetComponent<PerksManager>();
            if (!manager || !manager!.PerkType)
                throw new RecruitUnavailableException("This recruit's starting traits are unavailable.");
            // This is precisely the perk-only substep of native GenerateDescriptionData.
            manager.GetRandomPerks(5, out var tables, null);
            ValidateTables(tables, manager.PerkType);
            var proposed = candidate.Description;
            proposed.perks = new Villager.PerksData(tables);
            if (!Perks(proposed.perks).IsValid())
                throw new InvalidOperationException("Normal recruit generation returned invalid trait slots.");
            result = candidate.Preview with { Traits = Describe(proposed.perks) };
            Resolve(candidate);
            stagedDescription = proposed;
            stagedCandidate = candidate;
        }, out error);
        preview = success ? result : null;
        return success;
    }

    internal bool TryApply(out string? error)
    {
        var candidate = stagedCandidate;
        stagedCandidate = null;
        var success = Execute(() =>
        {
            if (candidate is null) throw new RecruitUnavailableException("Reroll a recruit to preview new traits first.");
            var (_, _, outlet) = Resolve(candidate);
            // Do not invoke the selection RPC: selection and its lost-villager side effects
            // remain exclusively in the game's normal confirmation flow.
            outlet.upcomingVillagerChoices.Set(candidate.Stamp.Slot, stagedDescription);
            var actual = outlet.upcomingVillagerChoices.Get(candidate.Stamp.Slot);
            if (!SameDescription(actual, stagedDescription))
                throw new InvalidOperationException("Recruit traits could not be confirmed. Refresh and inspect the recruit before summoning.");
        }, out error);
        Clear();
        return success;
    }

    private bool Execute(Action action, out string? error)
    {
        string? unavailable = null;
        var ran = RunOnce(() =>
        {
            try { action(); }
            catch (RecruitUnavailableException failure) { unavailable = failure.Message; }
        }, out error);
        error ??= unavailable;
        return ran && unavailable is null;
    }

    private (PopulationManager Population, Settlement Settlement) RequireContext()
    {
        var decision = guard.Refresh();
        if (!decision.Allowed) throw new RecruitUnavailableException(decision.Reason ?? "Load a single-player village first.");
        if (Menu.IsOpen<SelectVillagerMenu>())
            throw new RecruitUnavailableException("Close the game's recruit selection screen, then try again. Reopen it after applying your new traits.");
        var population = GameObjectResolver.FindUnique<PopulationManager>();
        var settlement = GameObjectResolver.FindUnique<Settlement>();
        if (!population || population!.IsLoading || !settlement)
            throw new RecruitUnavailableException("The current village is still loading or unavailable.");
        return (population, settlement!);
    }

    private (PopulationManager Population, Settlement Settlement, VillagerOutlet Outlet) Resolve(Candidate candidate)
    {
        var (population, settlement) = RequireContext();
        var outlets = population._villagerOutlets;
        if (outlets is null || outlets.Count > 128)
            throw new RecruitUnavailableException("Recruiting buildings are unavailable. Refresh the recruits.");
        VillagerOutlet? found = null;
        for (var i = 0; i < outlets.Count; i++)
        {
            var outlet = outlets[i];
            if (!outlet || outlet.GetInstanceID() != candidate.Stamp.Outlet) continue;
            if (found is not null) throw new RecruitUnavailableException("The recruiting building is ambiguous. Refresh the recruits.");
            found = outlet;
        }
        if (!found || !IsEligible(found!, settlement))
            throw new RecruitUnavailableException("This recruit is no longer waiting to be chosen. Refresh the recruits.");
        var current = found!.upcomingVillagerChoices;
        if (found.villagerChoicesCount >= current.Length || candidate.Stamp.Slot >= found.villagerChoicesCount)
            throw new RecruitUnavailableException("The available recruits have changed. Refresh the recruits.");
        var stamp = Stamp(population, settlement, found, candidate.Stamp.Slot, current.Get(candidate.Stamp.Slot));
        if (!candidate.Stamp.CanApplyTo(stamp, found._SpawnPending, found.IsActive,
            found.activationInteraction.IsActive, found._chosenLostVillager))
            throw new RecruitUnavailableException("This recruit changed after the preview. Refresh before rerolling again.");
        return (population, settlement, found);
    }

    private static bool IsEligible(VillagerOutlet outlet, Settlement settlement)
    {
        if (!outlet || !outlet.isActiveAndEnabled || outlet.Object == null || !outlet.Object.IsValid
            || !outlet.Object.HasStateAuthority || outlet._session == null || !outlet._session.isMaster
            || outlet._SpawnPending || outlet.IsActive || outlet._chosenLostVillager
            || !outlet.activationInteraction || outlet.activationInteraction.IsActive) return false;
        var owner = outlet.OwnerStructure;
        return owner && owner.IsValid && owner.IsActive && !owner.IsDead && !owner.Dismantled
            && owner.Settlement == settlement;
    }

    private static VillagerCharacterDefinition GetDefinition(PopulationManager population, Villager.DescriptionData data)
    {
        var definitions = population.villagerDefinitions;
        if (definitions is null || data.definitionID < 0 || data.definitionID >= definitions.Length
            || !definitions[data.definitionID] || data.nameID < 0)
            throw new RecruitUnavailableException("A recruit's identity is unavailable. Refresh after the village finishes loading.");
        return definitions[data.definitionID];
    }

    private static void ValidateTables(Il2CppSystem.Collections.Generic.List<StatusEffectModifierTable> tables, StatusEffectType type)
    {
        if (tables is null || tables.Count is < 1 or > 5)
            throw new InvalidOperationException("Normal recruit generation returned no usable traits.");
        var ids = new HashSet<int>();
        for (var i = 0; i < tables.Count; i++)
        {
            var table = tables[i];
            if (!table || table.effectType != type || table.id < 0 || !ids.Add(table.id))
                throw new InvalidOperationException("Normal recruit generation returned an invalid or repeated trait.");
            var perk = table.TryCast<PerkModifierTable>();
            if (!perk) continue;
            for (var j = 0; j < i; j++)
            {
                var other = tables[j].TryCast<PerkModifierTable>();
                if (other && (!perk!.IsCompatibleWith(other!) || !other!.IsCompatibleWith(perk!)))
                    throw new InvalidOperationException("Normal recruit generation returned conflicting traits.");
            }
        }
    }

    private static string Describe(Villager.PerksData perks)
    {
        var tables = perks.ToStatusEffectList();
        if (tables is null || tables.Count is < 1 or > 5)
            throw new RecruitUnavailableException("The recruit's trait descriptions are unavailable.");
        var lines = new List<string>();
        foreach (var table in tables)
        {
            if (!table) throw new RecruitUnavailableException("A recruit trait is unavailable.");
            lines.Add($"{table.Name}: {table.Description}");
        }
        return string.Join("\n", lines);
    }

    private static RecruitChoiceStamp Stamp(PopulationManager population, Settlement settlement,
        VillagerOutlet outlet, int slot, Villager.DescriptionData data) => new(
        population.GetInstanceID(), settlement.GetInstanceID(), outlet.GetInstanceID(), slot,
        outlet.villagerChoicesCount, data.definitionID, data.nameID,
        Appearance(data.appearance), data.portraitCameraIndex, Perks(data.perks));

    private static RecruitAppearanceStamp Appearance(CharacterAppearance.Appearance appearance) =>
        new(appearance.hairId, appearance.facialFeatureId,
            BitConverter.SingleToInt32Bits(appearance.hairColorValue),
            BitConverter.SingleToInt32Bits(appearance.skinColorValue),
            BitConverter.SingleToInt32Bits(appearance.tattooColorValue), appearance.irisId, appearance.tattooId);

    private static RecruitPerksStamp Perks(Villager.PerksData perks) =>
        new(perks.perk_0, perks.perk_1, perks.perk_2, perks.perk_3, perks.perk_4);

    private static bool SameDescription(Villager.DescriptionData first, Villager.DescriptionData second) =>
        first.definitionID == second.definitionID && first.nameID == second.nameID
        && Appearance(first.appearance) == Appearance(second.appearance) && first.portraitCameraIndex == second.portraitCameraIndex
        && Perks(first.perks) == Perks(second.perks);

    private sealed class RecruitUnavailableException(string message) : Exception(message);
}
