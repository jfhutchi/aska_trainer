#Requires -Version 7.0
param([string]$Version)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Release-Content.ps1')

$repository = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if (-not $Version) {
    [xml]$project = Get-Content -LiteralPath (Join-Path $repository 'src/HutchASKA.Plugin/HutchASKA.Plugin.csproj') -Raw
    $Version = [string]$project.Project.PropertyGroup.Version
}
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must be major.minor.patch.' }

$artifacts = Join-Path $repository 'artifacts'
$sourcePath = Join-Path $artifacts "HutchASKA-v$Version.zip"
$readmePath = Join-Path $repository 'docs/nexus/README.txt'
$destinationPath = Join-Path $artifacts "HutchASKA-v$Version-Nexus-beta.zip"
if (-not (Test-Path -LiteralPath $sourcePath)) { throw "Build the matching release archive first: $sourcePath" }
if (-not (Test-Path -LiteralPath $readmePath)) { throw "Nexus install instructions are missing: $readmePath" }
$readme = Get-Content -LiteralPath $readmePath -Raw
if (-not $readme.Contains("HutchASKA $Version")) { throw 'Nexus README version does not match the archive.' }

$prefix = 'BepInEx/plugins/HutchASKA/'
$sourceNames = @(Get-HutchReleaseFiles)
$source = [IO.Compression.ZipFile]::OpenRead($sourcePath)
$temporaryPath = Join-Path $artifacts ('.nexus-' + [guid]::NewGuid().ToString('N') + '.zip')
try {
    $actualSource = @($source.Entries | Where-Object { $_.Name } | ForEach-Object FullName)
    $expectedSource = @($sourceNames | ForEach-Object { "HutchASKA/$_" })
    if ($actualSource.Count -ne $expectedSource.Count -or
        @($actualSource | Where-Object { $_ -cnotin $expectedSource }).Count -gt 0 -or
        @($expectedSource | Where-Object { $_ -cnotin $actualSource }).Count -gt 0) {
        throw 'The source release archive has unexpected or missing files.'
    }
    $buildInfoEntry = $source.GetEntry('HutchASKA/BUILDINFO.txt')
    $reader = [IO.StreamReader]::new($buildInfoEntry.Open())
    try { $buildInfo = $reader.ReadToEnd() } finally { $reader.Dispose() }
    if (-not $buildInfo.StartsWith("HutchASKA $Version`n") -and
        -not $buildInfo.StartsWith("HutchASKA $Version`r`n")) { throw 'Source BUILDINFO version does not match.' }
    if ($buildInfo -notmatch 'Uncommitted source changes: False') { throw 'Nexus package requires a clean source build.' }

    $output = [IO.Compression.ZipFile]::Open($temporaryPath, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($name in $sourceNames | Where-Object { $_ -cne 'README.txt' }) {
            $original = $source.GetEntry("HutchASKA/$name")
            $entry = $output.CreateEntry("$prefix$name", [IO.Compression.CompressionLevel]::Optimal)
            $inputStream = $original.Open()
            $outputStream = $entry.Open()
            try { $inputStream.CopyTo($outputStream) }
            finally { $outputStream.Dispose(); $inputStream.Dispose() }
        }
        $entry = $output.CreateEntry("${prefix}README.txt", [IO.Compression.CompressionLevel]::Optimal)
        $writer = [IO.StreamWriter]::new($entry.Open(), [Text.UTF8Encoding]::new($false))
        try { $writer.Write($readme) } finally { $writer.Dispose() }
    }
    finally { $output.Dispose() }

    $verification = [IO.Compression.ZipFile]::OpenRead($temporaryPath)
    try {
        $actual = @($verification.Entries | Where-Object { $_.Name } | ForEach-Object FullName)
        $expected = @($sourceNames | ForEach-Object { "$prefix$_" })
        if ($actual.Count -ne $expected.Count -or
            @($actual | Where-Object { $_ -cnotin $expected }).Count -gt 0 -or
            @($expected | Where-Object { $_ -cnotin $actual }).Count -gt 0) {
            throw 'Nexus archive has unexpected or missing files.'
        }
        if (@($actual | Where-Object { $_ -match '\.(zip|7z|rar)$' }).Count -gt 0) {
            throw 'Nexus archive cannot contain nested archives.'
        }
        foreach ($name in @('HutchASKA.Plugin.dll', 'HutchASKA.Core.dll')) {
            $original = $source.GetEntry("HutchASKA/$name").Open()
            $packaged = $verification.GetEntry("$prefix$name").Open()
            $hash = [Security.Cryptography.SHA256]::Create()
            try {
                $expectedHash = [Convert]::ToHexString($hash.ComputeHash($original))
                $actualHash = [Convert]::ToHexString($hash.ComputeHash($packaged))
                if ($expectedHash -cne $actualHash) { throw "Nexus $name differs from the validated release archive." }
            }
            finally { $hash.Dispose(); $packaged.Dispose(); $original.Dispose() }
        }
    }
    finally { $verification.Dispose() }
    Move-Item -LiteralPath $temporaryPath -Destination $destinationPath -Force
    Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256 | Select-Object Path, Hash
}
finally {
    $source.Dispose()
    if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath -Force }
}
