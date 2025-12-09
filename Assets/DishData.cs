using UnityEngine;

[CreateAssetMenu(fileName = "NewDishData", menuName = "Dish/Dish Data")]
public class DishData : ScriptableObject
{
    [Header("Prefab Reference")]
    public GameObject dishPrefab;

    [Header("Visuals")]
    public Sprite[] stageSprites; // Clean stages

    [Header("Economy")]
    public float profitPerDish = 1f;

    [Header("Gameplay")]
    public int clicksRequired = 3; // clicks needed to clean

    [Header("Progression")]
    public int unlockAtDishCount = 0; // When this dish is unlocked

    [Header("Spawn Settings")]
    [Range(0f, 1f)] public float spawnChance = 1f; // chance among unlocked dishes

    [Header("UI")]
    [Tooltip("Scale factor for the main dish button. 1 = baseline plate size, >1 = bigger, <1 = smaller.")]
    public float uiScale = 1f;
}