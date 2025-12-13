using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static ConvertDecimal;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

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
    private long totalDishes = 0;
    private float totalProfit = 0f;

    [Header("Dish Types")]
    public List<DishData> allDishes;
    private DishData currentDish;

    public static float PendingProfitAdjustment = 0f;
    public static float PendingRewardAdjustment = 0f;

    [Header("Employee Dish Production")]
    [SerializeField] private EmployeeManager employeeManager;   // Drag your EmployeeManager here in Inspector
    [SerializeField] private bool enableEmployeeDishTick = true;

    // Carries fractional dish progress so displays stay integer-only
    private double employeeDishAccumulator = 0d;

    // Optional: how often we refresh UI when employees add dishes (seconds)
    [SerializeField] private float employeeDishUIRefresh = 0.25f;
    private float employeeDishUITimer = 0f;

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

        if (enableEmployeeDishTick && employeeManager != null)
            StartCoroutine(EmployeeDishTicker());

        UpdateUI();
    }

    public void NotifyProfitChanged() => OnProfitChanged?.Invoke();

    // Called when a dish is clicked enough times to complete it
    // Called when a dish is clicked enough times to complete it
    public float OnDishCleaned(DishData finishedDish)
    {
        if (finishedDish == null) return 0f;

        totalDishes += dishCountIncrement;

        // Apply global dishProfitMultiplier here so upgrades that affect
        // "dish profit" don't have to modify ScriptableObject asset values.
        float reward = dishCountIncrement * finishedDish.profitPerDish * dishProfitMultiplier;
        totalProfit += reward;

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

        return reward;
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

    private System.Collections.IEnumerator EmployeeDishTicker()
    {
        var wait = new WaitForEndOfFrame();
        while (true)
        {
            yield return wait;

            if (!enableEmployeeDishTick || employeeManager == null) continue;

            // Dishes per second from all employees (static dish intervals × live counts)
            double dps = employeeManager.GetTotalDishesPerSecond();

            // Accumulate fractional dishes with frame time
            employeeDishAccumulator += dps * Time.deltaTime;

            // When we have at least 1 whole dish, apply it to total (integer-safe)
            if (employeeDishAccumulator >= 1.0)
            {
                long whole = (long)System.Math.Floor(employeeDishAccumulator);
                AddDishesFromEmployees(whole);           // increments your total & handles UI
                employeeDishAccumulator -= whole;
            }

            // Throttle UI refresh (in case no whole dishes happened for a bit)
            employeeDishUITimer += Time.deltaTime;
            if (employeeDishUITimer >= employeeDishUIRefresh)
            {
                employeeDishUITimer = 0f;
                UpdateUI(); // uses BigNumberFormatter in your existing method
            }
        }
    }

    // Centralized place to apply employee-generated dishes.
    // Keeps ScoreManager authoritative over completed dishes.
    private void AddDishesFromEmployees(long amount)
    {
        if (amount <= 0) return;

        // totalDishes: use your real field type (int/long/BigInteger, etc.). long is typical.
        totalDishes += amount;

        // If other systems listen for dishes, invoke them here.
        // e.g., OnDishesChanged?.Invoke(totalDishes);

        // Update UI immediately on significant change
        UpdateUI();
    }

    public void OnModifierCountClicked() => TryUpgradeDishCount();
    public void OnModifierProfitClicked() => TryUpgradeProfit();

    // --- Getters ---
    public long GetTotalDishes() => totalDishes;
    public float GetTotalProfit() => totalProfit;
    public float GetProfitPerDish() => profitPerDish;
    public int GetDishCountIncrement() => dishCountIncrement;

    private void UpdateUI()
    {
        if (dishCountText != null)
        {
            // totalDishes is whatever you already track (int/long). Cast as needed.
            dishCountText.text = $"Dishes: {BigNumberFormatter.FormatNumber(totalDishes)}";
        }

        if (profitText != null)
        {
            // totalProfit is whatever you already track (float/double/decimal). Cast to double for formatter.
            profitText.text = $"Profit: {BigNumberFormatter.FormatMoney((double)totalProfit)}";
        }
    }
    public void LoadFromSave(long dishes, float profit, int countInc, float dishProfitMult)
    {
        totalDishes = dishes;
        totalProfit = profit;
        dishCountIncrement = countInc;
        dishProfitMultiplier = dishProfitMult;

        PendingProfitAdjustment = 0f;
        PendingRewardAdjustment = 0f;

        NotifyProfitChanged();
        UpdateUI();
    }
    // --- TEST/DEV ONLY: add dishes directly (free) ---
    public void AddDishes_ForTesting(long amount)
    {
        if (amount <= 0) return;
        // Keep ScoreManager authoritative over dishes
        // and keep UI consistent.
        var newTotal = GetTotalDishes() + amount;

        // private field is 'totalDishes' – set via backing logic
        // since it's private, we reuse the UI method after changing.
        // (We’re inside the class, so we can set directly.)
        totalDishes = newTotal;
        UpdateUI();
    }
}
