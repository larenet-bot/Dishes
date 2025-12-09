using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EmployeeManager : MonoBehaviour
{
    [Serializable]
    public class EmployeeUpgradeTier
    {
        public string upgradeName;
        [TextArea] public string description;
        public Sprite icon;
        [Tooltip("Cost of this upgrade tier.")]
        public float cost = 100f;
    }

    [Serializable]
    public class EmployeeDefinition
    {
        [Header("Config")]
        public string employeeName;
        [TextArea] public string description;

        [Tooltip("Starting cost for this employee.")]
        public float baseCost = 50f;

        [Tooltip("Cost multiplier applied every time you buy one.")]
        public float costMultiplier = 1.15f;

        [Tooltip("Dishes this employee completes per second (per one employee).")]
        public float dishesPerSecond = 0.5f;

        [Tooltip("Starting debuff applied to profit. 0.1 = 10% of dish value.")]
        public float startingDebuff = 0.1f;

        [Tooltip("How much the debuff increases per upgrade. Default 0.1 = +10%.")]
        public float debuffPerUpgrade = 0.1f;

        [Header("Audio")]
        public AudioClip[] purchaseSfx;

        [Header("Upgrade Tiers (per-employee debuff upgrades)")]
        public List<EmployeeUpgradeTier> upgrades = new List<EmployeeUpgradeTier>();

        [Header("UI References")]
        public GameObject panelRoot;           // The whole employee panel in the scroll area
        public Button buyButton;               // Buy employee button
        public Button upgradeButton;           // Upgrade button for this employee

        public TMP_Text nameText;
        public TMP_Text costText;
        public TMP_Text countText;
        public TMP_Text dishesPerSecondText;   // Shows total dishes/sec for this employee type
        public TMP_Text profitPerSecondText;   // Shows total profit/sec for this employee type
        public TMP_Text upgradeStatusText;     // Shows cost or "MAX"

        public Image employeeImage;            // Normal employee image
        public Image employeeBlackoutImage;    // Black overlay (active when unaffordable)
        public Image upgradeImage;             // Current upgrade icon

        // Runtime state
        [HideInInspector] public int count;
        [HideInInspector] public float currentCost;
        [HideInInspector] public int currentUpgradeIndex;
        [HideInInspector] public float currentDebuff;

        public void InitializeRuntime()
        {
            count = 0;
            currentCost = baseCost;
            currentUpgradeIndex = 0;
            currentDebuff = startingDebuff;
        }

        public float GetTotalDishesPerSecond()
        {
            return dishesPerSecond * count;
        }
    }

    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private AudioSource sfxSource;

    [Header("Employees")]
    [SerializeField] private List<EmployeeDefinition> employees = new List<EmployeeDefinition>();

    [Header("Employee Profit Tick")]
    [Tooltip("How often employee profit is added (seconds).")]
    [SerializeField] private float employeeProfitInterval = 1f;
    private float employeeProfitTimer = 0f;

    [Header("UI: Employees Menu")]
    [SerializeField] private GameObject employeesPanel;      // Scrollable panel
    [SerializeField] private GameObject helpWantedButton;   // Help Wanted button
    [SerializeField] private GameObject closeButton;        // X close button

   // [Header("UI: Description Text")]
    //[SerializeField] private TMP_Text descriptionText;      // Shared description display

    [Header("Profit Multipliers")]
    [Tooltip("Extra global multiplier applied only to employee profit (soap upgrades call this).")]
    [SerializeField] private float globalEmployeeProfitMultiplier = 1f;

    [Header("UI: Tooltip")]
    [SerializeField] private Canvas canvas;           // parent canvas
    [SerializeField] private RectTransform tooltipRoot;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(16f, -16f);

    private void Reset()
    {
        scoreManager = FindFirstObjectByType<ScoreManager>();
    }

    private void Awake()
    {
        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManager>();

        // Keep a local sfxSource as a fallback in case AudioManager isn't present.
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        foreach (var emp in employees)
            emp.InitializeRuntime();

        WireButtons();
        InitMenuVisibility();

        if (tooltipRoot != null)
            tooltipRoot.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        ScoreManager.OnProfitChanged += HandleProfitChanged;
        RefreshAllEmployeeUI();
    }

    private void OnDisable()
    {
        ScoreManager.OnProfitChanged -= HandleProfitChanged;
    }

    private void Update()
    {
        // Employee profit tick
        employeeProfitTimer += Time.deltaTime;
        if (employeeProfitTimer >= employeeProfitInterval)
        {
            employeeProfitTimer = 0f;
            AddEmployeeProfits();
        }

        // Close menu on outside click
        if (employeesPanel != null && employeesPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current == null)
                return;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            bool clickedInsidePanel = false;
            foreach (var r in results)
            {
                if (r.gameObject == null) continue;
                if (r.gameObject.transform.IsChildOf(employeesPanel.transform))
                {
                    clickedInsidePanel = true;
                    break;
                }
            }

            if (!clickedInsidePanel)
                CloseEmployeesPanel();
        }
        // Tooltip follow
        if (tooltipRoot != null && tooltipRoot.gameObject.activeSelf)
        {
            UpdateTooltipPosition();
        }
    }

    private void WireButtons()
    {
        for (int i = 0; i < employees.Count; i++)
        {
            int index = i;
            var emp = employees[i];

            if (emp.buyButton != null)
            {
                emp.buyButton.onClick.RemoveAllListeners();
                emp.buyButton.onClick.AddListener(() => BuyEmployee(index));
            }

            if (emp.upgradeButton != null)
            {
                emp.upgradeButton.onClick.RemoveAllListeners();
                emp.upgradeButton.onClick.AddListener(() => BuyEmployeeUpgrade(index));
            }
        }
    }

    private void HandleProfitChanged()
    {
        RefreshAllEmployeeUI();
    }

    // ---------------- Menu controls ----------------

    public void OpenEmployeesPanel()
    {
        if (employeesPanel) employeesPanel.SetActive(true);
        if (helpWantedButton) helpWantedButton.SetActive(false);
        if (closeButton) closeButton.SetActive(true);
        RefreshAllEmployeeUI();
    }

    public void CloseEmployeesPanel()
    {
        if (employeesPanel) employeesPanel.SetActive(false);
        if (helpWantedButton) helpWantedButton.SetActive(true);
        if (closeButton) closeButton.SetActive(false);
        ClearDescription();
    }

    public void ToggleEmployeesPanel()
    {
        if (employeesPanel == null) return;

        bool newState = !employeesPanel.activeSelf;
        employeesPanel.SetActive(newState);

        if (helpWantedButton) helpWantedButton.SetActive(!newState);
        if (closeButton) closeButton.SetActive(newState);

        if (newState)
            RefreshAllEmployeeUI();
        else
            ClearDescription();
    }

    private void InitMenuVisibility()
    {
        if (!employeesPanel || !helpWantedButton || !closeButton) return;
        employeesPanel.SetActive(false);
        helpWantedButton.SetActive(true);
        closeButton.SetActive(false);
    }

    // ---------------- Buying employees ----------------

    public void BuyEmployee(int index)
    {
        if (scoreManager == null) return;
        if (index < 0 || index >= employees.Count) return;

        var emp = employees[index];
        float wallet = scoreManager.GetTotalProfit();
        float cost = emp.currentCost;

        if (wallet < cost)
            return;

        // Pay
        scoreManager.SubtractProfit(cost, isPurchase: true);

        // Increment owned count
        emp.count++;

        // Scale cost
        emp.currentCost = Mathf.Max(0.01f, emp.currentCost * emp.costMultiplier);

        PlayPurchaseSfx(emp);
        RefreshAllEmployeeUI();
    }

    private void PlayPurchaseSfx(EmployeeDefinition emp)
    {
        if (emp == null || emp.purchaseSfx == null || emp.purchaseSfx.Length == 0)
            return;

        int idx = UnityEngine.Random.Range(0, emp.purchaseSfx.Length);
        var clip = emp.purchaseSfx[idx];
        if (clip == null) return;

        // Prefer centralized AudioManager if available so all SFX go through the same mixer / routing.
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(clip);
            return;
        }

        // Fallback to local AudioSource if AudioManager isn't present in the scene.
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // ---------------- Upgrading employees (debuff tiers) ----------------

    public void BuyEmployeeUpgrade(int index)
    {
        if (scoreManager == null) return;
        if (index < 0 || index >= employees.Count) return;

        var emp = employees[index];
        if (emp.upgrades == null || emp.upgrades.Count == 0) return;

        if (emp.currentUpgradeIndex >= emp.upgrades.Count)
            return; // already max

        var tier = emp.upgrades[emp.currentUpgradeIndex];

        float wallet = scoreManager.GetTotalProfit();
        if (wallet < tier.cost)
            return;

        // Pay
        scoreManager.SubtractProfit(tier.cost, isPurchase: true);

        // Increase debuff for this employee
        emp.currentDebuff += emp.debuffPerUpgrade;

        emp.currentUpgradeIndex++;
        RefreshAllEmployeeUI();
    }

    // ---------------- Profit tick ----------------

    private void AddEmployeeProfits()
    {
        if (scoreManager == null) return;

        // Current profit per dish, including soap multiplier
        float baseDishProfit = scoreManager.GetProfitPerDish();
        float dishMultiplier = scoreManager.dishProfitMultiplier;
        float effectivePerDish = baseDishProfit * dishMultiplier;

        if (effectivePerDish <= 0f)
            return;

        float totalProfitDelta = 0f;

        for (int i = 0; i < employees.Count; i++)
        {
            var emp = employees[i];
            if (emp.count <= 0) continue;

            float dps = emp.GetTotalDishesPerSecond();
            float debuff = Mathf.Max(emp.currentDebuff, 0f);

            float profitPerSecond =
                dps * effectivePerDish * debuff * globalEmployeeProfitMultiplier;

            totalProfitDelta += profitPerSecond * employeeProfitInterval;
        }

        if (totalProfitDelta > 0f)
            scoreManager.AddProfit(totalProfitDelta);
    }

    // ---------------- UI refresh ----------------

    private void RefreshAllEmployeeUI()
    {
        if (scoreManager == null) return;

        float wallet = scoreManager.GetTotalProfit();
        float baseDishProfit = scoreManager.GetProfitPerDish();
        float dishMultiplier = scoreManager.dishProfitMultiplier;
        float effectivePerDish = baseDishProfit * dishMultiplier;

        for (int i = 0; i < employees.Count; i++)
            RefreshEmployeeUI(employees[i], wallet, effectivePerDish);
    }

    private void RefreshEmployeeUI(EmployeeDefinition emp, float wallet, float effectivePerDish)
    {
        if (emp.panelRoot == null)
            return;

        if (emp.nameText != null)
            emp.nameText.text = emp.employeeName;

        if (emp.countText != null)
            emp.countText.text = $"Owned: {BigNumberFormatter.FormatNumber(emp.count)}";

        if (emp.costText != null)
            emp.costText.text = $"Cost: {BigNumberFormatter.FormatMoney(emp.currentCost)}";

        float totalDps = emp.GetTotalDishesPerSecond();
        if (emp.dishesPerSecondText != null)
        {
            emp.dishesPerSecondText.text =
                $"Dishes/sec: {BigNumberFormatter.FormatNumber(totalDps)}";
        }

        float profitPerSecond = 0f;
        if (effectivePerDish > 0f && emp.count > 0)
        {
            float debuff = Mathf.Max(emp.currentDebuff, 0f);
            profitPerSecond = totalDps * effectivePerDish * debuff * globalEmployeeProfitMultiplier;
        }

        if (emp.profitPerSecondText != null)
        {
            emp.profitPerSecondText.text =
                $"{BigNumberFormatter.FormatMoney(profitPerSecond)}/sec";
        }

        bool canAffordEmployee = wallet >= emp.currentCost;

        if (emp.buyButton != null)
            emp.buyButton.interactable = canAffordEmployee;

        // Grey out whole panel on unaffordable
        var cg = emp.panelRoot.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = canAffordEmployee ? 1f : 0.5f;
            cg.interactable = true; // still allow hover
        }

        // Blackout image when unaffordable
        if (emp.employeeBlackoutImage != null)
            emp.employeeBlackoutImage.gameObject.SetActive(!canAffordEmployee);

        // Upgrade UI
        if (emp.upgradeButton != null || emp.upgradeStatusText != null || emp.upgradeImage != null)
        {
            if (emp.upgrades != null && emp.upgrades.Count > 0 && emp.currentUpgradeIndex < emp.upgrades.Count)
            {
                var tier = emp.upgrades[emp.currentUpgradeIndex];

                if (emp.upgradeImage != null && tier.icon != null)
                    emp.upgradeImage.sprite = tier.icon;

                if (emp.upgradeStatusText != null)
                    emp.upgradeStatusText.text = BigNumberFormatter.FormatMoney(tier.cost);

                if (emp.upgradeButton != null)
                    emp.upgradeButton.interactable = wallet >= tier.cost;
            }
            else
            {
                if (emp.upgradeStatusText != null)
                    emp.upgradeStatusText.text = "MAX";

                if (emp.upgradeButton != null)
                    emp.upgradeButton.interactable = false;
            }
        }
    }

    // ---------------- Description hover API ----------------
    // Hook these to EventTrigger.PointerEnter/Exit for each employee panel
    // and upgrade image (pass the employee index).

    public void ShowEmployeeDescription(int index)
    {
        if (tooltipRoot == null || tooltipText == null) return;
        if (index < 0 || index >= employees.Count) return;

        var emp = employees[index];
        tooltipText.text = emp.description;

        tooltipRoot.gameObject.SetActive(true);
        UpdateTooltipPosition();
    }

    public void ShowEmployeeUpgradeDescription(int index)
    {
        if (tooltipRoot == null || tooltipText == null) return;
        if (index < 0 || index >= employees.Count) return;

        var emp = employees[index];
        string text;

        if (emp.upgrades == null || emp.upgrades.Count == 0)
        {
            text = "No upgrades for this employee.";
        }
        else if (emp.currentUpgradeIndex >= emp.upgrades.Count)
        {
            text = $"{emp.employeeName} upgrade maxed.";
        }
        else
        {
            var tier = emp.upgrades[emp.currentUpgradeIndex];
            text = tier.description;
        }

        tooltipText.text = text;
        tooltipRoot.gameObject.SetActive(true);
        UpdateTooltipPosition();
    }

    public void ClearDescription()
    {
        if (tooltipRoot != null)
            tooltipRoot.gameObject.SetActive(false);
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipRoot == null || canvas == null)
            return;

        Vector2 screenPos = Input.mousePosition;
        Vector2 localPos;

        // For Screen Space Overlay or Camera, this handles position conversion
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPos
        );

        tooltipRoot.anchoredPosition = localPos + tooltipOffset;
    }



    // ---------------- Public API for ScoreManager / Upgrades ----------------

    /// <summary>
    /// Sum of dishes per second from all employees.
    /// ScoreManager uses this to feed the employee dish ticker.
    /// </summary>
    public float GetTotalDishesPerSecond()
    {
        float total = 0f;
        for (int i = 0; i < employees.Count; i++)
            total += employees[i].GetTotalDishesPerSecond();
        return total;
    }

    /// <summary>
    /// Called by Upgrades to increase employee profit globally
    /// in addition to dish profit multipliers.
    /// </summary>
    public void MultiplyEmployeeProfit(float multiplier)
    {
        if (multiplier <= 0f) return;
        globalEmployeeProfitMultiplier *= multiplier;
        RefreshAllEmployeeUI();
    }
}
