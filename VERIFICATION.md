# Verification scope

The following checks cover the current 0.1.0 game sources:

- 23 independent simulation checks: movement bounds, shooting, waves, rival behaviour, damage, invulnerability, boss rounds, scoring and game-over transitions.
- 120 checks in an isolated Unity 2022.3.62f2 player: actual UI, game lifecycle, completed rounds, record integration and disposal.
- All 22 locale files have the same 11 non-empty keys. The exit hints use Tab to match MCG.
- An isolated localization player renders ready, combat and game-over states in all 22 languages: 66 screenshots, 814 label layout checks and 22 glyph checks, without missing glyphs, oversized labels or localization fallbacks.
- The release DLL is checked against the game and MCG APIs, uses the native Mono profile and has no embedded dependency DLLs, PDB references or private build paths.
- The standalone source and an exported Git candidate produce identical package contents.

The selected English screenshots in the README come from the isolated Unity player. These checks do not establish a full native Big Ambitions session or a Steam Workshop publication. A native smoke test should cover computer entry, movement and fire, Tab exit, Escape pause, and a completed round's record after restarting the game.

Local record persistence is provided by MCG. Use an MCG build containing its verified record serializer fix. A previous malformed record file that contains no score cannot be used to reconstruct a lost high score.

Private logs, machine paths, profile IDs and raw score files are deliberately excluded from this repository.
