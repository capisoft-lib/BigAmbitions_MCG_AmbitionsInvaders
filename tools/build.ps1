# Build AmbitionsInvaders only. Dependencies are supplied by the caller and are never packaged.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameDirectory,
    [Parameter(Mandatory = $true)][string]$UnityEditorPath,
    [Parameter(Mandatory = $true)][string]$McgDll,
    [string]$McgManifest
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$game = (Resolve-Path -LiteralPath $GameDirectory).Path
$editor = (Resolve-Path -LiteralPath $UnityEditorPath).Path
$mcg = (Resolve-Path -LiteralPath $McgDll).Path
$mcgHash = (Get-FileHash -LiteralPath $mcg -Algorithm SHA256).Hash
if (!$McgManifest) { $McgManifest = Join-Path (Split-Path $mcg -Parent) 'ModManifest.asset' }
$mcgManifestPath = (Resolve-Path -LiteralPath $McgManifest).Path
$mcgManifestHash = (Get-FileHash -LiteralPath $mcgManifestPath -Algorithm SHA256).Hash
$mcgManifestText = Get-Content -LiteralPath $mcgManifestPath -Raw
if ($mcgManifestText -notmatch '(?m)^\s+ModId: LIB_BaComputerGames\s*$') { throw 'MCG manifest has the wrong mod ID.' }
if ($mcgManifestText -notmatch '(?m)^\s+Version: (\d+\.\d+\.\d+)\s*$') { throw 'MCG manifest must declare a numeric version.' }
$mcgPackageVersion = [version]$Matches[1]
if ($mcgPackageVersion -lt [version]'1.0.1') { throw 'Ambitions Invaders 1.0.1 requires the final MCG 1.0.1+ package.' }
$manifestText = Get-Content -LiteralPath (Join-Path $repo 'ModManifest.asset') -Raw
if ($manifestText -notmatch '(?m)^  Version: 1\.0\.1\s*$' -or $manifestText -notmatch '(?m)^  ModId: AmbitionsInvaders\s*$') { throw 'Unexpected Invaders manifest version or ID.' }
$catalogText = Get-Content -LiteralPath (Join-Path $repo 'Scripts/AmbitionsInvadersMod.cs') -Raw
if ($catalogText -notmatch 'version: "1\.0\.1"' -or !$catalogText.Contains('"capisoft:ambitions-invaders"') -or !$catalogText.Contains('"invaders-standard-v1"')) { throw 'Unexpected Invaders catalog version or record identifiers.' }
$editorData = Join-Path (Split-Path $editor -Parent) 'Data'
$managed = Join-Path $game 'Big Ambitions_Data/Managed'
foreach ($binary in @($editor, (Join-Path $game 'UnityPlayer.dll'))) {
    $version = (Get-Item -LiteralPath $binary).VersionInfo.ProductVersion
    if ($version -notlike '2022.3.62f2*7670c08855a9*') { throw 'Unity Editor and game player must use 2022.3.62f2 (7670c08855a9).' }
}
if (!(Test-Path -LiteralPath (Join-Path $game 'MonoBleedingEdge'))) { throw 'A Mono game installation is required; IL2CPP is unsupported.' }
foreach ($name in @('mscorlib.dll', 'BigAmbitions.dll', 'BigAmbitions.ModAPI.dll', 'ArcadeMachines.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $managed $name))) { throw "Missing required game assembly: $name" }
}
$dotnet = Join-Path $editorData 'NetCoreRuntime/dotnet.exe'
$compiler = Join-Path $editorData 'DotNetSdkRoslyn/csc.dll'
$cecil = Join-Path $editorData 'il2cpp/build/deploy/Mono.Cecil.dll'
foreach ($tool in @($dotnet, $compiler, $cecil)) { if (!(Test-Path -LiteralPath $tool)) { throw 'Matching Unity compiler tools are missing.' } }
if (!('Mono.Cecil.AssemblyDefinition' -as [type])) { Add-Type -LiteralPath $cecil }
$dependency = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($mcg)
try {
    if ($dependency.Name.Name -ne 'LIB_BaComputerGames') { throw 'McgDll must be the standalone LIB_BaComputerGames assembly.' }
    if ($dependency.MainModule.AssemblyReferences.Name -contains 'netstandard') { throw 'MCG must be built for the game Mono player profile.' }
    $versionType = $dependency.MainModule.GetType('Capisoft.Lib.BaComputerGames.ComputerGames')
    if (!$versionType) { throw 'MCG public API is missing.' }
    $versionField = $versionType.Fields | Where-Object Name -eq 'ApiVersion'
    if (!$versionField -or [version]$versionField.Constant -lt [version]'1.0.1') { throw 'MCG API 1.0.1 or later is required.' }
    $mcgApiVersion = [string]$versionField.Constant
    $mcgAssemblyVersion = [string]$dependency.Name.Version
    if ($dependency.Name.Version -lt [version]'1.0.1.0') { throw 'The final MCG assembly version is required.' }
}
finally { $dependency.Dispose() }

# Unique output per invocation: no recursive deletion or implicit installation.
$buildRoot = Join-Path $repo ('artifacts/build-' + (Get-Date -Format 'yyyyMMdd-HHmmssfff') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 6))
$package = Join-Path $buildRoot 'MCG_AmbitionsInvaders'
$referenceRoot = Join-Path $buildRoot 'private-references'
New-Item -ItemType Directory -Path $package, $referenceRoot -Force | Out-Null

# The game strips unused Unity wrappers. Compile against the matching full Unity
# modules, retargeted from netstandard to the game's mscorlib profile in private copies.
$unityModules = Join-Path $editorData 'Managed/UnityEngine'
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory($unityModules)
$reader = New-Object Mono.Cecil.ReaderParameters
$reader.AssemblyResolver = $resolver
try {
    foreach ($module in Get-ChildItem -LiteralPath $unityModules -Filter 'UnityEngine*.dll' -File) {
        if ($module.Name -eq 'UnityEngine.UnityWebRequestModule.dll') { continue }
        $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($module.FullName, $reader)
        try {
            foreach ($reference in $assembly.MainModule.AssemblyReferences | Where-Object Name -eq 'netstandard') {
                $reference.Name = 'mscorlib'; $reference.Version = [version]'4.0.0.0'; $reference.Culture = $null
                $reference.PublicKeyToken = [byte[]]@(0xb7,0x7a,0x5c,0x56,0x19,0x34,0xe0,0x89)
            }
            $assembly.Write((Join-Path $referenceRoot $module.Name))
        }
        finally { $assembly.Dispose() }
    }
}
finally { $resolver.Dispose() }
$references = @(
    Get-ChildItem -LiteralPath $managed -Filter '*.dll' -File | Where-Object {
        $_.Name -notlike 'UnityEngine*.dll' -or $_.Name -in @('UnityEngine.UI.dll','UnityEngine.UnityWebRequestModule.dll')
    }
    Get-ChildItem -LiteralPath $referenceRoot -Filter '*.dll' -File
    Get-Item -LiteralPath $mcg
) | Sort-Object Name -Unique
$sources = @(Get-ChildItem -LiteralPath (Join-Path $repo 'Scripts') -Filter '*.cs' -File -Recurse | Sort-Object FullName)
if (!$sources.Count) { throw 'No AmbitionsInvaders sources found.' }
$dll = Join-Path $package 'AmbitionsInvaders.dll'
$response = Join-Path $buildRoot 'private-build.rsp'
$compilerArgs = @('/target:library','/optimize+','/debug-','/deterministic+','/langversion:latest','/define:BA_GAME_DLLS_IMPORTED')
$compilerArgs += '/pathmap:"' + $repo.Replace('\','/') + '=/_/AmbitionsInvaders"'
$compilerArgs += '/out:"' + $dll.Replace('\','/') + '"'
$compilerArgs += @($references | ForEach-Object { '/reference:"' + $_.FullName.Replace('\','/') + '"' })
$compilerArgs += @($sources | ForEach-Object { '"' + $_.FullName.Replace('\','/') + '"' })
[IO.File]::WriteAllLines($response, $compilerArgs, (New-Object Text.UTF8Encoding($false)))
& $dotnet exec $compiler /noconfig /nostdlib ("@" + $response)
if ($LASTEXITCODE -ne 0) { throw 'AmbitionsInvaders compilation failed; private build files remain under ignored artifacts.' }

$built = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll)
try {
    $assemblyRefs = @($built.MainModule.AssemblyReferences.Name)
    if ($assemblyRefs -contains 'netstandard' -or $assemblyRefs -notcontains 'mscorlib') { throw 'Wrong player runtime profile.' }
    if ($assemblyRefs -notcontains 'LIB_BaComputerGames') { throw 'The separate MCG dependency is missing.' }
    $modEntry = $built.MainModule.GetType('AmbitionsInvaders.AmbitionsInvadersMod')
    $registerAttributes = @($built.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'BAModAPI.RegisterModClassAttribute' })
    if (!$modEntry -or $modEntry.BaseType.FullName -ne 'System.Object' -or
        @($modEntry.Interfaces | Where-Object { $_.InterfaceType.FullName -eq 'BAModAPI.IModBigAmbitions' }).Count -ne 1 -or
        $registerAttributes.Count -ne 1 -or $registerAttributes[0].ConstructorArguments[0].Value.FullName -ne $modEntry.FullName) {
        throw 'RegisterModClass must target the BAModAPI-only Invaders entry.'
    }
    $entryMetadataTypes = @($modEntry.BaseType) + @($modEntry.Interfaces | ForEach-Object InterfaceType) +
        @($modEntry.Fields | ForEach-Object FieldType) + @($modEntry.Properties | ForEach-Object PropertyType) +
        @($modEntry.Methods | ForEach-Object { @($_.ReturnType) + @($_.Parameters | ForEach-Object ParameterType) })
    if (@($entryMetadataTypes | Where-Object { $_.FullName -like 'Capisoft.Lib.BaComputerGames*' }).Count) {
        throw 'The registered Invaders entry metadata must resolve without MCG.'
    }
    if ([string]$built.Name.Version -ne '1.0.1.0') { throw 'Unexpected Invaders assembly version.' }
    $fileVersion = $built.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'System.Reflection.AssemblyFileVersionAttribute' }
    $infoVersion = $built.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'System.Reflection.AssemblyInformationalVersionAttribute' }
    if (!$fileVersion -or $fileVersion.ConstructorArguments[0].Value -ne '1.0.1.0' -or !$infoVersion -or $infoVersion.ConstructorArguments[0].Value -ne '1.0.1') { throw 'Unexpected Invaders file/informational version.' }
    $mcgReference = $built.MainModule.AssemblyReferences | Where-Object Name -eq 'LIB_BaComputerGames'
    if ([string]$mcgReference.Version -ne $mcgAssemblyVersion) { throw 'MCG reference differs from compiler input.' }
    if ($assemblyRefs -contains 'ComputerArcade' -or $assemblyRefs -contains 'ComputerGameHighScore' -or $assemblyRefs -contains 'FlappyAmbition' -or $assemblyRefs -contains 'LIB_BaUnifiedUI' -or $assemblyRefs -contains 'UnityEditor') { throw 'Unexpected game/leaderboard dependency.' }
    if ($built.MainModule.HasDebugHeader) {
        foreach ($entry in $built.MainModule.GetDebugHeader().Entries) {
            if ([string]$entry.Directory.Type -in @('CodeView','EmbeddedPortablePdb')) { throw 'Debug symbols or a PDB reference must not be shipped.' }
        }
    }
}
finally { $built.Dispose() }
$bytes = [IO.File]::ReadAllBytes($dll)
# A UTF-16 string can begin on either byte alignment inside a PE file.
$decodedViews = @(
    [Text.Encoding]::UTF8.GetString($bytes)
    [Text.Encoding]::Unicode.GetString($bytes)
    [Text.Encoding]::Unicode.GetString($bytes, 1, $bytes.Length - 1)
)
foreach ($text in $decodedViews) {
    foreach ($privatePath in @($repo, $game, $editorData, $mcg, $env:USERPROFILE)) {
        if (!$privatePath) { continue }
        foreach ($variant in @($privatePath, $privatePath.Replace('\','/'))) {
            if ($text.IndexOf($variant, [StringComparison]::OrdinalIgnoreCase) -ge 0) { throw 'Private absolute path detected in the compiled assembly.' }
        }
    }
}
foreach ($name in @('README.md','REQUIRED_MODS.md','ART_PROVENANCE.md','VERIFICATION.md','CHANGELOG.md','LICENSE','ModManifest.asset','Thumbnail.jpg')) {
    Copy-Item -LiteralPath (Join-Path $repo $name) -Destination (Join-Path $package $name)
}
foreach ($folder in @('Locales','docs','Art')) {
    $target = Join-Path $package $folder
    New-Item -ItemType Directory -Path $target | Out-Null
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $repo $folder) -File | Where-Object Extension -in @('.json','.md','.png')) {
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $target $file.Name)
    }
}
if (@(Get-ChildItem -LiteralPath $package -Recurse -Filter '*.dll').Count -ne 1) { throw 'Package contains dependency DLLs.' }
if (@(Get-ChildItem -LiteralPath $package -Recurse -File | Where-Object Extension -in @('.pdb','.mdb','.rsp','.log')).Count) { throw 'Package contains private build artifacts.' }
if ((Get-FileHash -LiteralPath $mcg -Algorithm SHA256).Hash -ne $mcgHash) { throw 'MCG changed during compilation; rebuild against a stable dependency.' }
if ((Get-FileHash -LiteralPath $mcgManifestPath -Algorithm SHA256).Hash -ne $mcgManifestHash) { throw 'MCG manifest changed during compilation.' }
$buildStatus = [ordered]@{
    Version = '1.0.1'
    AssemblyVersion = '1.0.1.0'
    DllSha256 = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
    McgPackageVersion = [string]$mcgPackageVersion
    McgApiVersion = $mcgApiVersion
    McgAssemblyVersion = $mcgAssemblyVersion
    McgDllSha256 = $mcgHash
    ReleaseDependencySatisfied = $true
    NativeRuntimeVerified = $false
}
$buildStatus | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $buildRoot 'build-status.json') -Encoding UTF8
Write-Host ('AmbitionsInvaders-only package: ' + $package)
Write-Host ('SHA256: ' + (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash)
Write-Host 'Nothing installed or published. Only share the MCG_AmbitionsInvaders package, never its parent build directory.'
