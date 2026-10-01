# Don Quixote - Lesser Kindred Fix (Standalone)

This package contains the Donquixote `0.1.6.beta` character files and a compatibility patch for the reported Defend / Draw Pile crash. Players install and enable this one mod; the original Donquixote Workshop item is not a runtime dependency.

## Patch details

The original `DonquixoteDefend.OnPlay` awaits the character's `Cast` animation before granting Block. The patch replaces only that animation task with an already-completed task, then runs the original initializer and retains the original PCK assets. The later Block action remains intact.

This is a targeted mitigation for the observed cancellation/pause-resume race. It skips the Defend cast animation. It still needs in-game reproduction of the crash scenario to confirm the fix.

## Included content and attribution

- Original Donquixote `0.1.6.beta` PCK assets by 帽子.
- Original Donquixote DLL embedded in the standalone loader assembly, so the original Workshop mod is not required separately.
- Harmony patch for the Defend Cast animation wait.

This fork does not claim authorship of the original character or art. Obtain the original creator's permission before redistributing this combined package publicly.

## Compatibility and build

- Built against the locally installed Slay the Spire 2 public-beta assemblies (`0.107.1`).
- Requires the .NET 9 SDK and `Godot.NET.Sdk/4.5.1` to rebuild from source.
- The project reads `Donquixote.dll` and `Donquixote.pck` from the supplied mod folder. Set `DonquixoteModDir` if they are stored elsewhere.
- Run `build.ps1`; the staged install folder is `dist/DonquixoteActionFix/`.

## Install

Copy the staged mod files into the game's `mods` folder, remove/disable the separate original Donquixote copy to avoid duplicate character IDs, then enable `DonquixoteStandalone` in Mod Settings.
