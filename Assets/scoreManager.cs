using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    public TMP_Text dishCountText;
    public TMP_Text profitText;

    [Header("Upgrade Buttons")]
    public Button countUpgradeButton;
    public Button profitUpgradeButton;

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

    public void AddScore()
    {
        totalDishes += dishCountIncrement;
        totalProfit += dishCountIncrement * profitPerDish;

        dishSpawner?.TrySpawnDish(totalDishes);

        UpdateUI();
    }


    public void SubtractProfit(float amount)
    {
        totalProfit = Mathf.Max(0, totalProfit - amount);
        UpdateUI();
    }

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

        // Update button interactivity based on current profit
        if (countUpgradeButton != null)
            countUpgradeButton.interactable = totalProfit >= countUpgradeCost;

        if (profitUpgradeButton != null)
            profitUpgradeButton.interactable = totalProfit >= profitUpgradeCost;
    }

    // Upgrade dish count
    public void TryUpgradeDishCount()
    {
        if (totalProfit >= countUpgradeCost)
        {
            SubtractProfit(countUpgradeCost);
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
            SubtractProfit(profitUpgradeCost);
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
