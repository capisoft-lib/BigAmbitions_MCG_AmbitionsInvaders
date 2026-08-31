# Ambitions Invaders 1.0.0 publication checklist

These files prepare a release; they do not create a Steam item or configure Workshop dependencies. Native Big Ambitions gameplay verification is a separate step from compilation and isolated Unity tests.

The boxes below are operator checks for a Workshop upload, not a live deployment status. See [verification scope](../../VERIFICATION.md) for the checks already performed on the source and package.

## Publication fields

| Field | English | French |
| --- | --- | --- |
| Title | [title.txt](title.txt) | Same title |
| Short description | [short-description.txt](short-description.txt) | [short-description.fr.txt](short-description.fr.txt) |
| Full description (BBCode) | [full-description.md](full-description.md) | [full-description.fr.md](full-description.fr.md) |
| Update notes (BBCode) | [Steam_ChangeLog.md](Steam_ChangeLog.md) | [Steam_ChangeLog.fr.md](Steam_ChangeLog.fr.md) |

Icon: [Thumbnail.jpg](../../Thumbnail.jpg). The [PNG master](../../release-assets/invaders-cover-master.png) and [exact generation prompt](../../release-assets/PROMPT.md) remain in the source repository, outside the installed package. The cover is promotional artwork. The README's English gameplay images are isolated Unity renders.

## Build and installation

- [ ] Compile with the actual MCG 1.0.0+ DLL and matching manifest; check versions and hashes in the ignored build status file.
- [ ] Verify manifest/catalog version 1.0.0, assembly/file version 1.0.0.0, stable technical IDs and record ruleset.
- [ ] Verify one own DLL, 22 locales, four sprites, thumbnail and expected documents; no symbols, private paths, logs, user data or bundled dependencies.
- [ ] Verify all tests and reproducible package hashes, then install only `MCG_AmbitionsInvaders` while the game is closed. Preserve MCG records and back up the previous package outside ModsLocal.
- [ ] Audit and push the complete source tree and history with a public noreply identity.

## Before Steam publication

- [ ] On a copy of a save, test native computer entry, movement/fire, completed rounds, Tab exit, Backspace menu, Escape pause and records after restarting Big Ambitions.
- [ ] Check the English/French UI and actual thumbnail in the Steam publication interface.
- [ ] Select only the built `MCG_AmbitionsInvaders` directory as mod content, never the source repository, parent build directory or dependencies.
- [ ] Add [LIB MCG — Workshop item 3793604724](https://steamcommunity.com/sharedfiles/filedetails/?id=3793604724) to **Required Items**. No other mod library is required. Keep the Workshop link and mandatory MCG 1.0.0+ installation/activation warning in both descriptions; Required Items do not enforce semantic versions.
- [ ] Test a downloaded Workshop copy without an active duplicate ModsLocal installation, then choose the intended visibility.

Steam upload, item creation and visibility changes are not performed by this preparation. Publishing sources on GitHub is separate from publishing a compiled release asset.
