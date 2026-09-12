# Changelog

## 0.3.4

- Added Eitr sorting, highest first, with alphabetical ordering for ties.
- Displayed each food's base health regeneration in HP per 10 seconds in the selection list.
- Increased row height to keep regeneration text separate from assignment buttons.

## 0.3.3

- Added off-by-default `Food / Permanent assigned food`: assigned foods automatically apply at maximum health, stamina, and eitr bonuses without decay, and restore after death/rejoin.
- Clearing or replacing a slot immediately releases the old effect. Duplicate assignments share one effect. Assigned food takes priority within the three-food stomach limit.
- Disabling the mode resumes ordinary food decay. Existing unlocks and assignments are preserved.
- Added executable food-state regression checks using the installed game's food record types.

## 0.3.2

- Rebuilt against Valheim 1.0.7 to use its updated Character.Message API, fixing the per-frame MissingMethodException that prevented F7 and food hotkeys from running.
- Preserves existing food unlocks, slot assignments, and controls.

## 0.3.1

- First public release; increased mouse-wheel scrolling speed.
- Tested in a clean profile with BepInEx and Jotunn.

## 0.3.0

- Added a Valheim-style panel using Jotunn, food icons, native tooltips, and sorting.

## 0.2.0

- Added three character-specific food assignments with Z/V/B hotkeys.
- Prevented assigned food keys from also triggering auto-run.

## 0.1.0

- Initial eat-once-to-unlock prototype, food book, and character save support.
