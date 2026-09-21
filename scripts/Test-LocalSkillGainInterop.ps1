param(
    [string]$AskaGameDir = $env:ASKA_GAME_DIR,
    [string]$PluginAssemblyPath = (Join-Path $PSScriptRoot '../src/HutchASKA.Plugin/bin/Release/net6.0/HutchASKA.Plugin.dll')
)
$ErrorActionPreference = 'Stop'
if (-not $AskaGameDir) { throw 'Set ASKA_GAME_DIR or pass -AskaGameDir.' }
Add-Type -Path (Join-Path $AskaGameDir 'BepInEx/core/Mono.Cecil.dll')
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $AskaGameDir 'BepInEx/interop/Assembly-CSharp.dll'))
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($PluginAssemblyPath)
try {
    $awards = @($game.MainModule.GetType('SSSGame.InteractionMoveset').Methods | Where-Object Name -eq 'AwardProfficiencyPoints')
    if ($awards.Count -ne 1 -or $awards[0].ReturnType.FullName -ne 'System.Void' -or
        $awards[0].Parameters.Count -ne 3 -or
        $awards[0].Parameters[0].ParameterType.FullName -ne 'SSSGame.IInteractionAgent' -or
        $awards[0].Parameters[1].ParameterType.FullName -ne 'System.Single' -or
        $awards[0].Parameters[1].ParameterType.IsByReference -or
        $awards[0].Parameters[2].ParameterType.FullName -ne 'SandSailorStudio.Attributes.Attribute') {
        throw 'Skill gain requires the verified native by-value award signature.'
    }
    $feature = $plugin.MainModule.GetType('HutchASKA.Plugin.Player.SkillGainFeature')
    foreach ($name in @('PlayerPrefix', 'TribePrefix')) {
        $prefix = @($feature.Methods | Where-Object Name -eq $name)
        if ($prefix.Count -ne 1 -or $prefix[0].Parameters.Count -ne 2 -or
            $prefix[0].Parameters[0].Name -ne '__0' -or
            $prefix[0].Parameters[0].ParameterType.FullName -ne 'SSSGame.IInteractionAgent' -or
            $prefix[0].Parameters[1].Name -ne '__1' -or
            $prefix[0].Parameters[1].ParameterType.FullName -ne 'System.Single&') {
            throw "Skill gain prefix does not bind the verified agent/award arguments: $name"
        }
    }
    Write-Output 'PASS: Player and tribe skill gain bind a by-value native experience award; original primitive-byref methods are not targeted. Gameplay acceptance remains required.'
}
finally { $plugin.Dispose(); $game.Dispose() }
