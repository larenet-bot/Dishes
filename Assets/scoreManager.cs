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

    [Header("Employees")]
    public List<Employee> employees = new List<Employee>();

    public float employeeProfitInterval = 1f; // seconds
    private float employeeProfitTimer = 0f;

    [Header("Intern Employee UI")]
    public TMP_Text internNameText;
    public TMP_Text internCostText;
    public TMP_Text internCountText;
    public Button internBuyButton;

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

        // Add employee types first
        employees.Add(new Employee("Intern", 50f, 2f));
        employees.Add(new Employee("Elephant", 200f, 10f));
        employees.Add(new Employee("Firefighter", 1000f, 50f));

        if (internBuyButton != null)
            internBuyButton.onClick.AddListener(() => BuyEmployee(0)); // 0 = Intern index

        // Now update the UI
        UpdateUI();
    }

    void Update()
    {
        // Handle employee profit generation
        employeeProfitTimer += Time.deltaTime;
        if (employeeProfitTimer >= employeeProfitInterval)
        {
            employeeProfitTimer = 0f;
            AddEmployeeProfits();
        }
    }

    // Event for profit changes
    public delegate void ProfitChanged();
    public static event ProfitChanged OnProfitChanged;

    private void NotifyProfitChanged()
    {
        OnProfitChanged?.Invoke();
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
    }

    // Add profit from bubbles or other sources
    public void AddBubbleReward(float reward)
    {
        totalProfit += reward;
        PendingRewardAdjustment += reward;
        UpdateUI();
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

        // Update Intern employee UI
        var intern = employees[0]; // Assuming Intern is first
        if (internNameText != null)
            internNameText.text = intern.name;
        if (internCostText != null)
            internCostText.text = $"Cost: ${intern.cost:0.00}";
        if (internCountText != null)
            internCountText.text = $"Owned: {intern.count}";
        if (internBuyButton != null)
            internBuyButton.interactable = totalProfit >= intern.cost;
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

    [System.Serializable]
    public class Employee
    {
        public string name;
        public float cost;
        public float profitPerInterval;
        public int count;

        public Employee(string name, float cost, float profitPerInterval)
        {
            this.name = name;
            this.cost = cost;
            this.profitPerInterval = profitPerInterval;
            this.count = 0;
        }

        public float GetTotalProfitPerSecond()
        {
            return profitPerInterval * count;
        }
    }

    public void BuyEmployee(int employeeIndex)
    {
        if (employeeIndex < 0 || employeeIndex >= employees.Count)
            return;

        Employee emp = employees[employeeIndex];
        if (totalProfit >= emp.cost)
        {
            SubtractProfit(emp.cost, true); // This already adds to PendingProfitAdjustment
            emp.count++;
            emp.cost *= 1.15f; // Increase cost for next purchase
            UpdateUI();
            Debug.Log($"Bought {emp.name}, now have {emp.count}");
        }
        else
        {
            Debug.Log($"Not enough profit to buy {emp.name} (need ${emp.cost:0.00})");
        }
    }

    private void AddEmployeeProfits()
    {
        float totalEmployeeProfit = 0f;
        foreach (var emp in employees)
        {
            float empProfit = emp.GetTotalProfitPerSecond();
            //Debug.Log($"[ScoreManager] Employee: {emp.name}, Count: {emp.count}, ProfitPerInterval: {emp.profitPerInterval}, ProfitThisInterval: {empProfit}");
            totalEmployeeProfit += empProfit;
        }

        if (totalEmployeeProfit > 0f)
        {
            //Debug.Log($"[ScoreManager] Adding total employee profit: {totalEmployeeProfit} to totalProfit: {totalProfit}");
            totalProfit += totalEmployeeProfit;
            UpdateUI();
        }
    }
}
