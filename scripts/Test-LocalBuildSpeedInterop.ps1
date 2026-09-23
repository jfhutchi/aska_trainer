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
    $feature = $plugin.MainModule.GetType('HutchASKA.Plugin.Player.BuildSpeedFeature')
    if (-not $feature) { throw 'Build Speed feature is missing from the plugin.' }
    $instructions = @($feature.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions })
    if (@($instructions | Where-Object { $_.OpCode.Name -eq 'ldstr' -and $_.Operand -eq 'RequestAddBuildVolume' }).Count -gt 0 -or
        @($instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'RequestAddBuildVolume' }).Count -gt 0) {
        throw 'Build Speed references the withdrawn primitive-byref work hook.'
    }
    $session = $game.MainModule.GetTypes() | Where-Object FullName -eq 'SSSGame.PlayerBuildInteractionConfig/PlayerBuildInteractionSession'
    $event = @($session.Methods | Where-Object { $_.Name -eq '_OnAnimatorEvent' -and $_.Parameters.Count -eq 1 -and $_.Parameters[0].ParameterType.FullName -eq 'System.String' })
    $getter = @($session.Properties | Where-Object Name -eq 'BuildConfig' | ForEach-Object GetMethod)
    if ($event.Count -ne 1 -or $getter.Count -ne 1 -or $getter[0].Parameters.Count -ne 0 -or
        $getter[0].ReturnType.FullName -ne 'SSSGame.PlayerBuildInteractionConfig') {
        throw 'Current local building event/configuration getter signatures do not match.'
    }
    $enable = $feature.Methods | Where-Object Name -eq 'TryEnable'
    $targetNames = @($enable.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldstr' } | ForEach-Object Operand)
    if ('_OnAnimatorEvent' -cnotin $targetNames -or 'BuildConfig' -cnotin $targetNames) {
        throw 'Expected scoped building event/configuration hooks are missing.'
    }
    foreach ($withdrawn in @('HutchASKA.Plugin.Items.ItemDecayPatch', 'HutchASKA.Plugin.Items.EquipmentWearPatch')) {
        if ($plugin.MainModule.GetType($withdrawn)) {
            throw "Withdrawn item processing hook is still compiled: $withdrawn"
        }
    }
    $durability = $plugin.MainModule.GetType('HutchASKA.Plugin.Items.InfiniteDurabilityFeature')
    $durabilityEnable = $durability.Methods | Where-Object Name -eq 'TryEnable'
    $durabilityPrefix = $durability.Methods | Where-Object Name -eq 'SetValuePrefix'
    if (-not $durabilityEnable -or -not $durabilityPrefix -or
        $durabilityPrefix.Parameters.Count -ne 2 -or
        $durabilityPrefix.Parameters[0].ParameterType.FullName -ne 'SandSailorStudio.Attributes.Property' -or
        $durabilityPrefix.Parameters[1].ParameterType.FullName -ne 'System.Single' -or
        'SetValue' -cnotin @($durabilityEnable.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldstr' } | ForEach-Object Operand) -or
        @($durability.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions } | Where-Object {
            $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -in @('Run', '_DealDurabilityDamage')
        }).Count -gt 0) {
        throw 'Durability must use the by-value property setter, not the withdrawn primitive-byref item hooks.'
    }
    foreach ($name in @('Items.NoSpoilageFeature', 'World.MushroomRegrowthFeature')) {
        $itemFeature = $plugin.MainModule.GetType("HutchASKA.Plugin.$name")
        $probe = $itemFeature.Methods | Where-Object Name -eq 'ProbeCompatibility'
        $enableItem = $itemFeature.Methods | Where-Object Name -eq 'TryEnable'
        if (-not $probe -or -not $enableItem -or
            -not @($probe.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'Incompatible' }).Count -or
            @($enableItem.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldc.i4.0' }).Count -ne 1 -or
            @($enableItem.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.DeclaringType.Namespace -eq 'HarmonyLib' }).Count -gt 0) {
            throw "$name must reject activation without installing hooks."
        }
    }
    $mushrooms = $plugin.MainModule.GetType('HutchASKA.Plugin.World.MushroomRegrowthFeature')
    if (@($mushrooms.Methods | Where-Object { $_.Name -in @('Prepare', 'BeforeWeatherDispatch') }).Count -gt 0 -or
        @($mushrooms.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions } | Where-Object {
            $_.Operand -is [Mono.Cecil.MethodReference] -and
            ($_.Operand.Name -eq 'set_NextReplenishDate' -or $_.Operand.DeclaringType.Namespace -eq 'HarmonyLib')
        }).Count -gt 0) { throw 'Withdrawn mushroom timer hook remains compiled.' }
    Write-Output 'PASS: Mushroom regrowth rejects activation and contains no timer writes or hooks.'
    Write-Output 'PASS: Build Speed uses the local event/configuration hooks and does not reference the withdrawn primitive-byref work hook. Native progress still requires a game test.'
    Write-Output 'PASS: Durability uses a by-value property setter; withdrawn item decay/wear hooks are absent and No Spoilage remains unavailable.'
}
finally { $plugin.Dispose(); $game.Dispose() }
