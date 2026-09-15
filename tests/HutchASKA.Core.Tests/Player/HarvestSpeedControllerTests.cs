using HutchASKA.Core.Player;

namespace HutchASKA.Core.Tests.Player;

public sealed class HarvestSpeedControllerTests
{
    [Theory]
    [InlineData("Chop", "Chop", "Chop", 1, 1, true)]
    [InlineData("ChopGround", "ChopGround", "ChopGround", 3, 3, true)]
    [InlineData("Chop", "Chop", "Attack", 1, 1, false)]
    [InlineData("Chop", "Chop", "Chop", 1, 0, false)]
    [InlineData("Chop", "ChopGround", "Chop", 1, 1, false)]
    [InlineData("", "Chop", "Chop", 1, 1, false)]
    [InlineData("Chop", "Chop", "Chop", 0, 0, false)]
    public void RequiresTheSpecificLiveHarvestAction(string session, string moveset, string geometry,
        int expected, int current, bool allowed) =>
        Assert.Equal(allowed, HarvestSpeedController.HasActiveHarvestAction(session, moveset, geometry, expected, current));

    [Fact]
    public void PresetChangesDoNotCompoundAndRestoreTheNativeBaseline()
    {
        var controller = new HarvestSpeedController();
        Assert.Equal(1.5f, controller.Target(10, .75f, 2));
        controller.RecordApplied(1.5f);
        Assert.Equal(3, controller.Target(10, 1.5f, 4));
        controller.RecordApplied(3);
        Assert.Equal(.75f, controller.RestoreTarget(10, 3));
    }

    [Fact]
    public void ExternalChangesArePreservedAndDifferentTargetsCannotInheritOwnership()
    {
        var controller = new HarvestSpeedController();
        controller.RecordApplied(controller.Target(10, 1, 2));
        Assert.Null(controller.RestoreTarget(11, 2));
        Assert.Null(controller.RestoreTarget(10, .5f));
        Assert.Throws<InvalidOperationException>(() => controller.Target(11, 1, 2));
        Assert.Equal(1.5f, controller.Target(10, .5f, 3));
        controller.Clear();
        Assert.Equal(2, controller.Target(11, 1, 2));
    }

    [Theory]
    [InlineData(1, 12)]
    [InlineData(2, 6)]
    [InlineData(3, 4)]
    [InlineData(4, 3)]
    public void GatherPresetsDivideNativeWorkWithoutChangingProgress(float multiplier, float threshold) =>
        Assert.Equal(threshold, HarvestSpeedController.GatherThreshold(12, multiplier));

    [Fact]
    public void DisablingGatherKeepsTheSameCompletionFraction() =>
        Assert.Equal(6, HarvestSpeedController.RestoreGatherProgress(1.5f, 3, 12));

    [Fact]
    public void APausedAnimatorRemainsPaused()
    {
        var controller = new HarvestSpeedController();
        Assert.Equal(0, controller.Target(10, 0, 4));
    }
}
