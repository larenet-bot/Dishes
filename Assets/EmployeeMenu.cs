using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EmployeeManager : MonoBehaviour
{
    [Header("Reference to ScoreManager")]
    [SerializeField] private ScoreManager scoreManager;

    [Header("Employees")]
    [SerializeField] private List<Employee> employees = new List<Employee>();

    [Header("Profit Tick")]
    [SerializeField] private float employeeProfitInterval = 1f;
    private float employeeProfitTimer = 0f;

    // --- Hardcoded UI (kept as-is for now; easy to replace with a prefab list later) ---
    [Header("Intern Employee UI")]
    [SerializeField] private TMP_Text internNameText;
    [SerializeField] private TMP_Text internCostText;
    [SerializeField] private TMP_Text internCountText;
    [SerializeField] private Button internBuyButton;

    [Header("Elephant Employee UI")]
    [SerializeField] private TMP_Text elephantNameText;
    [SerializeField] private TMP_Text elephantCostText;
    [SerializeField] private TMP_Text elephantCountText;
    [SerializeField] private Button elephantBuyButton;

    [Header("Firetruck Employee UI")]
    [SerializeField] private TMP_Text firetruckNameText;
    [SerializeField] private TMP_Text firetruckCostText;
    [SerializeField] private TMP_Text firetruckCountText;
    [SerializeField] private Button firetruckBuyButton;

    [Header("Employees Menu")]
    [SerializeField] private GameObject employeesPanel;
    [SerializeField] private GameObject employeesMenuButton;
    [SerializeField] private GameObject closeButton; // the X

    private void Reset()
    {
        scoreManager = FindFirstObjectByType<ScoreManager>();
    }

    private void Awake()
    {
        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManager>();

        // Seed defaults if list is empty (mirrors your previous setup)
        if (employees.Count == 0)
        {
            employees.Add(new Employee("Intern", 50f, 2f));
            employees.Add(new Employee("Elephant", 200f, 10f));
            employees.Add(new Employee("Firetruck", 1000f, 50f));
        }

        // Button hooks (safe if null)
        if (internBuyButton) internBuyButton.onClick.AddListener(() => BuyEmployee(0));
        if (elephantBuyButton) elephantBuyButton.onClick.AddListener(() => BuyEmployee(1));
        if (firetruckBuyButton) firetruckBuyButton.onClick.AddListener(() => BuyEmployee(2));
    }

    private void OnEnable()
    {
        ScoreManager.OnProfitChanged += HandleProfitChanged;
        InitMenuVisibility();
        UpdateEmployeeUI();
    }

    private void OnDisable()
    {
        ScoreManager.OnProfitChanged -= HandleProfitChanged;
    }

    private void Update()
    {
        employeeProfitTimer += Time.deltaTime;
        if (employeeProfitTimer >= employeeProfitInterval)
        {
            employeeProfitTimer = 0f;
            AddEmployeeProfits();
        }
    }

    private void HandleProfitChanged()
    {
        // Profit changed elsewhere ? update button interactability & labels
        UpdateEmployeeUI();
    }

    // --- Public menu controls (wire from Unity buttons) ---
    public void OpenEmployeesPanel()
    {
        if (employeesPanel) employeesPanel.SetActive(true);
        if (employeesMenuButton) employeesMenuButton.SetActive(false);
        if (closeButton) closeButton.SetActive(true);
    }

    public void CloseEmployeesPanel()
    {
        if (employeesPanel) employeesPanel.SetActive(false);
        if (employeesMenuButton) employeesMenuButton.SetActive(true);
        if (closeButton) closeButton.SetActive(false);
    }

    public void ToggleEmployeesPanel()
    {
        if (!employeesPanel) return;
        bool newState = !employeesPanel.activeSelf;
        employeesPanel.SetActive(newState);
        if (employeesMenuButton) employeesMenuButton.SetActive(!newState);
        if (closeButton) closeButton.SetActive(newState);
    }

    private void InitMenuVisibility()
    {
        if (!employeesPanel || !employeesMenuButton || !closeButton) return;
        employeesPanel.SetActive(false);
        employeesMenuButton.SetActive(true);
        closeButton.SetActive(false);
    }

    // --- Buying / Earnings ---
    public void BuyEmployee(int index)
    {
        if (index < 0 || index >= employees.Count || scoreManager == null) return;

        var emp = employees[index];
        float wallet = scoreManager.GetTotalProfit();

        if (wallet >= emp.cost)
        {
            scoreManager.SubtractProfit(emp.cost, isPurchase: true);
            emp.count++;
            emp.cost *= 1.15f; // same scaling
            UpdateEmployeeUI();
            Debug.Log($"[EmployeeManager] Bought {emp.name}, now have {emp.count}");
        }
        else
        {
            Debug.Log($"[EmployeeManager] Not enough profit to buy {emp.name} (need ${emp.cost:0.00})");
        }
    }

    private void AddEmployeeProfits()
    {
        if (scoreManager == null) return;

        float total = 0f;
        foreach (var e in employees)
            total += e.GetTotalProfitPerSecond();

        if (total > 0f)
        {
            scoreManager.AddProfit(total); // centralizes UI + event firing in ScoreManager
        }
    }

    // --- UI ---
    private void UpdateEmployeeUI()
    {
        // Guard if ScoreManager not ready (e.g., scene startup order)
        float wallet = scoreManager ? scoreManager.GetTotalProfit() : 0f;

        // Intern
        if (employees.Count > 0)
        {
            var e = employees[0];
            if (internNameText) internNameText.text = e.name;
            if (internCostText) internCostText.text = $"Cost: ${e.cost:0.00}";
            if (internCountText) internCountText.text = $"Owned: {e.count}";
            if (internBuyButton) internBuyButton.interactable = wallet >= e.cost;
        }

        // Elephant
        if (employees.Count > 1)
        {
            var e = employees[1];
            if (elephantNameText) elephantNameText.text = e.name;
            if (elephantCostText) elephantCostText.text = $"Cost: ${e.cost:0.00}";
            if (elephantCountText) elephantCountText.text = $"Owned: {e.count}";
            if (elephantBuyButton) elephantBuyButton.interactable = wallet >= e.cost;
        }

        // Firetruck
        if (employees.Count > 2)
        {
            var e = employees[2];
            if (firetruckNameText) firetruckNameText.text = e.name;
            if (firetruckCostText) firetruckCostText.text = $"Cost: ${e.cost:0.00}";
            if (firetruckCountText) firetruckCountText.text = $"Owned: {e.count}";
            if (firetruckBuyButton) firetruckBuyButton.interactable = wallet >= e.cost;
        }
    }

    // Public API: multiply all employee profit rates by multiplier (e.g. 1.5 => +50%)
    public void MultiplyEmployeeProfit(float multiplier)
    {
        if (multiplier <= 0f) return;
        foreach (var e in employees)
        {
            e.profitPerInterval *= multiplier;
        }
        UpdateEmployeeUI();
    }

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

        public float GetTotalProfitPerSecond() => profitPerInterval * count;
    }
}