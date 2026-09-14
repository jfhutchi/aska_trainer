using System.Globalization;
using System.Text.RegularExpressions;

namespace HutchASKA.Core.Compatibility;

public static class SteamBuildInfo
{
    public static string? FromManifest(string manifest, string installDirectory)
    {
        var directory = Field(manifest, "installdir");
        var build = Field(manifest, "buildid");
        return string.Equals(directory, installDirectory, StringComparison.OrdinalIgnoreCase)
            && ulong.TryParse(build, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 ? build : null;
    }
    private static string? Field(string manifest, string key)
    {
        var matches = Regex.Matches(manifest, "\"" + key + "\"\\s+\"([^\"]*)\"", RegexOptions.CultureInvariant);
        return matches.Count == 1 ? matches[0].Groups[1].Value : null;
    }
}
