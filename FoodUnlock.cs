using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;

namespace FoodUnlock
{
    [BepInPlugin(Id, "Food Unlock", "0.3.1")]
    [BepInDependency("com.jotunn.jotunn")]
    public sealed partial class Plugin : BaseUnityPlugin
    {
        private const string Id = "local.foodunlock";
        // Versioned, namespaced character data leaves other mods' save fields alone.
        private const string SaveKey = Id + ".foods.v1";
        private const string SlotKey = Id + ".slot.v1.";
        private static Plugin instance;
        private Harmony harmony;
        private static readonly System.Reflection.MethodInfo TakeInput = AccessTools.Method(typeof(Player), "TakeInput");
        private ConfigEntry<KeyboardShortcut> toggle;
        private readonly ConfigEntry<KeyCode>[] slotKeys = new ConfigEntry<KeyCode>[3];
        private Player bookPlayer;
        private bool visible;
        private readonly List<ItemDrop.ItemData> foods = new List<ItemDrop.ItemData>();
        private string status = "";

        private void Awake()
        {
            instance = this;
            toggle = Config.Bind("Controls", "Food book", new KeyboardShortcut(KeyCode.F7),
                "Open or close the food book. Eat a real food once to unlock it.");
            var defaults = new[] { KeyCode.Z, KeyCode.V, KeyCode.B };
            for (var i = 0; i < slotKeys.Length; i++)
                slotKeys[i] = Config.Bind("Controls", "Food slot " + (i + 1), defaults[i],
                    "Eat the food assigned in the book. Works outside menus, without Alt/Ctrl/Shift.");
            harmony = new Harmony(Id);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Food Unlock 0.3.1 loaded. Food book shortcut: " + toggle.Value);
        }

        private static bool IsFood(ItemDrop.ItemData item)
        {
            return item?.m_shared != null && item.m_dropPrefab != null
                && item.m_shared.m_food > 0 && item.m_shared.m_foodBurnTime > 0
                && item.m_shared.m_consumeStatusEffect == null;
        }

        private static HashSet<string> ReadUnlocks(Player player)
        {
            return new HashSet<string>(player.m_customData.TryGetValue(SaveKey, out var saved)
                ? saved.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>(), StringComparer.Ordinal);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.EatFood), typeof(ItemDrop.ItemData))]
        private static class LearnFood
        {
            private static void Postfix(Player __instance, ItemDrop.ItemData item, bool __result)
            {
                if (!__result || __instance != Player.m_localPlayer || !IsFood(item)) return;
                var unlocked = ReadUnlocks(__instance);
                if (!unlocked.Add(item.m_dropPrefab.name)) return;
                __instance.m_customData[SaveKey] = string.Join("\n", unlocked.OrderBy(x => x, StringComparer.Ordinal));
                __instance.Message(MessageHud.MessageType.TopLeft,
                    "Food unlocked: " + Localization.instance.Localize(item.m_shared.m_name));
                instance.Logger.LogInfo("Unlocked food: " + item.m_dropPrefab.name);
                if (instance.visible)
                {
                    instance.RefreshFoods();
                    instance.RebuildRows();
                }
            }
        }

        private void Update()
        {
            var player = Player.m_localPlayer;
            if (visible && (player == null || player != bookPlayer || player.IsDead())) Close();
            if (visible)
            {
                if (toggle.Value.IsDown() || Input.GetKeyDown(KeyCode.Escape)) Close();
                else UpdateBook();
                return;
            }
            if (player == null || player.IsDead()) return;
            var openBook = toggle.Value.IsDown();
            var slot = PressedSlot(player);
            if (!openBook && slot < 0) return;
            if (!(bool)TakeInput.Invoke(player, null)) return;
            if (!openBook)
            {
                var name = AssignedFood(player, slot);
                var food = ResolveFood(name);
                if (food != null && ReadUnlocks(player).Contains(name))
                    Eat(player, food);
                else player.Message(MessageHud.MessageType.TopLeft, "Assigned food unavailable. Choose another in the food book.");
                return;
            }
            bookPlayer = player;
            status = "";
            RefreshFoods();
            if (!CreateBook()) { bookPlayer = null; return; }
            visible = true;
            GUIManager.BlockInput(true);
        }

        private static string AssignedFood(Player player, int slot)
            => player.m_customData.TryGetValue(SlotKey + slot, out var name) ? name : "";

        private int PressedSlot(Player player, bool held = false)
        {
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)
                || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return -1;
            for (var i = 0; i < slotKeys.Length; i++)
                if (slotKeys[i].Value != KeyCode.None
                    && (held ? Input.GetKey(slotKeys[i].Value) : Input.GetKeyDown(slotKeys[i].Value))
                    && AssignedFood(player, i).Length > 0) return i;
            return -1;
        }

        // Suppress only a simultaneous auto-run action when an assigned food key is pressed.
        // No persistent changes to the user's game bindings or other mods' key bindings.
        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButton), typeof(string))]
        private static class AvoidAutoRun
        {
            private static void Postfix(string __0, ref bool __result)
            {
                if (!__result || __0 != "AutoRun" || instance == null || instance.visible) return;
                var player = Player.m_localPlayer;
                if (player != null && !player.IsDead() && instance.PressedSlot(player, true) >= 0)
                    __result = false;
            }
        }

        private static ItemDrop.ItemData ResolveFood(string name)
        {
            if (string.IsNullOrEmpty(name) || ObjectDB.instance == null) return null;
            var prefab = ObjectDB.instance.GetItemPrefab(name);
            var item = prefab != null ? prefab.GetComponent<ItemDrop>()?.m_itemData : null;
            if (item == null) return null;
            var copy = item.Clone();
            copy.m_dropPrefab = prefab;
            return IsFood(copy) ? copy : null;
        }

        private void Eat(Player player, ItemDrop.ItemData food)
        {
            // Detached serving: inventory and container contents are never changed.
            var serving = food.Clone();
            serving.m_dropPrefab = food.m_dropPrefab;
            if (player.CanConsumeItem(serving, true) && player.EatFood(serving))
            {
                status = "Ate " + DisplayName(serving) + ". No item consumed.";
                player.Message(MessageHud.MessageType.TopLeft, status);
            }
            else status = "You cannot eat that right now.";
        }

        private void RefreshFoods()
        {
            foods.Clear();
            if (bookPlayer == null || ObjectDB.instance == null) return;
            foreach (var name in ReadUnlocks(bookPlayer))
            {
                var food = ResolveFood(name);
                if (food != null) foods.Add(food);
            }
            SortFoods();
        }

        private static string DisplayName(ItemDrop.ItemData food)
            => Localization.instance.Localize(food.m_shared.m_name);

        private void Close()
        {
            if (!visible) return;
            visible = false;
            GUIManager.BlockInput(false);
            DestroyBook();
            bookPlayer = null;
            foods.Clear();
        }


        private void OnDestroy()
        {
            Close();
            harmony?.UnpatchSelf();
            if (instance == this) instance = null;
        }
    }
}
