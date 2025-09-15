using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    public TMP_Text dishCountText;
    public TMP_Text profitText;

    [Header("Tracking")]
    private int totalDishes = 0;
    private float totalProfit = 0f;

    [Header("Dish Modifiers")]
    public int dishCountIncrement = 1;
    public float profitPerDish = 1f;

    [Header("Upgrade Settings")]
    public float countUpgradeCost = 10f;
    public float profitUpgradeCost = 25f;

    public float upgradeCostIncrease = 10f; // Flat cost increase per upgrade
    public int dishCountIncrementStep = 1;   // Each upgrade adds this amount
    public float profitPerDishStep = 1f;     // Each upgrade adds this amount
    private DishSpawner dishSpawner;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        // Replace the deprecated FindObjectOfType with FindFirstObjectByType  
        dishSpawner = Object.FindFirstObjectByType<DishSpawner>();

        UpdateUI();
    }

    // Event for profit changes
    public delegate void ProfitChanged();
    public static event ProfitChanged OnProfitChanged;

    private void NotifyProfitChanged()
    {
        OnProfitChanged?.Invoke();
    }
    // central adder used by EmployeeManager and others
    public void AddProfit(float amount)
    {
        if (amount == 0f) return;
        totalProfit += amount;
        // If you want to track sources, you can add a separate pending bucket here
        // PendingRewardAdjustment += amount; // optional
        UpdateUI();
        NotifyProfitChanged();
    }
    // Add score from clicks or actions
    public void AddScore()
    {
        totalDishes += dishCountIncrement;
        totalProfit += dishCountIncrement * profitPerDish;

        UpdateUI();
        NotifyProfitChanged();
    }

    public static float PendingProfitAdjustment = 0f;
    public static float PendingRewardAdjustment = 0f;

    // Subtract profit when purchasing
    public void SubtractProfit(float amount, bool isPurchase = false)
    {
        totalProfit = Mathf.Max(0, totalProfit - amount);
        if (isPurchase)
            PendingProfitAdjustment += amount;
        UpdateUI();
        NotifyProfitChanged();
    }

    // Add profit from bubbles or other sources
    public void AddBubbleReward(float reward)
    {
        totalProfit += reward;
        PendingRewardAdjustment += reward;
        UpdateUI();
        NotifyProfitChanged();
    }

    // --- Getters ---
    public int GetTotalDishes() => totalDishes;
    public float GetTotalProfit() => totalProfit;
    public float GetProfitPerDish() => profitPerDish;
    public int GetDishCountIncrement() => dishCountIncrement;

    private void UpdateUI()
    {
        if (dishCountText != null)
            dishCountText.text = $"Dishes: {totalDishes}";

        if (profitText != null)
            profitText.text = $"Profit: ${totalProfit:0.00}";
    }

    // Upgrade dish count
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

    // Upgrade profit per dish
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

    // Button handlers
    public void OnModifierCountClicked() => TryUpgradeDishCount();
    public void OnModifierProfitClicked() => TryUpgradeProfit();

}
