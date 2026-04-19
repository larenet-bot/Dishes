using UnityEngine;

[CreateAssetMenu(fileName = "DishData", menuName = "Dishes/Dish Data", order = 1)]
public class DishData : ScriptableObject
{
    [Header("Identification")]
    public string displayName;

    [Header("Visual Stages")]
    [Tooltip("Ordered sprites for dish stages. Index 0 = dirtiest, last index = clean.")]
    public Sprite[] stageSprites = new Sprite[0];

    [Header("UI")]
    [Tooltip("Per-dish UI scale applied in DishVisual.SetDish")]
    public float uiScale = 1f;

    [Header("Gameplay")]
    [Tooltip("Profit awarded per dish cleaned")]
    public float profitPerDish = 1f;

    [Tooltip("Number of dishes cleaned (total) required to unlock this dish")]
    public int unlockAtDishCount = 0;

    [Tooltip("Relative spawn chance used by DishSpawner")]
    public float spawnChance = 1f;
}