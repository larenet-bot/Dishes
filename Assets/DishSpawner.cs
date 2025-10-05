using System.Collections.Generic;
using UnityEngine;

public class DishSpawner : MonoBehaviour
{
    [Header("Dish Pool")]
    public List<DishData> allDishes;

    [Header("Spawn Chance Multiplier")]
    [Range(0.01f, 10f)]
    public float spawnChanceMultiplier = 1f;

    private List<DishData> unlockedDishes = new List<DishData>();

    private void Start()
    {
        // Ensure the first dish is always unlocked
        if (allDishes.Count > 0 && !unlockedDishes.Contains(allDishes[0]))
        {
            unlockedDishes.Add(allDishes[0]);
            Debug.Log($" Starting with unlocked dish: {allDishes[0].name}");
        }
    }

    public DishData GetRandomDish(int totalDishesCleaned)
    {
        // Unlock new dishes when threshold is reached
        foreach (var dish in allDishes)
        {
            if (totalDishesCleaned >= dish.unlockAtDishCount && !unlockedDishes.Contains(dish))
            {
                unlockedDishes.Add(dish);
                Debug.Log($" Unlocked new dish: {dish.name} (Threshold: {dish.unlockAtDishCount})");
            }
        }

        if (unlockedDishes.Count == 0)
        {
            Debug.LogWarning(" No unlocked dishes found!");
            return null;
        }

        // Weighted random selection based on spawnChance and multiplier
        float totalChance = 0f;
        foreach (var dish in unlockedDishes)
            totalChance += dish.spawnChance * spawnChanceMultiplier;

        float roll = Random.value * totalChance;
        float cumulative = 0f;

        foreach (var dish in unlockedDishes)
        {
            cumulative += dish.spawnChance * spawnChanceMultiplier;
            if (roll <= cumulative)
            {
                Debug.Log($" Spawned dish: {dish.name} (Roll={roll:F2})");
                return dish;
            }
        }

        // fallback (shouldn’t happen, but safe)
        return unlockedDishes[unlockedDishes.Count - 1];
    }
}
