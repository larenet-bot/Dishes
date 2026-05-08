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
        ResetUnlocks();

        if (allDishes != null && allDishes.Count > 0)
        {
            Debug.Log($" Starting with unlocked dish: {allDishes[0].name}");
        }
    }

    /// <summary>
    /// Clears dish progression back to the first dish.
    /// ScoreManager calls this after loading a kitchen so dish unlocks rebuild
    /// from that kitchen's saved total dishes.
    /// </summary>
    public void ResetUnlocks()
    {
        unlockedDishes.Clear();

        if (allDishes != null && allDishes.Count > 0 && allDishes[0] != null)
        {
            unlockedDishes.Add(allDishes[0]);
        }
    }

    public DishData GetRandomDish(long totalDishesCleaned)
    {
        if (allDishes == null || allDishes.Count == 0)
        {
            Debug.LogWarning(" No dishes assigned to DishSpawner!");
            return null;
        }

        if (unlockedDishes.Count == 0)
        {
            ResetUnlocks();
        }

        foreach (var dish in allDishes)
        {
            if (dish == null)
            {
                continue;
            }

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

        float totalChance = 0f;

        foreach (var dish in unlockedDishes)
        {
            if (dish == null)
            {
                continue;
            }

            totalChance += dish.spawnChance * spawnChanceMultiplier;
        }

        if (totalChance <= 0f)
        {
            Debug.LogWarning(" Total dish spawn chance is 0 or less. Falling back to first unlocked dish.");
            return unlockedDishes[0];
        }

        float roll = Random.value * totalChance;
        float cumulative = 0f;

        foreach (var dish in unlockedDishes)
        {
            if (dish == null)
            {
                continue;
            }

            cumulative += dish.spawnChance * spawnChanceMultiplier;

            if (roll <= cumulative)
            {
                Debug.Log($" Spawned dish: {dish.name} (Roll={roll:F2})");
                return dish;
            }
        }

        return unlockedDishes[unlockedDishes.Count - 1];
    }
}