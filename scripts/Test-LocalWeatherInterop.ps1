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
    $forecast = $game.MainModule.GetType('SSSGame.Weather.WeatherSystem/Forecast')
    $sample = @($forecast.Methods | Where-Object Name -eq 'GetWeatherConditions')
    $override = @($forecast.Methods | Where-Object Name -eq 'GetWeatherConditionDebug')
    if ($sample.Count -ne 1 -or $sample[0].ReturnType.FullName -ne 'System.Void' -or
        $sample[0].Parameters.Count -ne 2 -or $sample[0].Parameters[0].ParameterType.FullName -ne 'System.Int32' -or
        $sample[0].Parameters[1].ParameterType.FullName -ne 'System.Single' -or
        $override.Count -ne 1 -or $override[0].ReturnType.FullName -ne 'System.Void' -or
        $override[0].Parameters.Count -ne 3 -or
        $override[0].Parameters[0].ParameterType.FullName -ne 'SSSGame.Weather.ProgressingWeatherConditionConfig' -or
        $override[0].Parameters[1].ParameterType.FullName -ne 'System.Single' -or
        $override[0].Parameters[2].ParameterType.FullName -ne 'System.Single') {
        throw 'Weather requires the verified by-value forecast sample and temporary condition helper.'
    }
    $feature = $plugin.MainModule.GetType('HutchASKA.Plugin.World.WeatherOverrideFeature')
    $postfix = @($feature.Methods | Where-Object Name -eq 'ForecastPostfix')
    if ($postfix.Count -ne 1 -or $postfix[0].Parameters.Count -ne 1 -or
        $postfix[0].Parameters[0].Name -ne '__instance' -or
        $postfix[0].Parameters[0].ParameterType.FullName -ne $forecast.FullName) {
        throw 'Weather postfix does not bind the current native forecast instance.'
    }
    $types = @($plugin.MainModule.GetTypes() | Where-Object { $_.FullName.StartsWith($feature.FullName) })
    $calls = @($types | ForEach-Object Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions } |
        Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] } | ForEach-Object Operand)
    foreach ($call in $calls) {
        if ($call.DeclaringType.FullName.StartsWith('SSSGame.Weather.') -and
            (($call.Name.StartsWith('set_') -and $call.Name -ne 'set_MustUpdateNextFrame') -or
            $call.Name -in @('ResetWeatherForecast', 'Initialise', 'Rpc_ClearSkies', 'Rpc_SetGameTime', 'SetGameTime', 'Rpc_SetDayOfYear', 'SetDayOfYear'))) {
            throw "Weather selection must not rewrite native forecast dates, assets, clock or override fields: $call"
        }
    }
    if (-not @($calls | Where-Object { $_.Name -eq 'GetWeatherConditionDebug' }).Count) {
        throw 'Temporary weather sample replacement is missing.'
    }
    Write-Output 'PASS: Weather uses the by-value forecast sample, with no direct forecast/date/asset/clock writes. Gameplay and save/reload acceptance remain required.'
}
finally { $plugin.Dispose(); $game.Dispose() }
