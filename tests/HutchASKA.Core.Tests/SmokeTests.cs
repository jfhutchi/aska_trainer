namespace HutchASKA.Core.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void CoreAssembly_HasExpectedName()
    {
        Assert.Equal("HutchASKA.Core", typeof(CoreMarker).Assembly.GetName().Name);
    }
}
