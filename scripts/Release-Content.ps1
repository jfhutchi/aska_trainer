#Requires -Version 7.0

function Get-HutchReleaseFiles {
    @('HutchASKA.Plugin.dll', 'HutchASKA.Core.dll', 'README.txt', 'LICENSE',
      'THIRD_PARTY_NOTICES.md', 'BUILDINFO.txt', 'licenses/BepInEx-LGPL-2.1.txt',
      'licenses/HarmonyX-MIT.txt', 'licenses/Il2CppInterop-LGPL-3.0.txt')
}

function Assert-HutchReleaseContent([string]$Directory) {
    $resolvedRoot = (Resolve-Path -LiteralPath $Directory).Path
    $expected = @(Get-HutchReleaseFiles)
    $actual = @()
    foreach ($entry in Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Force) {
        if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Release cannot contain links: $($entry.Name)" }
        $relative = [IO.Path]::GetRelativePath($resolvedRoot, $entry.FullName).Replace('\', '/')
        if ($entry.PSIsContainer) {
            if ($relative -cne 'licenses') { throw "Unexpected release directory: $relative" }
            continue
        }
        if ($relative -cnotin $expected) { throw "Forbidden or unexpected release file: $relative" }
        $actual += $relative
    }
    foreach ($name in $expected) {
        if ($name -cnotin $actual) { throw "Required release file missing: $name" }
    }
}
