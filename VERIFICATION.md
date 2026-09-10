# Food Unlock 0.3.3 verification — 2026-09-10

## Completed

- Release build against the installed Valheim 1.0.7 assemblies: zero errors.
  Two transitive assembly-version warnings (`System.Net.Http` and
  `System.IO.Compression`) remain.
- 15 executable checks using real game food-record types and synthetic data:
  disabled mode, three assignments, all three maximum bonuses, 3,600 simulated
  decay ticks, restoration after death-style list clearing, immediate clearing
  and replacement, duplicate assignments, saved ownership after reload,
  preservation of ordinary meals, full-stomach priority, and disabling the mode.
- All 99 referenced game/Unity members resolve against the installed assemblies.
- Five string-based method signatures checked, five Harmony patch declarations
  present, and the compiled configuration default verified as `false`.
- Package contains only the mod DLL, manifest, README, changelog, license, and icon.

Commands are in `BUILDING.md`. Local build/test/API logs and checksums are in
`dist/`. Game/dependency binaries copied to test output are not packaged or tracked.

SHA-256 of `bin/Release/netstandard2.1/FoodUnlock.dll`:

```text
5664900444784F9FC33BD3C80D4D06556B693B886F8CF1B58103D6E1BC5268B5
```

SHA-256 of `dist/Food_Unlock-0.3.3.zip`:

```text
4D1DA893C3CA893B092F48873CD3EE28C7CE9FEFC45C0E5DB0AA395FB829890B
```

## Local installation

After Valheim exited normally, version 0.3.3 was installed in the Default profile
at `BepInEx/plugins/Kolefield-Food_Unlock/FoodUnlock/FoodUnlock.dll`. The original
DLL was backed up outside plugin discovery, the installed SHA-256 matched the
tested artifact above, and an assembly scan found exactly one active
`local.foodunlock` plugin identity. Configuration, manager package state,
characters, and worlds were preserved.

## Pending: runtime verification

Runtime Harmony installation, UI callbacks, real death/respawn, multiplayer,
and other-mod interactions have **not** been independently tested for 0.3.3.

After exiting Valheim normally:

1. Use an isolated BepInEx/Jotunn profile and separate save directory for the
   runtime smoke test. Verify Food Unlock 0.3.3 initializes with all five patches
   and no exceptions.
2. Launch the intended profile through r2modman → Start modded. Confirm the new
   setting defaults off and normal hotkey eating/timers still work.
3. Enable `Food / Permanent assigned food`. Assign three different unlocked
   foods, including an eitr food. Verify all three full bonuses and frozen timers.
4. Clear each slot and replace it while other slots remain assigned; confirm the
   old effect disappears immediately and the new one applies. Test a full stomach.
5. Die and respawn, then log out/rejoin. Verify assignments restore full effects.
   Test duplicates (one shared effect) and disable the setting (normal decay).

Rollback after installation: exit normally and restore the backed-up DLL.
The new namespaced ownership field is ignored by older versions; existing unlock
and slot save keys remain unchanged. Manager updates can overwrite local builds.
