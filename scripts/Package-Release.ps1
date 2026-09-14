#Requires -Version 7.0
param([string]$AskaGameDir = $env:ASKA_GAME_DIR)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Release-Content.ps1')
& (Join-Path $PSScriptRoot 'Build-Local.ps1') -AskaGameDir $AskaGameDir -NoDebugSymbols
$repository = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
[xml]$project = Get-Content -LiteralPath (Join-Path $repository 'src/HutchASKA.Plugin/HutchASKA.Plugin.csproj') -Raw
$version = [string]$project.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Release version must be major.minor.patch.' }
$pluginSource = Get-Content -LiteralPath (Join-Path $repository 'src/HutchASKA.Plugin/Plugin.cs') -Raw
if ($pluginSource -notmatch ('PluginVersion\s*=\s*"' + [regex]::Escape($version) + '"')) { throw 'Plugin and project versions differ.' }
$artifacts = [IO.Path]::GetFullPath((Join-Path $repository 'artifacts'))
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
$staging = Join-Path $artifacts ('.package-' + [guid]::NewGuid().ToString('N'))
$contentRoot = Join-Path $staging 'HutchASKA'
New-Item -ItemType Directory -Path (Join-Path $contentRoot 'licenses') -Force | Out-Null
try {
    foreach ($name in @('HutchASKA.Plugin.dll', 'HutchASKA.Core.dll')) {
        $source = Join-Path $repository "src/HutchASKA.Plugin/bin/Release/net6.0/$name"
        $assembly = [Reflection.AssemblyName]::GetAssemblyName($source)
        if ($assembly.Name -cne [IO.Path]::GetFileNameWithoutExtension($name) -or $assembly.Version.ToString(3) -ne $version) {
            throw "Unexpected authored assembly identity: $name"
        }
        Copy-Item -LiteralPath $source -Destination (Join-Path $contentRoot $name)
    }
    Copy-Item -LiteralPath (Join-Path $repository 'README.md') -Destination (Join-Path $contentRoot 'README.txt')
    foreach ($name in @('LICENSE', 'THIRD_PARTY_NOTICES.md', 'licenses/BepInEx-LGPL-2.1.txt', 'licenses/HarmonyX-MIT.txt', 'licenses/Il2CppInterop-LGPL-3.0.txt')) {
        Copy-Item -LiteralPath (Join-Path $repository $name) -Destination (Join-Path $contentRoot $name)
    }
    $revision = git -C $repository rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Could not determine source revision.' }
    $dirty = git -C $repository status --porcelain --untracked-files=normal
    if ($LASTEXITCODE -ne 0) { throw 'Could not determine source status.' }
    @("HutchASKA $version", "Source commit: $revision", "Uncommitted source changes: $([bool]$dirty)",
      'Status: development candidate; manual gameplay acceptance is required.',
      'Source and documentation: https://github.com/jfhutchi/aska_trainer',
      'Runtime dependencies and ASKA files are obtained separately.') |
        Set-Content -LiteralPath (Join-Path $contentRoot 'BUILDINFO.txt') -Encoding utf8NoBOM
    Assert-HutchReleaseContent $contentRoot
    $candidate = Join-Path $staging 'candidate.zip'
    Compress-Archive -LiteralPath $contentRoot -DestinationPath $candidate
    $archive = [IO.Compression.ZipFile]::OpenRead($candidate)
    try {
        $expected = @(Get-HutchReleaseFiles | ForEach-Object { "HutchASKA/$_" })
        $actual = @($archive.Entries | Where-Object { $_.Name } | ForEach-Object FullName)
        if ($actual.Count -ne $expected.Count -or @($actual | Where-Object { $_ -cnotin $expected }).Count -gt 0 -or
            @($expected | Where-Object { $_ -cnotin $actual }).Count -gt 0) {
            throw 'Release ZIP contains unexpected or missing entries.'
        }
    }
    finally { $archive.Dispose() }
    $destination = Join-Path $artifacts "HutchASKA-v$version.zip"
    Move-Item -LiteralPath $candidate -Destination $destination -Force
    Get-FileHash -LiteralPath $destination -Algorithm SHA256 | Select-Object Path, Hash
}
finally {
    $resolvedStaging = (Resolve-Path -LiteralPath $staging).Path
    $allowedPrefix = $artifacts.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedStaging.StartsWith($allowedPrefix, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolvedStaging) -notmatch '^\.package-[0-9a-f]{32}$') { throw 'Unsafe staging cleanup path.' }
    Remove-Item -LiteralPath $resolvedStaging -Recurse -Force
}
