using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

    [Header("Bubble Buffs")]
    [SerializeField] private Image happyEmployeesTimerFill;
    [SerializeField] private GameObject happyEmployeesTimerRoot;
    [SerializeField] private TMP_Text happyEmployeesTitleText;
    [SerializeField] private TMP_Text happyEmployeesInfoText;

    [SerializeField] private Image wellMotivatedTimerFill;
    [SerializeField] private TMP_Text wellMotivatedTitleText;
    [SerializeField] private TMP_Text wellMotivatedInfoText;
    [SerializeField] private GameObject wellMotivatedTimerRoot;

    private float happyEmployeesMultiplier = 1f;
    private float happyEmployeesEndTime = -1f;
    private float happyEmployeesDuration = 0f;

    private float wellMotivatedMultiplier = 1f;
    private float wellMotivatedEndTime = -1f;
    private float wellMotivatedDuration = 0f;

    private void Awake()
    {
        Application.runInBackground = true;
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

        SetBuffUIActive(happyEmployeesTimerRoot, happyEmployeesTimerFill, false);
        SetBuffUIActive(wellMotivatedTimerRoot, wellMotivatedTimerFill, false);

        if (happyEmployeesTitleText != null) happyEmployeesTitleText.text = string.Empty;
        if (happyEmployeesInfoText != null) happyEmployeesInfoText.text = string.Empty;
        if (wellMotivatedTitleText != null) wellMotivatedTitleText.text = string.Empty;
        if (wellMotivatedInfoText != null) wellMotivatedInfoText.text = string.Empty;

        UpdateUI();
    }

    private void Update()
    {
        UpdateBuffs();
    }

    public void NotifyProfitChanged() => OnProfitChanged?.Invoke();

    public bool IsHappyEmployeesActive => Time.time < happyEmployeesEndTime;
    public bool IsWellMotivatedActive => Time.time < wellMotivatedEndTime;

    public float EmployeeSpeedMultiplier => IsHappyEmployeesActive ? happyEmployeesMultiplier : 1f;
    public float WellMotivatedMultiplier => IsWellMotivatedActive ? wellMotivatedMultiplier : 1f;

    public float GetEffectiveDishProfitMultiplier()
    {
        return dishProfitMultiplier * WellMotivatedMultiplier;
    }

    // Called when a dish is clicked enough times to complete it
    // Called when a dish is clicked enough times to complete it
    // Called when a dish is completed.
    // Overload supports sinks (ex: wash basin) that change how many dishes are awarded per completion.
    public float OnDishCleaned(DishData finishedDish)
    {
        return OnDishCleaned(finishedDish, dishCountIncrement);
    }

    public float OnDishCleaned(DishData finishedDish, long dishesCompleted)
    {
        if (finishedDish == null) return 0f;
        if (dishesCompleted <= 0) return 0f;

        totalDishes += dishesCompleted;

        float reward = (float)dishesCompleted * finishedDish.profitPerDish * GetEffectiveDishProfitMultiplier();
        totalProfit += reward;

        NotifyProfitChanged();
        UpdateUI();

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
            double dps = employeeManager.GetTotalDishesPerSecond() * EmployeeSpeedMultiplier;

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

    // ScoreManager.cs
    public void AwardBackgroundWash(DishData referenceDish, long dishesCompleted, float cashMultiplier = 1f)
    {
        if (referenceDish == null || dishesCompleted <= 0) return;

        totalDishes += dishesCompleted;

        float reward = dishesCompleted * referenceDish.profitPerDish * GetEffectiveDishProfitMultiplier() * cashMultiplier;
        totalProfit += reward;

        NotifyProfitChanged();
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

    // ---------------- Bubble buffs API ----------------

    // Backwards-compatible overloads (in case other scripts call these without a title).
    public void ApplyHappyEmployeesBoost(float multiplier, float seconds)
    {
        ApplyHappyEmployeesBoost("Happy Employees", multiplier, seconds);
    }

    public void ApplyWellMotivatedBoost(float multiplier, float seconds)
    {
        ApplyWellMotivatedBoost("Well Motivated", multiplier, seconds);
    }

    public void ApplyHappyEmployeesBoost(string title, float multiplier, float seconds)
    {
        multiplier = Mathf.Max(1f, multiplier);
        seconds = Mathf.Max(0.1f, seconds);

        happyEmployeesMultiplier = Mathf.Max(happyEmployeesMultiplier, multiplier);
        happyEmployeesDuration = seconds;
        happyEmployeesEndTime = Time.time + seconds;

        if (happyEmployeesTitleText != null)
            happyEmployeesTitleText.text = string.IsNullOrWhiteSpace(title) ? "Happy Employees" : title;

        if (happyEmployeesInfoText != null)
            happyEmployeesInfoText.text = $"x{happyEmployeesMultiplier:0.#} employee speed";

        SetBuffUIActive(happyEmployeesTimerRoot, happyEmployeesTimerFill, true);
        NotifyProfitChanged();
    }

    public void ApplyWellMotivatedBoost(string title, float multiplier, float seconds)
    {
        multiplier = Mathf.Max(1f, multiplier);
        seconds = Mathf.Max(0.1f, seconds);

        wellMotivatedMultiplier = Mathf.Max(wellMotivatedMultiplier, multiplier);
        wellMotivatedDuration = seconds;
        wellMotivatedEndTime = Time.time + seconds;

        if (wellMotivatedTitleText != null)
            wellMotivatedTitleText.text = string.IsNullOrWhiteSpace(title) ? "Well Motivated" : title;

        if (wellMotivatedInfoText != null)
        {
            // Example: "x20 per dish"
            wellMotivatedInfoText.text = $"x{wellMotivatedMultiplier:0.#} per dish";
        }

        SetBuffUIActive(wellMotivatedTimerRoot, wellMotivatedTimerFill, true);
        NotifyProfitChanged();
    }

    private void UpdateBuffs()
    {
        // Happy Employees UI
        if (IsHappyEmployeesActive)
        {
            UpdateTimerFill(happyEmployeesTimerFill, happyEmployeesEndTime, happyEmployeesDuration);
            SetBuffUIActive(happyEmployeesTimerRoot, happyEmployeesTimerFill, true);
        }
        else
        {
            if (happyEmployeesEndTime > 0f)
            {
                happyEmployeesEndTime = -1f;
                happyEmployeesDuration = 0f;
                happyEmployeesMultiplier = 1f;
                if (happyEmployeesTitleText != null) happyEmployeesTitleText.text = string.Empty;
                if (happyEmployeesInfoText != null) happyEmployeesInfoText.text = string.Empty;
                NotifyProfitChanged();
            }
            SetBuffUIActive(happyEmployeesTimerRoot, happyEmployeesTimerFill, false);
        }

        // Well Motivated UI
        if (IsWellMotivatedActive)
        {
            UpdateTimerFill(wellMotivatedTimerFill, wellMotivatedEndTime, wellMotivatedDuration);
            SetBuffUIActive(wellMotivatedTimerRoot, wellMotivatedTimerFill, true);
        }
        else
        {
            if (wellMotivatedEndTime > 0f)
            {
                wellMotivatedEndTime = -1f;
                wellMotivatedDuration = 0f;
                wellMotivatedMultiplier = 1f;
                if (wellMotivatedTitleText != null) wellMotivatedTitleText.text = string.Empty;
                if (wellMotivatedInfoText != null) wellMotivatedInfoText.text = string.Empty;
                NotifyProfitChanged();
            }
            SetBuffUIActive(wellMotivatedTimerRoot, wellMotivatedTimerFill, false);
        }
    }

    private void UpdateTimerFill(Image img, float endTime, float duration)
    {
        if (img == null) return;
        if (duration <= 0.0001f)
        {
            img.fillAmount = 0f;
            return;
        }

        float remaining = Mathf.Max(0f, endTime - Time.time);
        float t = Mathf.Clamp01(remaining / duration);
        img.fillAmount = t;
    }

    private void SetBuffUIActive(GameObject root, Image fallbackImage, bool active)
    {
        if (root != null)
        {
            if (root.activeSelf != active)
                root.SetActive(active);
            return;
        }

        if (fallbackImage != null)
        {
            if (fallbackImage.gameObject.activeSelf != active)
                fallbackImage.gameObject.SetActive(active);
        }
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
