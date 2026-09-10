param(
    [string]$Assembly = (Join-Path $PSScriptRoot '../bin/Release/netstandard2.1/FoodUnlock.dll'),
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim',
    [string]$ProfileDir = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\Default"
)
$ErrorActionPreference = 'Stop'
$profile = $ProfileDir
$managed = Join-Path $GameDir 'valheim_Data/Managed'
Add-Type -Path "$profile\BepInEx\core\Mono.Cecil.dll"
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory($managed)
$resolver.AddSearchDirectory("$profile\BepInEx\core")
$resolver.AddSearchDirectory((Split-Path (Resolve-Path $Assembly)))
$parameters = [Mono.Cecil.ReaderParameters]::new()
$parameters.AssemblyResolver = $resolver
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path $Assembly), $parameters)
$count = 0
$errors = 0
foreach ($member in $asm.MainModule.GetMemberReferences()) {
    if ($member.DeclaringType.Scope.Name -notmatch '^(assembly_|Unity|Splatform)') { continue }
    $count++
    try {
        $definition = $member.Resolve()
        if ($null -eq $definition) { "MISSING: $($member.FullName)"; $errors++ }
        elseif ($definition -is [Mono.Cecil.FieldDefinition] -and $definition.IsLiteral) { "CONSTANT: $($member.FullName) = $($definition.Constant)"; $errors++ }
    } catch { "UNRESOLVED: $($member.FullName): $_"; $errors++ }
}
"Checked $count game/Unity member references; $errors findings."
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed 'assembly_valheim.dll'), $parameters)
$player = $game.MainModule.GetType('Player')
# String-based private targets are not covered by ordinary member-reference resolution.
$targets = @{
    UpdateFood = 'System.Single,System.Boolean'
    GetTotalFoodValue = 'System.Single&,System.Single&,System.Single&'
    SetMaxEitr = 'System.Single,System.Boolean'
    OnSpawned = 'System.Boolean'
    TakeInput = ''
}
foreach ($name in $targets.Keys) {
    $methods = @($player.Methods | Where-Object {
        $_.Name -eq $name -and (($_.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ',') -eq $targets[$name]
    })
    $returnType = if ($name -eq 'TakeInput') { 'System.Boolean' } else { 'System.Void' }
    if ($methods.Count -ne 1 -or $methods[0].ReturnType.FullName -ne $returnType -or $methods[0].IsStatic) {
        "MISSING OR CHANGED TARGET: Player.$name"; $errors++
    } else { "PASS: Player.$name signature" }
}
$plugin = $asm.MainModule.GetType('FoodUnlock.Plugin')
$patches = @($plugin.NestedTypes | Where-Object {
    @($_.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' }).Count -gt 0
})
if ($patches.Count -ne 5) { 'Expected five Harmony patch declarations'; $errors++ }
else { 'PASS: five Harmony patch declarations (runtime installation not tested)' }
$awake = $plugin.Methods | Where-Object Name -eq 'Awake'
$setting = @($awake.Body.Instructions | Where-Object {
    $_.OpCode.Name -eq 'ldstr' -and $_.Operand -eq 'Permanent assigned food'
})
if ($setting.Count -ne 1 -or $setting[0].Next.OpCode.Name -ne 'ldc.i4.0') {
    'Permanent assigned food default is not false'; $errors++
} else { 'PASS: permanent assigned food compiled default is false' }
$game.Dispose()
$asm.Dispose()
if ($errors -gt 0) { exit 1 }
