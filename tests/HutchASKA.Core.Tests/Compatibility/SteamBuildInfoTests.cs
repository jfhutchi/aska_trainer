using HutchASKA.Core.Compatibility;

namespace HutchASKA.Core.Tests.Compatibility;

public sealed class SteamBuildInfoTests
{
    [Fact]
    public void RequiresMatchingInstallDirectoryAndUniqueBuildId()
    {
        const string manifest = "\"AppState\" { \"installdir\" \"ASKA\" \"buildid\" \"25186770\" }";
        Assert.Equal("25186770", SteamBuildInfo.FromManifest(manifest, "aska"));
        Assert.Null(SteamBuildInfo.FromManifest(manifest, "AnotherGame"));
        Assert.Null(SteamBuildInfo.FromManifest(manifest + "\"buildid\" \"1\"", "ASKA"));
    }
    [Theory]
    [InlineData("\"installdir\" \"ASKA\"")]
    [InlineData("\"installdir\" \"ASKA\" \"buildid\" \"unknown\"")]
    [InlineData("\"installdir\" \"ASKA\" \"buildid\" \"0\"")]
    public void UnavailableBuildIsNotReplacedWithApplicationVersion(string manifest) =>
        Assert.Null(SteamBuildInfo.FromManifest(manifest, "ASKA"));
}
