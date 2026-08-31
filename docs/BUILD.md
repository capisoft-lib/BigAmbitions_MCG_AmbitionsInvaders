# Build and test

The standalone build produces only `MCG_AmbitionsInvaders/AmbitionsInvaders.dll`, its manifest, artwork, thumbnail, locales and documentation. It does not install or publish anything.

## Requirements

- Windows and PowerShell 7.
- A legally installed Mono version of Big Ambitions using Unity **2022.3.62f2 (7670c08855a9)**.
- The matching Unity Editor, including its bundled compiler and Mono.Cecil tools.
- A separately built or installed `LIB_BaComputerGames.dll`, API 1.0.0 or newer, compiled for the game's Mono profile, with its matching 1.0.0+ `ModManifest.asset` alongside the DLL (or supplied through `-McgManifest`).
- .NET 8 SDK for the independent simulation tests.

Provide your own local dependency locations through environment variables or the script parameters. Do not commit these values or copy dependency DLLs into this repository.

```powershell
./tools/build.ps1 -GameDirectory $env:BA_GAME_DIR -UnityEditorPath $env:UNITY_EDITOR -McgDll $env:MCG_DLL
dotnet run --project ./tools/Tests~/AmbitionsInvaders.Tests.csproj -c Release
```

The build rejects a preview MCG dependency. Manifest/catalog versions must be 1.0.0, and the resulting assembly and file versions must be 1.0.0.0. Its ignored `build-status.json` records dependency versions and hashes without private paths.

The build creates a fresh directory beneath ignored `artifacts/` and prints the package location and DLL SHA-256. Share only its `MCG_AmbitionsInvaders` child folder. The parent includes private reference copies and a compiler response file and must never be shared.

The compiler uses deterministic, optimized builds with debug symbols disabled and a neutral source path map. The script rejects PDB/embedded-symbol references, private input paths in the DLL, the wrong runtime profile and bundled dependency DLLs.

## Unity SDK integration

The repository can also be placed under a Big Ambitions mod SDK project's `Assets/Mods` directory. Retain all existing `.meta` files and import the separate MCG API before compiling. The `tools/Tests~` directory is deliberately ignored by Unity's asset importer. The standalone script is the reproducible package route documented here; an arbitrary SDK build is not automatically covered by its privacy checks.

Folder name: `MCG_AmbitionsInvaders`. Technical mod and assembly names: `AmbitionsInvaders`. Catalog ID: `capisoft:ambitions-invaders`. Ruleset: `invaders-standard-v1`. Keep these identifiers stable to preserve records.
