using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    // --- Dish conversion (profit -> dishes) ---
    [Header("Dish Conversion")]
    [Tooltip("How many dollars of employee profit correspond to 1 dish. Example: $2.00 profit == 1 dish => set to 2.0")]
    [SerializeField] private float profitToDishInterval = 1f;

    // Static, per-employee dish rates (dishes per second per ONE employee).
    // These are computed ONCE from the base profitPerInterval and DO NOT change
    // when upgrades multiply profit rates.
    [SerializeField, ReadOnlyIfPlaying] private float intern_DishInterval = 0f;
    [SerializeField, ReadOnlyIfPlaying] private float elephant_DishInterval = 0f;
    [SerializeField, ReadOnlyIfPlaying] private float firetruck_DishInterval = 0f;

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

        // --- Compute static dish intervals ONCE from the base profitPerInterval ---
        // Guard against invalid divisor
        if (profitToDishInterval <= 0f) profitToDishInterval = 1f;

        // We assume index 0=Intern, 1=Elephant, 2=Firetruck based on your current UI wiring.
        if (employees.Count > 0) intern_DishInterval = employees[0].profitPerInterval / profitToDishInterval;
        if (employees.Count > 1) elephant_DishInterval = employees[1].profitPerInterval / profitToDishInterval;
        if (employees.Count > 2) firetruck_DishInterval = employees[2].profitPerInterval / profitToDishInterval;

        // NOTE: These dish intervals are intentionally NOT touched again by upgrades.
        // Upgrades may multiply e.profitPerInterval for PROFIT, but dish intervals remain fixed.

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
    // === Employee dish-rate accessors =========================================
    // Call from ScoreManager to read dish rates without exposing your internals.
    // Assumes you created these in EmployeeManager as per your last step:
    //   intern_DishInterval, elephant_DishInterval, firetruck_DishInterval
    // and that 'employees' list aligns by index (0,1,2). Adjust if needed.

    public float GetDishIntervalForIndex(int i)
    {
        // Return the precomputed, static dish interval for the given employee index.
        // If you add more employees, extend this switch (or convert to a List<float>).
        switch (i)
        {
            case 0: return intern_DishInterval;
            case 1: return elephant_DishInterval;
            case 2: return firetruck_DishInterval;
            default: return 0f;
        }
    }

    public int GetEmployeeCountForIndex(int i)
    {
        if (i < 0 || i >= employees.Count) return 0;
        return employees[i].count;
    }

    public int GetEmployeeTypeCount()
    {
        return employees != null ? employees.Count : 0;
    }

    /// <summary>
    /// Sum of (dishInterval * count) across all employee types.
    /// This is the *current* dishes per second your crew is producing.
    /// </summary>
    public float GetTotalDishesPerSecond()
    {
        float total = 0f;
        int types = GetEmployeeTypeCount();
        for (int i = 0; i < types; i++)
        {
            float di = GetDishIntervalForIndex(i);   // static dish interval (fixed)
            int ct = GetEmployeeCountForIndex(i);  // live count
            total += di * ct;
        }
        return total;
    }
}

// Makes a serialized field read-only at runtime (play mode).
public class ReadOnlyIfPlayingAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyIfPlayingAttribute))]
public class ReadOnlyIfPlayingDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool prev = GUI.enabled;
        if (Application.isPlaying) GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = prev;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUI.GetPropertyHeight(property, label, true);
}
#endif
