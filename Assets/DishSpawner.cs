using System.Collections.Generic;
using UnityEngine;

public class DishSpawner : MonoBehaviour
{
    public List<GameObject> dishPrefabs; // Add different dish prefabs here
    public Transform dishParent;

    private int dishesSpawned = 1;

    public void TrySpawnDish(int totalDishes)
    {
        if (totalDishes / 100 >= dishesSpawned && dishesSpawned < dishPrefabs.Count)
        {
            SpawnDish(dishesSpawned); // Index matches milestone
            dishesSpawned++;
        }
    }

    private void SpawnDish(int index)
    {
        GameObject prefabToSpawn = dishPrefabs[index];
        GameObject newDish = Instantiate(prefabToSpawn, dishParent);

        RectTransform rt = newDish.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;
        }

        Debug.Log($"Dish {index + 1} spawned!");
    }
}

