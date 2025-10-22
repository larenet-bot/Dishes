using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    public TMP_Text dishCountText;
    public TMP_Text profitText;

    [Header("Dish Modifiers")]
    public int dishCountIncrement = 1;
    public float profitPerDish = 1f;

    [Header("Multipliers")]
    // Global multiplier applied to every DishData.profitPerDish when awarding profit.
    // This avoids mutating ScriptableObject assets directly.
    public float dishProfitMultiplier = 1f;

    [Header("Upgrade Settings")]
    public float countUpgradeCost = 10f;
    public float profitUpgradeCost = 25f;
    public float upgradeCostIncrease = 10f;
    public int dishCountIncrementStep = 1;
    public float profitPerDishStep = 1f;

    [Header("Game References")]
    public DishSpawner dishSpawner;
    public DishClicker activeDish;

    [Header("Tracking")]
    private int totalDishes = 0;
    private float totalProfit = 0f;

    [Header("Dish Types")]
    public List<DishData> allDishes;
    private DishData currentDish;

    public static float PendingProfitAdjustment = 0f;
    public static float PendingRewardAdjustment = 0f;

    // Profit change event
    public delegate void ProfitChanged();
    public static event ProfitChanged OnProfitChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Start()
    {
        // Start with first dish if available
        if (allDishes.Count > 0)
        {
            currentDish = allDishes[0];
            activeDish.Init(currentDish);
        }

        UpdateUI();
    }

    public void NotifyProfitChanged() => OnProfitChanged?.Invoke();

    // Called when a dish is clicked enough times to complete it
    public void OnDishCleaned(DishData finishedDish)
    {
        totalDishes += dishCountIncrement;

        // Apply global dishProfitMultiplier here so upgrades that affect "dish profit"
        // don't have to modify ScriptableObject asset values.
        totalProfit += dishCountIncrement * finishedDish.profitPerDish * dishProfitMultiplier;

        NotifyProfitChanged();
        UpdateUI();

        // Pick next dish using DishSpawner
        if (dishSpawner != null && activeDish != null)
        {
            DishData nextDish = dishSpawner.GetRandomDish(totalDishes);
            if (nextDish != null)
            {
                currentDish = nextDish;
                activeDish.Init(currentDish);
            }
        }
    }

    // --- Manual Add / Subtract Profit ---
    public void AddProfit(float amount)
    {
        if (amount == 0f) return;
        totalProfit += amount;
        UpdateUI();
        NotifyProfitChanged();
    }

    public void SubtractProfit(float amount, bool isPurchase = false)
    {
        totalProfit = Mathf.Max(0, totalProfit - amount);
        if (isPurchase) PendingProfitAdjustment += amount;
        UpdateUI();
        NotifyProfitChanged();
    }

    public void AddBubbleReward(float reward)
    {
        totalProfit += reward;
        PendingRewardAdjustment += reward;
        UpdateUI();
        NotifyProfitChanged();
    }

    // Exposed API used by upgrades to multiply dish profit globally
    public void MultiplyDishProfit(float multiplier)
    {
        if (multiplier <= 0f) return;
        dishProfitMultiplier *= multiplier;
        NotifyProfitChanged();
        UpdateUI();
    }

    // Exposed API used by glove upgrades to increase the dishes-per-complete
    public void IncreaseDishCountIncrement(int amount)
    {
        if (amount == 0) return;
        dishCountIncrement += amount;
        Debug.Log($"[ScoreManager] dishCountIncrement increased by {amount} => {dishCountIncrement}");
        // Notify subscribers (UI / rate displays) and refresh UI
        NotifyProfitChanged();
        UpdateUI();
    }

    // --- Upgrades ---
    public void TryUpgradeDishCount()
    {
        if (totalProfit >= countUpgradeCost)
        {
            SubtractProfit(countUpgradeCost, true);
            dishCountIncrement += dishCountIncrementStep;
            countUpgradeCost += upgradeCostIncrease;
            Debug.Log($"Dish count increased to {dishCountIncrement}");
        }
        else
        {
            Debug.Log($"Not enough profit to upgrade dish count (need ${countUpgradeCost})");
        }
        UpdateUI();
    }

    public void TryUpgradeProfit()
    {
        if (totalProfit >= profitUpgradeCost)
        {
            SubtractProfit(profitUpgradeCost, true);
            profitPerDish += profitPerDishStep;
            profitUpgradeCost += upgradeCostIncrease;
            Debug.Log($"Profit per dish increased to ${profitPerDish:0.00}");
        }
        else
        {
            Debug.Log($"Not enough profit to upgrade profit per dish (need ${profitUpgradeCost})");
        }
        UpdateUI();
    }

    public void OnModifierCountClicked() => TryUpgradeDishCount();
    public void OnModifierProfitClicked() => TryUpgradeProfit();

    // --- Getters ---
    public int GetTotalDishes() => totalDishes;
    public float GetTotalProfit() => totalProfit;
    public float GetProfitPerDish() => profitPerDish;
    public int GetDishCountIncrement() => dishCountIncrement;

    private void UpdateUI()
    {
        if (dishCountText != null) dishCountText.text = $"Dishes: {totalDishes}";
        if (profitText != null) profitText.text = $"Profit: ${totalProfit:0.00}";
    }
}
