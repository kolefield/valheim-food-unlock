# Building Food Unlock

Requires the .NET 8 SDK, an installed copy of Valheim, and a BepInEx profile with
Jotunn. Game and dependency assemblies are referenced locally and never bundled.

Version 0.3.4 was built against Valheim 1.0.12 and Jotunn 2.30.0. Rebuilding binds the optional
arguments in `Character.Message` calls to the updated game API; an older game
installation will produce an incompatible DLL even from this source version.

```powershell
dotnet build -c Release -p:GameDir="C:\Games\Valheim" -p:ProfileDir="C:\Mods\ValheimProfile"
```

GameDir must contain `valheim_Data/Managed`. ProfileDir must contain `BepInEx/core`
and `BepInEx/plugins/ValheimModding-Jotunn/Jotunn.dll`, as installed by r2modman.
Adjust the Jotunn reference if your manual installation differs.

The current local profile is Gale's `Default`. To use it explicitly:

```powershell
dotnet build -c Release "-p:ProfileDir=$env:APPDATA\com.kesomannen.gale\valheim\profiles\Default"
./scripts/check-api.ps1 -ProfileDir "$env:APPDATA\com.kesomannen.gale\valheim\profiles\Default"
```

Output: `bin/Release/netstandard2.1/FoodUnlock.dll`.

Run `./scripts/package.ps1` to package the existing Release DLL. Build first after
code changes. The script verifies versions and creates `dist/Food_Unlock-0.3.4.zip`
with only the DLL, manifest, icon, README, changelog, and MIT license.

Regenerate the original icon on Windows with `./scripts/create-icon.ps1`.
It uses System.Drawing and no game artwork.

## Implementation

- `FoodUnlock.cs`: successful-eating observation, saves, hotkeys, and input blocking.
- `FoodBookUI.cs`: food book, icons, tooltips, assignment controls, sorting.
- `PermanentFood.cs`: optional mode, assignment resolution cache, immediate maximum
  stat updates, and Harmony hooks before food ticks/totals and after spawning.
- `PermanentFoodState.cs`: reconciliation of assigned effects and ordinary meals.
- Save key `local.foodunlock.foods.v1`: newline-separated unlocked food prefab IDs.
- Save keys `local.foodunlock.slot.v1.0` through `.2`: assigned food prefab IDs.
- Save key `local.foodunlock.permanent.v1`: shared food names managed by the
  optional mode, so stale effects can be removed after reloading. Removed when
  disabling; unlock and assignment keys are unchanged.

Missing fields represent empty unlocks/assignments. Unknown IDs are preserved but
hidden when the food prefab is unavailable. Preserve these keys across updates.

## Verification

Run the regression executable (no world, character, or running game required):

```powershell
dotnet run --project tests/FoodStateTests -c Release
./scripts/check-api.ps1
```

The executable uses the installed game's actual `Player.Food` and `ItemData`
types with synthetic records. It covers three simultaneous assignments, 3,600
decay ticks, death-style list clearing, immediate clear/replacement, duplicates,
saved ownership, ordinary-food preservation, full-stomach priority, and disabling.
It does not invoke Unity, real death, UI callbacks, or runtime Harmony patches.

See `VERIFICATION.md` for the current build evidence and pending gameplay checks.

Version 0.3.2 passed a Release build, resolution of all 89 game/Unity member
references, and an isolated Valheim 1.0.7 startup test with Jotunn 2.29.2. Both
Harmony patches loaded, the default shortcut was F7, and `Update` executed at
the main menu without the missing-method exception. Opening the book and eating
food in a world still require interactive testing.

The author confirmed 0.3.1 works in a clean BepInEx + Jotunn profile. No automated
full gameplay suite is included. Exercise unlocking, tooltips, sorting, assignment,
hotkey consumption without an item, normal eating restrictions, and save/rejoin
in-game after changes. Check runtime output. Compilation does not establish
multiplayer, controller, or cross-platform coverage.

Local game references currently produce transitive System.Net.Http and
System.IO.Compression version warnings. The mod has no direct dependency on either.
