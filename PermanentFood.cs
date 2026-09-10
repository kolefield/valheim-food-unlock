using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace FoodUnlock
{
    public sealed partial class Plugin
    {
        private static readonly System.Reflection.MethodInfo TotalFood = AccessTools.Method(typeof(Player), "GetTotalFoodValue");
        private static readonly Action<Player, float, bool> SetEitr =
            AccessTools.MethodDelegate<Action<Player, float, bool>>(AccessTools.Method(typeof(Player), "SetMaxEitr"));
        private Player lastFoodPlayer;
        private bool lastPermanentMode;
        private Player cachedFoodPlayer;
        private ObjectDB cachedFoodDB;
        private string cachedAssignments;
        private PermanentFoodState.Selection[] cachedPermanentFoods = Array.Empty<PermanentFoodState.Selection>();

        private IReadOnlyList<PermanentFoodState.Selection> DesiredPermanentFoods(Player player)
        {
            player.m_customData.TryGetValue(SaveKey, out var unlocked);
            var assignments = unlocked + "\0" + string.Join("\0", Enumerable.Range(0, 3).Select(i => AssignedFood(player, i)));
            if (cachedFoodPlayer != player || cachedFoodDB != ObjectDB.instance || cachedAssignments != assignments)
            {
                var unlocks = ReadUnlocks(player);
                cachedPermanentFoods = Enumerable.Range(0, 3).Select(i => AssignedFood(player, i))
                    .Where(unlocks.Contains).Select(name => new PermanentFoodState.Selection { PrefabName = name, Item = ResolveFood(name) })
                    .Where(selection => selection.Item != null).ToArray();
                cachedFoodPlayer = player;
                cachedFoodDB = ObjectDB.instance;
                cachedAssignments = assignments;
            }
            return cachedPermanentFoods;
        }

        private void SyncPermanentFood(Player player)
        {
            if (player != Player.m_localPlayer || player.IsDead() || ObjectDB.instance == null) return;
            PermanentFoodState.Reconcile(player.GetFoods(), player.m_customData,
                permanentAssignedFood.Value ? DesiredPermanentFoods(player) : Array.Empty<PermanentFoodState.Selection>(),
                permanentAssignedFood.Value);
        }

        private void RefreshPermanentFood(Player player)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead() || ObjectDB.instance == null) return;
            if (!permanentAssignedFood.Value && !player.m_customData.ContainsKey(PermanentFoodState.ManagedKey)) return;
            // Do not call UpdateFood(forceUpdate): that advances all food timers and the game's
            // update accumulator. Recalculate the maxima directly, without granting a heal/refill.
            SyncPermanentFood(player);
            var totals = new object[] { 0f, 0f, 0f };
            TotalFood.Invoke(player, totals);
            player.SetMaxHealth((float)totals[0], true);
            player.SetMaxStamina((float)totals[1], true);
            SetEitr(player, (float)totals[2], true);
        }

        private void UpdatePermanentMode(Player player)
        {
            if (player == null || player.IsDead()) { lastFoodPlayer = null; return; }
            if (ObjectDB.instance == null) return;
            if (player == lastFoodPlayer && lastPermanentMode == permanentAssignedFood.Value) return;
            lastFoodPlayer = player;
            lastPermanentMode = permanentAssignedFood.Value;
            RefreshPermanentFood(player);
        }

        [HarmonyPatch(typeof(Player), "UpdateFood")]
        private static class RestoreAssignedFood
        {
            private static void Prefix(Player __instance) => instance?.SyncPermanentFood(__instance);
        }

        // Runs after vanilla decay but before vanilla computes and applies maximum stats.
        [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
        private static class FullAssignedFoodBonuses
        {
            private static void Prefix(Player __instance) => instance?.SyncPermanentFood(__instance);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        private static class RestoreFoodOnSpawn
        {
            private static void Postfix(Player __instance) => instance?.RefreshPermanentFood(__instance);
        }
    }
}
