using BepInEx;
using HutchASKA.Core.Compatibility;

namespace HutchASKA.Plugin.Infrastructure;

internal static class SteamBuildReader
{
    public static string Detect(Action<Exception> reportError)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Paths.GameRootPath) || !Path.IsPathFullyQualified(Paths.GameRootPath))
                return "Unavailable (game installation path is unavailable)";
            var gameRoot = Path.TrimEndingDirectorySeparator(Paths.GameRootPath);
            var steamApps = Directory.GetParent(gameRoot)?.Parent;
            if (steamApps is null) return "Unavailable (Steam library location not found)";
            var manifest = Path.Combine(steamApps.FullName, "appmanifest_1898300.acf");
            if (!File.Exists(manifest)) return "Unavailable (matching Steam manifest not found)";
            return SteamBuildInfo.FromManifest(File.ReadAllText(manifest), Path.GetFileName(gameRoot))
                ?? "Unavailable (Steam manifest does not identify this installation)";
        }
        catch (IOException error) { reportError(error); return "Unavailable (Steam manifest read failed)"; }
        catch (UnauthorizedAccessException error) { reportError(error); return "Unavailable (Steam manifest access denied)"; }
    }
}
