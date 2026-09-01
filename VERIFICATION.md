# Verification scope

## Release 1.0.1 — 2026-09-01

Manifest/catalog/informational version **1.0.1** and assembly/file version **1.0.1.0** are aligned. All 23 simulation checks and the 37-file standalone package build pass against MCG package/API **1.0.1**, assembly **1.0.1.0**, SHA-256 `6B111BF45763A424F3A69FD939DF6D12B0DFC276D797B5622EF26A01CDF19051`. Invaders DLL SHA-256: `A3519C0750BF170208300CA6B840154D93060FDC7E0F5BF3F7EB3AF3D40BF2A9`.

A matching Unity Mono probe, with `LIB_BaComputerGames.dll` deliberately unavailable, resolves the rebuilt `AmbitionsInvaders.AmbitionsInvadersMod` entry without loading MCG. The build enforces this BAModAPI-only registered-type metadata contract and ships only Invaders' DLL. No native game launch or Workshop publication was performed.

The following checks cover the 1.0.0 game sources:

The release is compiled against the actual MCG package/API 1.0.0 and assembly 1.0.0.0. Invaders declares 1.0.0 in its manifest/catalog and 1.0.0.0 in its assembly/file metadata. The package contains 37 files, including its 1254 × 1254 JPEG icon; the PNG master and publication text files remain outside the installed package. Gameplay and existing record identifiers are unchanged from the audited preview.

- 23 independent simulation checks: movement bounds, shooting, waves, rival behaviour, damage, invulnerability, boss rounds, scoring and game-over transitions.
- 120 checks in an isolated Unity 2022.3.62f2 player: actual UI, game lifecycle, completed rounds, record integration and disposal.
- All 22 locale files have the same 11 non-empty keys. Host navigation hints are displayed by MCG; Invaders only displays its own gameplay controls.
- An isolated localization player renders ready, combat and game-over states in all 22 languages: 66 screenshots, 814 label layout checks and 22 glyph checks, without missing glyphs, oversized labels or localization fallbacks.
- The release DLL is checked against the game and MCG APIs, uses the native Mono profile and has no embedded dependency DLLs, PDB references or private build paths.
- The standalone source and an exported Git candidate produce identical package contents.

The selected English screenshots in the README come from the isolated Unity player. These checks do not establish a full native Big Ambitions session or a Steam Workshop publication. A native smoke test should cover computer entry, movement and fire, Tab exit, Escape pause, and a completed round's record after restarting the game.

Local record persistence is provided by the separate MCG 1.0.0+ dependency, including its verified record serializer fix. A previous malformed record file that contains no score cannot be used to reconstruct a lost high score.

Private logs, machine paths, profile IDs and raw score files are deliberately excluded from this repository.
