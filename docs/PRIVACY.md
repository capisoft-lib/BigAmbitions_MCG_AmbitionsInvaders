# Publication privacy

Only source, Unity metadata, locales, documentation, pixel-art sprites and selected English UI renders belong in Git. No game/library binaries, build outputs, debug symbols, response files, logs, save data, score records, credentials or workstation configuration are included.

The repository excludes local build outputs and private inputs through `.gitignore`. This is a safeguard, not a replacement for reviewing the exact staged files and complete commit history before publication.

The package is compiled without symbols. Its PE debug directory and both UTF-8 and UTF-16 strings are checked for symbol references and private build paths. Removing a sidecar PDB alone would not establish that the DLL is clean.

Published screenshots are game-only renders with no desktop chrome or textual metadata. They show controlled test data, not a player's profile or records. The sprites retain their original C2PA AI-generation provenance; these metadata were checked for private paths and workstation details. Raw logs and internal evidence are intentionally not published.

The standalone build keeps reference copies and response files under ignored `artifacts/`. Share only the printed `MCG_AmbitionsInvaders` package folder. Git commits use the maintainer's public GitHub noreply identity.
