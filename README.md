# Food Unlock

**Eat a food once. Unlock unlimited servings.**

A Valheim-style food book with icons, native hover tooltips, health/stamina/eitr sorting,
and three assignable hotkeys. Skip repeat food farming while keeping normal food
bonuses, timers, and stomach limits by default. An optional permanent-food mode
keeps assigned foods at full strength through death.

## How to use

1. Eat a real food normally to unlock it for your character.
2. Press **F7** to open the food book.
3. Click **Eat** beside an unlocked food whenever your stomach allows it. No item
   is needed in your inventory or consumed from nearby containers.
4. Use **Assign Z**, **Assign V**, or **Assign B** to put foods on hotkeys. Close
   the book and press the corresponding key to eat.

The three assignments appear above the list. Use **Clear** to remove an assignment.
Hover a food for its item tooltip. Sort by **Name**, **Health**, **Stamina**, or **Eitr**;
numeric stats sort highest first, with alphabetical ordering for ties. Each food
also shows its base health regeneration in HP per 10 seconds, before status-effect
modifiers. Scroll with the mouse wheel or drag the list.
Press F7, Escape, or Close to close the book.

**The world keeps running while the book is open.**

## Install

Install with **r2modman** or **Thunderstore Mod Manager**, then launch modded.
The package declares these required dependencies for automatic installation:

- [BepInExPack_Valheim 5.4.2333](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jotunn 2.29.2](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)

For manual installation, install the dependencies first and copy `FoodUnlock.dll`
from the release ZIP's `plugins/FoodUnlock/` directory to your profile's
`BepInEx/plugins/FoodUnlock/` directory. Restart the game.

If you installed a development copy manually, remove that old `FoodUnlock.dll`
before installing the managed package to avoid duplicate copies.

## Controls and settings

| Control | Default |
| --- | --- |
| Open/close food book | F7 |
| Eat assigned food 1 | Z |
| Eat assigned food 2 | V |
| Eat assigned food 3 | B |

Change bindings in `BepInEx/config/local.foodunlock.cfg` after the first launch,
or with an optional BepInEx Configuration Manager. Food keys require no Alt, Ctrl,
or Shift modifiers and do not operate in chat, inventory, or standard game menus.
Use distinct bindings. Alt+Z/V/B remain available for other mods.

An assigned food key suppresses simultaneous auto-run while held; clearing that
assignment restores normal behavior.

### Permanent assigned food (optional)

In `BepInEx/config/local.foodunlock.cfg`, set:

```ini
[Food]
Permanent assigned food = true
```

This setting is **off by default**. With it enabled, assigning an unlocked food to
Z, V, or B automatically applies its full health, stamina, and eitr bonuses. Its
timer and bonuses stay at maximum, and assigned foods return automatically after
death and on rejoining. You do not need to press the food hotkeys.

Clearing a slot immediately removes its food effect. Replacing an assignment
immediately removes the old effect and applies the new food. Duplicate assignments
share one effect; it remains until the last slot assigning that food is cleared.
Choose three different foods for three effects.

The three-food stomach limit remains. If space is needed, assigned food replaces
the most depleted ordinary, unassigned food. Other ordinary foods retain normal
timers and behavior. Missing or no-longer-unlocked foods cannot be applied.

This holds the food's **bonuses** at maximum; it does not refill your current
health, stamina, or eitr continuously. Disabling the setting lets active food
resume normal decay. Edit the file while the game is closed, or change the setting
live through BepInEx Configuration Manager.

## Unlock rules

- Unlocks happen on **successful eating**, not crafting or discovering a recipe.
- Foods eaten before installing the mod must be eaten again to unlock.
- Normal raw edible foods, including berries, count.
- Meads, potions, and foods with consumption status effects are excluded.
- Food effects expire normally unless permanent assigned food is enabled.
- Unlocks and assignments are stored per character in normal character saves.
  Log out normally to save your progress.

Recipes, inventory layouts, and storage are unchanged.

## Compatibility

Version 0.3.1 was player-tested on Windows in a clean profile with only BepInEx,
Jotunn, and Food Unlock. It was also used alongside AzuCraftyBoxes,
AzuExtendedPlayerInventory, PlantEasily, PlantEverything, Sailing, TargetPortal,
AutoRepair, AzuAutoStore, and BepInEx Configuration Manager.

This is a small early release. Dedicated-server/multiplayer behavior, controller
navigation, Linux/Proton, and arbitrary food-overhaul mods have not been validated.
There is no server-enforced unlock checking or configuration synchronization.

## Uninstall

Close the game and uninstall through your mod manager, or remove the manually
installed FoodUnlock plugin folder. The namespaced unlock record remains in the
character save but is unused without the mod. No game assets are replaced.

## Feedback and source

[Source and releases](https://github.com/kolefield/valheim-food-unlock)
 · [Report a bug](https://github.com/kolefield/valheim-food-unlock/issues)

For a bug report, include mod/game versions, reproduction steps, and relevant
errors from `BepInEx/LogOutput.log`. Remove private information before sharing logs.

See [BUILDING.md](https://github.com/kolefield/valheim-food-unlock/blob/main/BUILDING.md)
for development instructions. Source and the original package icon are MIT licensed.
Valheim assets are loaded from the installed game and are not distributed here.
