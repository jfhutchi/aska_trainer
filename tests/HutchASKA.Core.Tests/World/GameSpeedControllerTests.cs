using HutchASKA.Core.World;

namespace HutchASKA.Core.Tests.World;

public sealed class GameSpeedControllerTests
{
    [Fact]
    public void RespectsPauseAndAdoptsNativeResumeBaseline()
    {
        var controller = new GameSpeedController();
        Assert.Null(controller.Target(0, 2));
        Assert.Null(controller.Baseline);
        Assert.Equal(2, controller.Target(1, 2));
        controller.RecordApplied(2);
        Assert.Null(controller.Target(0, 2));
        Assert.Null(controller.RestoreTarget(0));
        Assert.Equal(2, controller.Target(1, 2));
        controller.RecordApplied(2);
        Assert.Equal(1, controller.RestoreTarget(2));
    }
    [Fact]
    public void PreservesNonUnitBaselineAcrossPresetChanges()
    {
        var controller = new GameSpeedController();
        Assert.Equal(1, controller.Target(.5f, 2));
        controller.RecordApplied(1);
        Assert.Equal(2.5f, controller.Target(1, 5));
        controller.RecordApplied(2.5f);
        Assert.Equal(.5f, controller.RestoreTarget(2.5f));
    }
}
