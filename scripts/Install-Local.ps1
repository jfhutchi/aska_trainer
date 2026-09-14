param([string]$AskaGameDir = $env:ASKA_GAME_DIR)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Build-Local.ps1') -AskaGameDir $AskaGameDir
if (Get-Process -Name Aska -ErrorAction SilentlyContinue) { throw 'Close ASKA before installing HutchASKA.' }
$destination = Join-Path $env:ASKA_GAME_DIR 'BepInEx\plugins\HutchASKA'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
foreach ($name in @('HutchASKA.Plugin.dll', 'HutchASKA.Core.dll')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "..\src\HutchASKA.Plugin\bin\Release\net6.0\$name") -Destination $destination -Force
}
Write-Output "Installed HutchASKA assemblies into $destination"
