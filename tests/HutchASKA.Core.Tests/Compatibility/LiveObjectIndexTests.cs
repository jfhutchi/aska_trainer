using HutchASKA.Core.Compatibility;

namespace HutchASKA.Core.Tests.Compatibility;

public sealed class LiveObjectIndexTests
{
    private sealed class Candidate { public bool Active = true; }

    [Fact]
    public void RepeatedLookups_DiscoverOnlyOnceButCheckLivenessEveryTime()
    {
        var scans = 0;
        var candidate = new Candidate();
        var index = new LiveObjectIndex<Candidate>(() => { scans++; return new[] { candidate }; }, x => x.Active);
        for (var i = 0; i < 1000; i++) Assert.Same(candidate, index.FindUnique());
        candidate.Active = false;
        Assert.Null(index.FindUnique());
        Assert.Equal(1, scans);
    }

    [Fact]
    public void Invalidation_DiscoversNewObjectsAndRejectsAmbiguity()
    {
        var candidates = new List<Candidate> { new() };
        var index = new LiveObjectIndex<Candidate>(() => candidates.ToArray(), x => x.Active);
        Assert.Same(candidates[0], index.FindUnique());
        candidates.Add(new Candidate());
        index.Invalidate();
        Assert.Null(index.FindUnique());
        candidates[0].Active = false;
        Assert.Same(candidates[1], index.FindUnique());
    }

    [Fact]
    public void InactiveCandidatesAreRetainedSoActivationDoesNotNeedAnotherScan()
    {
        var first = new Candidate();
        var second = new Candidate { Active = false };
        var index = new LiveObjectIndex<Candidate>(() => new[] { first, second }, x => x.Active);
        Assert.Same(first, index.FindUnique());
        second.Active = true;
        Assert.Null(index.FindUnique());
        first.Active = false;
        Assert.Same(second, index.FindUnique());
    }
}
