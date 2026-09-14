param([string]$AskaGameDir = $env:ASKA_GAME_DIR)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($AskaGameDir)) {
    $steam = (Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
    $candidates = @()
    if ($steam) {
        $libraries = Join-Path $steam 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $libraries) {
            foreach ($match in [regex]::Matches((Get-Content -LiteralPath $libraries -Raw), '"path"\s+"([^"]+)"')) {
                $library = $match.Groups[1].Value.Replace('\\', '\')
                $candidate = Join-Path $library 'steamapps\common\ASKA'
                if (Test-Path -LiteralPath (Join-Path $candidate 'ASKA.exe')) { $candidates += $candidate }
            }
        }
    }
    $candidates = @($candidates | Select-Object -Unique)
    if ($candidates.Count -ne 1) { throw 'ASKA_GAME_DIR is not set and discovery is ambiguous or unavailable. Pass -AskaGameDir.' }
    $AskaGameDir = $candidates[0]
}
$resolved = (Resolve-Path -LiteralPath $AskaGameDir).Path
foreach ($relative in @('ASKA.exe', 'BepInEx\core', 'BepInEx\interop\Assembly-CSharp.dll', 'BepInEx\interop\SandSailorStudio.dll')) {
    $required = Join-Path $resolved $relative
    if (-not (Test-Path -LiteralPath $required)) { throw "Required ASKA/BepInEx path not found: $required" }
}
$env:ASKA_GAME_DIR = $resolved
dotnet build (Join-Path $PSScriptRoot '..\src\HutchASKA.Plugin\HutchASKA.Plugin.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw "Plugin build failed with exit code $LASTEXITCODE" }
