using System;
using System.Collections.Generic;
using System.Linq;
using FoodUnlock;
using Selection = FoodUnlock.PermanentFoodState.Selection;

static class Program
{
    static int checks;
    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAIL: " + label);
        checks++;
        System.Console.WriteLine("PASS: " + label);
    }

    static Selection Meal(string name, float hp, float stamina, float eitr) => new Selection
    {
        PrefabName = name,
        Item = new ItemDrop.ItemData { m_shared = new ItemDrop.ItemData.SharedData
        { m_name = "$" + name, m_food = hp, m_foodStamina = stamina, m_foodEitr = eitr, m_foodBurnTime = 1200 } }
    };

    static Player.Food Serving(Selection meal, float time) => new Player.Food
    { m_name = meal.PrefabName, m_item = meal.Item, m_time = time, m_health = 1, m_stamina = 2, m_eitr = 3 };

    static void Main()
    {
        var a = Meal("HealthMeal", 100, 20, 0);
        var b = Meal("StaminaMeal", 20, 100, 0);
        var c = Meal("EitrMeal", 20, 20, 100);
        var d = Meal("Replacement", 60, 60, 60);
        var data = new Dictionary<string, string> { ["unrelated"] = "keep" };
        var active = new List<Player.Food>();
        var assigned = new[] { a, b, c };
        PermanentFoodState.Reconcile(active, data, assigned, false);
        Check(active.Count == 0 && data.Count == 1, "disabled mode leaves assignments inactive and data untouched");
        PermanentFoodState.Reconcile(active, data, assigned, true);
        Check(active.Select(f => f.m_name).SequenceEqual(new[] { a.PrefabName, b.PrefabName, c.PrefabName }), "all three slots apply");
        Check(active[0].m_health == 100 && active[1].m_stamina == 100 && active[2].m_eitr == 100, "maximum health, stamina and eitr contributions");
        for (var tick = 0; tick < 3600; tick++)
        {
            foreach (var food in active)
            {
                food.m_time--;
                var decay = (float)Math.Pow(Math.Clamp(food.m_time / 1200, 0, 1), .3);
                food.m_health *= decay; food.m_stamina *= decay; food.m_eitr *= decay;
            }
            PermanentFoodState.Reconcile(active, data, assigned, true);
        }
        Check(active.All(f => f.m_time == 1200 && f.m_health == f.m_item.m_shared.m_food
            && f.m_stamina == f.m_item.m_shared.m_foodStamina && f.m_eitr == f.m_item.m_shared.m_foodEitr), "3600 simulated decay ticks remain exactly full");
        active.Clear(); // Vanilla death clears m_foods but retains character custom data.
        PermanentFoodState.Reconcile(active, data, assigned, true);
        Check(active.Count == 3 && active[2].m_eitr == 100, "restores food after death-style list clearing");
        PermanentFoodState.Reconcile(active, data, new[] { a, c }, true);
        Check(active.Count == 2 && active.All(f => f.m_name != b.PrefabName), "clearing a slot removes its effect immediately");
        PermanentFoodState.Reconcile(active, data, new[] { d, c }, true);
        Check(active.Count == 2 && active.Any(f => f.m_name == d.PrefabName) && active.All(f => f.m_name != a.PrefabName), "replacement removes old food and applies new food immediately");
        PermanentFoodState.Reconcile(active, data, new[] { d, d, c }, true);
        Check(active.Count == 2, "duplicate slot assignments do not stack");
        PermanentFoodState.Reconcile(active, data, new[] { d, c }, true);
        Check(active.Count == 2, "clearing one duplicate keeps the remaining assignment active");
        var reloadedData = new Dictionary<string, string>(data);
        var reloadedFoods = active.Select(f => Serving(new Selection { PrefabName = f.m_name, Item = f.m_item }, 800)).ToList();
        PermanentFoodState.Reconcile(reloadedFoods, reloadedData, new[] { c }, true);
        Check(reloadedFoods.Count == 1 && reloadedFoods[0].m_name == c.PrefabName, "saved ownership removes stale food after reload");
        active.Add(Serving(a, 700));
        PermanentFoodState.Reconcile(active, data, Array.Empty<Selection>(), true);
        Check(active.Count == 1 && active[0].m_name == a.PrefabName && active[0].m_time == 700, "clear all preserves unrelated ordinary food and its timer");
        active.Add(Serving(b, 100)); active.Add(Serving(c, 600));
        PermanentFoodState.Reconcile(active, data, new[] { d }, true);
        Check(active.Count == 3 && active.Any(f => f.m_name == d.PrefabName) && active.All(f => f.m_name != b.PrefabName), "full stomach evicts most depleted unassigned meal");
        PermanentFoodState.Reconcile(active, data, new[] { a, c, d }, true);
        Check(active.Count == 3 && active.All(f => f.m_time == 1200), "existing meals promoted without duplication");
        PermanentFoodState.Reconcile(active, data, assigned, false);
        active[0].m_time--;
        PermanentFoodState.Reconcile(active, data, assigned, false);
        Check(active.Count == 3 && active[0].m_time == 1199 && !data.ContainsKey(PermanentFoodState.ManagedKey), "disabling releases ownership and permits normal decay");
        Check(data["unrelated"] == "keep", "unrelated character save fields preserved");
        System.Console.WriteLine($"{checks} checks passed using installed Valheim food record types. No Unity lifecycle or live gameplay exercised.");
    }
}
