using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DevConsole : MonoBehaviour
{
    [Header("Toggle")]
    [Tooltip("Key to toggle console on/off")]
    public KeyCode toggleKey = KeyCode.BackQuote;

    [Header("UI")]
    public Canvas rootCanvas;              // optional: if null, a minimal canvas is created at runtime
    public GameObject panel;               // panel with background
    public TMP_InputField inputField;      // where you type commands
    public TMP_Text logText;               // output log
    public ScrollRect scrollRect;          // for log scrolling

    [Header("Refs (auto-wired if null)")]
    public ScoreManager score;
    public EmployeeManager employees;
    public Upgrades upgrades;

    [Header("Settings")]
    public bool enableConsole = true;
    public int maxLogChars = 8000;

    private readonly StringBuilder _log = new StringBuilder(512);
    private bool _open;

    void Awake()
    {
        if (score == null) score = FindFirstObjectByType<ScoreManager>();
        if (employees == null) employees = FindFirstObjectByType<EmployeeManager>();
        if (upgrades == null) upgrades = FindFirstObjectByType<Upgrades>();

        if (rootCanvas == null || panel == null || inputField == null || logText == null)
            BuildMinimalUI();

        SetOpen(false);
        PrintLine("DevConsole ready. Press ` to open. Type 'help' for commands.");

        if (inputField != null)
        {
            inputField.lineType = TMPro.TMP_InputField.LineType.SingleLine;

            // Clear any old listeners, then hook both submit paths.
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener(SubmitCommand);

            inputField.onEndEdit.RemoveAllListeners();
            inputField.onEndEdit.AddListener(s =>
            {
                // Some TMP versions only fire onEndEdit; capture Enter manually.
                if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))
                    SubmitCommand(s);
            });
        }
    }

    void Update()
    {
        if (!enableConsole) return;
        if (Input.GetKeyDown(toggleKey)) SetOpen(!_open);

        // Submit on Enter when focused
        if (_open && inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
        {
            var line = inputField.text.Trim();
            inputField.text = string.Empty;
            if (!string.IsNullOrEmpty(line)) HandleCommand(line);
            inputField.ActivateInputField();
            SubmitCommand(inputField.text);
        }
    }

    // -------------- Commands --------------

    void HandleCommand(string raw)
    {
        PrintLine($"> {raw}");

        // tokenizing: verb [args...]
        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var verb = parts[0].ToLowerInvariant();

        try
        {
            switch (verb)
            {
                case "help": CmdHelp(); break;

                // profit add/sub/set
                case "profit": CmdProfit(parts); break;

                // dishes add/set
                case "dishes": CmdDishes(parts); break;

                // employee verbs: list, add, buy
                case "employee":
                case "emp": CmdEmployee(parts); break;

                // upgrades: set/jump to soap|glove|sponge idx, free or spend
                case "upgrade":
                case "up": CmdUpgrade(parts); break;

                // rate peek
                case "rate": CmdRate(); break;

                // clear console
                case "clear": _log.Clear(); RefreshLog(); break;

                default:
                    PrintLine("Unknown command. Type 'help'.");
                    break;
            }
        }
        catch (Exception ex)
        {
            PrintLine($"ERR: {ex.Message}");
        }
    }

    void CmdHelp()
    {
        PrintLine("Commands:");
        PrintLine("  help");
        PrintLine("  clear");
        PrintLine("  rate                      -> shows average profit/sec (if active)");
        PrintLine("  profit add <amount>       -> add profit (free)");
        PrintLine("  profit sub <amount>       -> subtract profit (purchase-aware)");
        PrintLine("  profit set <amount>       -> set total profit exactly");
        PrintLine("  dishes add <amount>       -> add dishes (free)");
        PrintLine("  dishes set <amount>       -> set total dishes exactly");
        PrintLine("  emp list                  -> list employee types & counts");
        PrintLine("  emp add <idx|name> <n>    -> add employees (free)");
        PrintLine("  emp buy <idx|name> <n>    -> buy employees (spend money)");
        PrintLine("  up set <soap|glove|sponge> <idx> [spend]");
        PrintLine("                            -> jump to an intermediate tier; add 'spend' to pay costs");
    }

    // profit add/sub/set
    void CmdProfit(string[] p)
    {
        Require(score != null, "ScoreManager not found.");
        if (p.Length < 3) { PrintLine("Usage: profit add|sub|set <amount>"); return; }

        var mode = p[1].ToLowerInvariant();
        var amt = ParseFloat(p[2]);

        switch (mode)
        {
            case "add":
                score.AddProfit(amt);
                PrintLine($"Profit = {BigNumberFormatter.FormatMoney(score.GetTotalProfit())}");
                break;

            case "sub":
                score.SubtractProfit(amt, isPurchase: true);   // counts as a purchase (keeps Avg calc correct)
                PrintLine($"Profit = {BigNumberFormatter.FormatMoney(score.GetTotalProfit())}");
                break;

            case "set":
                // set = (current -> target)
                var delta = amt - score.GetTotalProfit();
                if (Mathf.Approximately(delta, 0f)) { PrintLine("No change."); return; }
                if (delta > 0) score.AddProfit(delta);
                else score.SubtractProfit(-delta, isPurchase: false); // not a purchase; just force down
                PrintLine($"Profit = {BigNumberFormatter.FormatMoney(score.GetTotalProfit())}");
                break;

            default:
                PrintLine("Usage: profit add|sub|set <amount>");
                break;
        }
    }

    // dishes add/set (free)
    void CmdDishes(string[] p)
    {
        Require(score != null, "ScoreManager not found.");
        if (p.Length < 3) { PrintLine("Usage: dishes add|set <amount>"); return; }

        var mode = p[1].ToLowerInvariant();
        var amt = ParseLong(p[2]);

        switch (mode)
        {
            case "add":
                score.AddDishes_ForTesting(amt);
                PrintLine($"Dishes = {BigNumberFormatter.FormatNumber(score.GetTotalDishes())}");
                break;

            case "set":
                var newTotal = Mathf.Max(0, (int)(amt)); // clamp non-negative
                var diff = (long)newTotal - score.GetTotalDishes();
                if (diff > 0) score.AddDishes_ForTesting(diff);
                else if (diff < 0) { score.AddDishes_ForTesting(diff); } // supports reducing as well
                PrintLine($"Dishes = {BigNumberFormatter.FormatNumber(score.GetTotalDishes())}");
                break;

            default:
                PrintLine("Usage: dishes add|set <amount>");
                break;
        }
    }

    // emp list/add/buy
    void CmdEmployee(string[] p)
    {
        Require(employees != null, "EmployeeManager not found.");
        if (p.Length < 2) { PrintLine("Usage: emp list | emp add <idx|name> <n> | emp buy <idx|name> <n>"); return; }

        var sub = p[1].ToLowerInvariant();

        if (sub == "list")
        {
            int types = employees.GetEmployeeTypeCount();
            for (int i = 0; i < types; i++)
            {
                var count = employees.GetEmployeeCountForIndex(i);
                PrintLine($"[{i}] count={count}");
            }
            return;
        }

        if (p.Length < 4) { PrintLine("Usage: emp add|buy <idx|name> <n>"); return; }

        int index = ResolveEmployeeIndex(p[2]);
        int n = Mathf.Max(1, (int)ParseLong(p[3]));

        if (index < 0) { PrintLine("Unknown employee."); return; }

        if (sub == "add")
        {
            employees.AddEmployees_Free(index, n);
            PrintLine($"Gave +{n} to employee[{index}].");
        }
        else if (sub == "buy")
        {
            for (int i = 0; i < n; i++) employees.BuyEmployee(index);
            PrintLine($"Bought {n}x employee[{index}].");
        }
        else
        {
            PrintLine("Usage: emp add|buy <idx|name> <n>");
        }
    }

    // up set soap|glove|sponge idx [spend]
    void CmdUpgrade(string[] p)
    {
        Require(upgrades != null, "Upgrades not found.");
        if (p.Length < 4) { PrintLine("Usage: up set <soap|glove|sponge> <index> [spend]"); return; }
        if (p[1].ToLowerInvariant() != "set") { PrintLine("Usage: up set <soap|glove|sponge> <index> [spend]"); return; }

        string kind = p[2].ToLowerInvariant();
        int idx = Mathf.Max(0, (int)ParseLong(p[3]));
        bool spend = (p.Length >= 5 && p[4].Equals("spend", StringComparison.OrdinalIgnoreCase));

        switch (kind)
        {
            case "soap": upgrades.SetSoapTierIndex(idx, spend); PrintLine($"Set SOAP -> {idx} {(spend ? "(spent)" : "(free)")}"); break;
            case "glove": upgrades.SetGloveTierIndex(idx, spend); PrintLine($"Set GLOVE -> {idx} {(spend ? "(spent)" : "(free)")}"); break;
            case "sponge": upgrades.SetSpongeTierIndex(idx, spend); PrintLine($"Set SPONGE -> {idx} {(spend ? "(spent)" : "(free)")}"); break;
            default: PrintLine("Kind must be soap|glove|sponge."); break;
        }
    }

    void CmdRate()
    {
        if (ProfitRate.Instance == null) { PrintLine("ProfitRate not active."); return; }
        var a = ProfitRate.Instance.AverageProfit;
        PrintLine($"Avg Profit/sec = {BigNumberFormatter.FormatMoney(a)}");
    }

    // -------------- helpers --------------
    int ResolveEmployeeIndex(string token)
    {
        // You can extend this to map names -> indices if you expose a name getter.
        // For now: try int index, else unknown (-1).
        if (int.TryParse(token, out int i)) return i;
        return -1;
    }

    float ParseFloat(string s)
    {
        if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) return v;
        throw new Exception("Bad number.");
    }
    long ParseLong(string s)
    {
        if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v)) return v;
        throw new Exception("Bad integer.");
    }

    void Require(bool cond, string msg)
    {
        if (!cond) throw new Exception(msg);
    }

    void PrintLine(string line)
    {
        _log.AppendLine(line);
        if (_log.Length > maxLogChars)
        {
            // trim from the start
            _log.Remove(0, _log.Length - maxLogChars);
        }
        RefreshLog();
    }

    void RefreshLog()
    {
        if (logText != null) logText.text = _log.ToString();
        if (scrollRect != null) Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f; // stick to bottom
    }

    void SetOpen(bool open)
    {
        _open = open;
        if (panel != null) panel.SetActive(open);
        if (open && inputField != null) inputField.ActivateInputField();
        Time.timeScale = open ? 0f : 1f; // pause while typing (optional)
    }

    private void SubmitCommand(string line)
    {
        string trimmed = (line ?? string.Empty).Trim();

        // Clear and re-focus the field immediately
        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.caretPosition = 0;
            inputField.selectionAnchorPosition = 0;
            inputField.selectionFocusPosition = 0;
            inputField.ActivateInputField(); // keep keyboard focus
        }

        if (trimmed.Length == 0) return; // nothing to do
        HandleCommand(trimmed);
    }

    // Build a barebones overlay if you didn’t wire UI
    void BuildMinimalUI()
    {
        var goCanvas = new GameObject("DevConsoleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        rootCanvas = goCanvas.GetComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        goCanvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Panel
        var goPanel = new GameObject("Panel", typeof(Image));
        goPanel.transform.SetParent(goCanvas.transform, false);
        panel = goPanel;
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.55f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        // Log (TMP)
        var logGO = new GameObject("LogText", typeof(TextMeshProUGUI));
        logGO.transform.SetParent(panel.transform, false);
        logText = logGO.GetComponent<TextMeshProUGUI>();
        var logRT = logGO.GetComponent<RectTransform>();
        logRT.anchorMin = new Vector2(0.02f, 0.25f);
        logRT.anchorMax = new Vector2(0.98f, 0.95f);
        logRT.offsetMin = logRT.offsetMax = Vector2.zero;
        logText.textWrappingMode = TextWrappingModes.Normal;
        logText.fontSize = 20;

        // Input (TMP)
        var inputGO = new GameObject("InputField", typeof(TMP_InputField), typeof(TextMeshProUGUI), typeof(Image));
        inputGO.transform.SetParent(panel.transform, false);
        inputField = inputGO.GetComponent<TMP_InputField>();
        var inputRT = inputGO.GetComponent<RectTransform>();
        inputRT.anchorMin = new Vector2(0.02f, 0.05f);
        inputRT.anchorMax = new Vector2(0.98f, 0.18f);
        inputRT.offsetMin = inputRT.offsetMax = Vector2.zero;

        var textComp = inputGO.GetComponent<TextMeshProUGUI>();
        textComp.textWrappingMode = TextWrappingModes.NoWrap;
        textComp.fontSize = 22;
        inputField.textComponent = textComp;
        inputGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.15f);
    }
}
