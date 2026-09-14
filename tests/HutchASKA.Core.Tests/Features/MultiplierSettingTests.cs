using HutchASKA.Core.Features;

namespace HutchASKA.Core.Tests.Features;

public sealed class MultiplierSettingTests
{
    [Fact]
    public void ClampsBoundsAndRestoresOne()
    {
        var setting = new MultiplierSetting(1, 5);
        setting.Value = -2;
        Assert.Equal(1, setting.Value);
        setting.Value = 9;
        Assert.Equal(5, setting.Value);
        setting.Reset();
        Assert.Equal(1, setting.Value);
        setting.Value = float.NaN;
        Assert.Equal(1, setting.Value);
    }
}
