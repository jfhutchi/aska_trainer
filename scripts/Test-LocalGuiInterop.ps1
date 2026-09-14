param([string]$AskaGameDir = $env:ASKA_GAME_DIR)
$ErrorActionPreference = 'Stop'
if (-not $AskaGameDir) { throw 'Set ASKA_GAME_DIR or pass -AskaGameDir.' }
Add-Type -Path (Join-Path $AskaGameDir 'BepInEx/core/Mono.Cecil.dll')
$gui = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $AskaGameDir 'BepInEx/interop/UnityEngine.IMGUIModule.dll'))
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $PSScriptRoot '../src/HutchASKA.Plugin/bin/Release/net6.0/HutchASKA.Plugin.dll'))
try {
    $methods = @{}
    foreach ($type in $gui.MainModule.GetTypes()) {
        foreach ($method in $type.Methods) { $methods[$method.FullName] = $method }
    }
    $pending = [Collections.Generic.Queue[string]]::new()
    $paths = @{}
    foreach ($type in $plugin.MainModule.GetTypes()) {
        foreach ($method in $type.Methods) {
            if (-not $method.HasBody) { continue }
            foreach ($instruction in $method.Body.Instructions) {
                if ($instruction.Operand -isnot [Mono.Cecil.MethodReference]) { continue }
                $key = $instruction.Operand.FullName
                if ($methods.ContainsKey($key) -and -not $paths.ContainsKey($key)) {
                    $paths[$key] = "$($method.FullName) -> $key"
                    $pending.Enqueue($key)
                }
            }
        }
    }
    if ($pending.Count -eq 0) { throw 'No plugin IMGUI calls found; build the plugin first.' }
    $failures = [Collections.Generic.List[string]]::new()
    while ($pending.Count -gt 0) {
        $key = $pending.Dequeue()
        $method = $methods[$key]
        if (-not $method.HasBody) { continue }
        foreach ($instruction in $method.Body.Instructions) {
            if ($instruction.OpCode.Name -eq 'ldstr' -and $instruction.Operand -eq 'Method unstripping failed') {
                $failures.Add($paths[$key])
            }
            if ($instruction.Operand -isnot [Mono.Cecil.MethodReference]) { continue }
            $next = $instruction.Operand.FullName
            if ($methods.ContainsKey($next) -and -not $paths.ContainsKey($next)) {
                $paths[$next] = "$($paths[$key]) -> $next"
                $pending.Enqueue($next)
            }
        }
    }
    if ($failures.Count -gt 0) { throw "Reachable IMGUI unstripping stubs ($($failures.Count)):`n$($failures -join "`n")" }
    Write-Output "PASS: $($paths.Count) reachable managed IMGUI methods contain no unstripping failure stubs. Native rendering still requires an in-game test."
}
finally { $plugin.Dispose(); $gui.Dispose() }
