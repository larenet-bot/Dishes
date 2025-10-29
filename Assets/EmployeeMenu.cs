using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.EventSystems;

public class EmployeeManager : MonoBehaviour
{
    [Header("Reference to ScoreManager")]
    [SerializeField] private ScoreManager scoreManager;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Employees")]
    [SerializeField] private List<Employee> employees = new List<Employee>();

    [Header("Profit Tick")]
    [SerializeField] private float employeeProfitInterval = 1f;
    private float employeeProfitTimer = 0f;

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

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        if (employees.Count > 0 && employees[0].outputMixerGroup != null)
            sfxSource.outputAudioMixerGroup = employees[0].outputMixerGroup;

        // Seed defaults if list is empty
        if (employees.Count == 0)
        {
            employees.Add(new Employee("Intern", 50f, 2f));
            employees.Add(new Employee("Elephant", 200f, 10f));
            employees.Add(new Employee("Firetruck", 1000f, 50f));
        }

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
        // detect outside click to close the employee panel
        if (employeesPanel != null && employeesPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            // ignore clicks that hit UI elements *inside* the panel
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
            {
                CloseEmployeesPanel();
            }
        }
    }

    private void HandleProfitChanged()
    {
        UpdateEmployeeUI();
    }

    // -------------------- Panel Controls --------------------
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

    private bool IsClickOutsidePanel(Vector2 clickPos)
    {
        if (employeesPanel == null) return false;
        if (!employeesPanel.activeSelf) return false;

        RectTransform panelRect = employeesPanel.GetComponent<RectTransform>();
        if (panelRect == null) return false;

        return !RectTransformUtility.RectangleContainsScreenPoint(panelRect, clickPos, null);
    }

    // -------------------- Buying --------------------
    public void BuyEmployee(int index)
    {
        if (index < 0 || index >= employees.Count || scoreManager == null) return;

        var emp = employees[index];
        float wallet = scoreManager.GetTotalProfit();

        if (wallet >= emp.cost)
        {
            scoreManager.SubtractProfit(emp.cost, isPurchase: true);
            emp.count++;
            emp.cost *= 1.15f;
            UpdateEmployeeUI();
            Debug.Log($"[EmployeeManager] Bought {emp.name}, now have {emp.count}");
        }
        else
        {
            Debug.Log($"[EmployeeManager] Not enough profit to buy {emp.name} (need ${emp.cost:0.00})");
        }

        if (emp.buySounds != null && emp.buySounds.Length > 0)
        {
            var clip = emp.buySounds[Random.Range(0, emp.buySounds.Length)];
            sfxSource.outputAudioMixerGroup = emp.outputMixerGroup;
            sfxSource.PlayOneShot(clip);
        }
    }

    private void AddEmployeeProfits()
    {
        if (scoreManager == null) return;

        float total = 0f;
        foreach (var e in employees)
            total += e.GetTotalProfitPerSecond();

        if (total > 0f)
            scoreManager.AddProfit(total);
    }

    // -------------------- UI --------------------
    private void UpdateEmployeeUI()
    {
        float wallet = scoreManager ? scoreManager.GetTotalProfit() : 0f;

        // Intern
        if (employees.Count > 0)
        {
            var e = employees[0];
            if (internNameText) internNameText.text = e.name;
            if (internCostText) internCostText.text = $"Cost: {BigNumberFormatter.FormatMoney((double)e.cost)}";
            if (internCountText) internCountText.text = $"Owned: {BigNumberFormatter.FormatNumber(e.count)}";
            if (internBuyButton) internBuyButton.interactable = wallet >= e.cost;
            SetIconForAffordability(e, wallet);
        }

        // Elephant
        if (employees.Count > 1)
        {
            var e = employees[1];
            if (elephantNameText) elephantNameText.text = e.name;
            if (elephantCostText) elephantCostText.text = $"Cost: {BigNumberFormatter.FormatMoney((double)e.cost)}";
            if (elephantCountText) elephantCountText.text = $"Owned: {BigNumberFormatter.FormatNumber(e.count)}";
            if (elephantBuyButton) elephantBuyButton.interactable = wallet >= e.cost;
            SetIconForAffordability(e, wallet);
        }

        // Firetruck
        if (employees.Count > 2)
        {
            var e = employees[2];
            if (firetruckNameText) firetruckNameText.text = e.name;
            if (firetruckCostText) firetruckCostText.text = $"Cost: {BigNumberFormatter.FormatMoney((double)e.cost)}";
            if (firetruckCountText) firetruckCountText.text = $"Owned: {BigNumberFormatter.FormatNumber(e.count)}";
            if (firetruckBuyButton) firetruckBuyButton.interactable = wallet >= e.cost;
            SetIconForAffordability(e, wallet);
        }
    }

    private void SetIconForAffordability(Employee emp, float wallet)
    {
        if (emp.employeeImageObject == null || emp.employeeImageGreyObject == null)
            return;

        bool canAfford = wallet >= emp.cost;

        // Toggle GameObjects instead of swapping Sprites
        emp.employeeImageObject.SetActive(canAfford);
        emp.employeeImageGreyObject.SetActive(!canAfford);
    }

    // Multiply profit rates (for upgrades, etc.)
    public void MultiplyEmployeeProfit(float multiplier)
    {
        if (multiplier <= 0f) return;
        foreach (var e in employees)
        {
            e.profitPerInterval *= multiplier;
        }
        UpdateEmployeeUI();
    }

    // -------------------- Data Classes --------------------
    [System.Serializable]
    public class Employee
    {
        public string name;
        public float cost;
        public float profitPerInterval;
        public int count;

        [Header("Icon Objects (child GameObjects)")]
        public GameObject employeeImageObject;      // Visible when affordable
        public GameObject employeeImageGreyObject;  // Visible when unaffordable

        [Header("Audio Settings")]
        public AudioClip[] buySounds;
        public AudioClip[] workSounds;
        public AudioMixerGroup outputMixerGroup;

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
