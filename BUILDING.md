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
code changes. The script verifies versions and creates `dist/Food_Unlock-0.3.5.zip`
with only the DLL, manifest, icon, README, changelog, and MIT license.

Regenerate the original icon on Windows with `./scripts/create-icon.ps1`.
It uses System.Drawing and no game artwork.

## Implementation

- `FoodUnlock.cs`: successful-eating observation, saves, hotkeys, and input blocking.
- `FoodBookUI.cs`: food book, icons, tooltips, assignment controls, sorting.
- `FoodBookScale.cs`: preferred scale and fitting to the parent canvas.
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

Run the isolated scaling checks with the .NET 8 SDK:

```powershell
dotnet run --project tests/FoodBookScaleTests -c Release
```

The executable runs the same scale calculation used by the book. It covers the
default, 2×, fractional values, width and height limits, and invalid or tiny
inputs. It requires no game assemblies.

In-game UI test on a character with enough unlocked foods to scroll:

1. At **2560×1440** and **3840×2160**, compare `UI / Food book scale` at `1`,
   `1.5`, and `2`. Open with F7 and verify the title, all three slots, list,
   status text, and Close button stay fully visible. Confirm the larger values
   enlarge the book when the canvas has room.
2. At `2`, resize the game window while the book is open until the book has to
   shrink. Enlarge the window and confirm the book grows back to the requested
   size. Repeat after changing Valheim's UI scale and reopening the book.
3. At both resolutions with scaling enabled, hover food rows and assigned
   slots. Check that native tooltips appear next to the appropriate item, are
   readable near the screen edges, and disappear when the pointer moves away.
4. Click sorting, Eat, Assign, Clear, and Close. Verify the visible buttons and
   their click targets line up. Scroll up and down with the mouse wheel and by
   dragging the list. Confirm the first and last rows are reachable and the list
   stops drawing at the viewport edges.
5. Reopen and close with F7 and Escape, check the BepInEx log for exceptions,
   and restart to verify the configured scale and existing unlocks and
   assignments persist.

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
