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

    [Header("Employees")]
    public List<Employee> employees = new List<Employee>();

    public float employeeProfitInterval = 1f; // seconds
    private float employeeProfitTimer = 0f;

    [Header("Teenager Employee UI")]
    public TMP_Text teenagerNameText;
    public TMP_Text teenagerCostText;
    public TMP_Text teenagerCountText;
    public Button teenagerBuyButton;

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

        // Example: Add some employee types
        employees.Add(new Employee("Teenager", 50f, 2f));
        employees.Add(new Employee("Elephant", 200f, 10f));
        employees.Add(new Employee("Firefighter", 1000f, 50f));

        if (teenagerBuyButton != null)
            teenagerBuyButton.onClick.AddListener(() => BuyEmployee(0)); // 0 = Teenager index
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

    public void AddBubbleReward(float reward)
    {
        totalProfit += reward;
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

        // Update Teenager employee UI
        var teenager = employees[0]; // Assuming Teenager is first
        if (teenagerNameText != null)
            teenagerNameText.text = teenager.name;
        if (teenagerCostText != null)
            teenagerCostText.text = $"Cost: ${teenager.cost:0.00}";
        if (teenagerCountText != null)
            teenagerCountText.text = $"Owned: {teenager.count}";
        if (teenagerBuyButton != null)
            teenagerBuyButton.interactable = totalProfit >= teenager.cost;
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
            SubtractProfit(emp.cost);
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
            totalEmployeeProfit += emp.GetTotalProfitPerSecond();

        if (totalEmployeeProfit > 0f)
        {
            totalProfit += totalEmployeeProfit;
            UpdateUI();
        }
    }
}
