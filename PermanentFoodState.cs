using System;
using System.Collections.Generic;
using System.Linq;

namespace FoodUnlock
{
    // Works on actual game food records; no player, UI, or Unity lifecycle dependencies.
    internal static class PermanentFoodState
    {
        internal const string ManagedKey = "local.foodunlock.permanent.v1";

        internal sealed class Selection
        {
            internal string PrefabName;
            internal ItemDrop.ItemData Item;
        }

        internal static void Reconcile(List<Player.Food> active, IDictionary<string, string> data,
            IReadOnlyList<Selection> assigned, bool enabled)
        {
            if (!enabled)
            {
                // Existing servings resume normal decay when the option is disabled.
                data.Remove(ManagedKey);
                return;
            }

            // Valheim identifies duplicate foods by shared name, not prefab ID.
            var desired = assigned.GroupBy(selection => selection.Item.m_shared.m_name, StringComparer.Ordinal)
                .Select(group => group.First()).Take(3).ToArray();
            var names = new HashSet<string>(desired.Select(selection => selection.Item.m_shared.m_name), StringComparer.Ordinal);
            var previous = new HashSet<string>(data.TryGetValue(ManagedKey, out var saved)
                ? saved.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>(), StringComparer.Ordinal);

            active.RemoveAll(food => previous.Contains(food.m_item.m_shared.m_name)
                && !names.Contains(food.m_item.m_shared.m_name));

            foreach (var selection in desired)
            {
                var item = selection.Item;
                var food = active.Find(entry => entry.m_item.m_shared.m_name == item.m_shared.m_name);
                if (food == null)
                {
                    // Make room without displacing another assigned food. Leave other meals alone
                    // unless capacity requires eviction, preferring the most depleted ordinary meal.
                    while (active.Count >= 3)
                    {
                        var replace = active.Where(entry => !names.Contains(entry.m_item.m_shared.m_name))
                            .OrderBy(entry => entry.m_time).FirstOrDefault();
                        if (replace == null) break;
                        active.Remove(replace);
                    }
                    if (active.Count >= 3) continue;
                    food = new Player.Food { m_name = selection.PrefabName, m_item = item };
                    active.Add(food);
                }
                food.m_time = item.m_shared.m_foodBurnTime;
                food.m_health = item.m_shared.m_food;
                food.m_stamina = item.m_shared.m_foodStamina;
                food.m_eitr = item.m_shared.m_foodEitr;
            }

            if (names.Count == 0) data.Remove(ManagedKey);
            else data[ManagedKey] = string.Join("\n", names.OrderBy(name => name, StringComparer.Ordinal));
        }
    }
}
