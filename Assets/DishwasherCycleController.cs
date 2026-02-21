using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runs the Dishwasher rinse cycle when the player is committed to the Dishwasher sink.
/// Awards "background" washes that increase dishes + profit without resetting the current dish visuals.
/// </summary>
public class DishwasherCycleController : MonoBehaviour
{
    [Header("Refs")]
    public SinkManager sinkManager;
    public ScoreManager scoreManager;
    public DishClicker dishClicker;

    [Header("UI")]
    [Tooltip("Optional root to hide/show when dishwasher is active.")]
    public GameObject timerRoot;

    [Tooltip("Fill image (0..1). Set Image Type = Filled.")]
    public Image timerFill;

    [Tooltip("Text like 'Rinse 4:12'.")]
    public TMP_Text timerText;

    [Tooltip("Optional: extra line for technique info.")]
    public TMP_Text statusText;

    [Header("Behavior")]
    public bool runWhilePaused = false;

    private float cycleDuration = 300f;
    private float cycleRemaining = 300f;
    private int autoWashCount = 0;

    private System.Action<SinkManager.SinkType> sinkTypeHandler;
    private System.Action<string> nodePurchasedHandler;
    private System.Action sinkResetHandler;

    private void Reset()
    {
        sinkManager = FindFirstObjectByType<SinkManager>();
        scoreManager = FindFirstObjectByType<ScoreManager>();
        dishClicker = FindFirstObjectByType<DishClicker>();
    }

    private void Awake()
    {
        if (sinkManager == null) sinkManager = FindFirstObjectByType<SinkManager>();
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (dishClicker == null) dishClicker = FindFirstObjectByType<DishClicker>();

        sinkTypeHandler = _ => OnSinkStateChanged();
        nodePurchasedHandler = _ => OnUpgradesChanged();
        sinkResetHandler = () => OnSinkStateChanged();

        if (sinkManager != null)
        {
            sinkManager.OnSinkTypeChanged += sinkTypeHandler;
            sinkManager.OnNodePurchased += nodePurchasedHandler;
            sinkManager.OnSinkReset += sinkResetHandler;
        }

        OnSinkStateChanged();
    }

    private void OnDestroy()
    {
        if (sinkManager != null)
        {
            if (sinkTypeHandler != null) sinkManager.OnSinkTypeChanged -= sinkTypeHandler;
            if (nodePurchasedHandler != null) sinkManager.OnNodePurchased -= nodePurchasedHandler;
            if (sinkResetHandler != null) sinkManager.OnSinkReset -= sinkResetHandler;
        }
    }

    private void Update()
    {
        if (sinkManager == null || scoreManager == null) return;

        bool dishwasherActive = sinkManager.CurrentSinkType == SinkManager.SinkType.Dishwasher;
        if (!dishwasherActive)
        {
            SetUIVisible(false);
            return;
        }

        SetUIVisible(true);

        if (!runWhilePaused && Time.timeScale == 0f)
        {
            UpdateUI();
            return;
        }

        if (cycleDuration <= 0f) cycleDuration = 1f;

        float dt = runWhilePaused ? Time.unscaledDeltaTime : Time.deltaTime;

        cycleRemaining -= dt;
        if (cycleRemaining <= 0f)
        {
            TriggerAutoWash();
            // Start next cycle (use latest upgrades)
            cycleDuration = sinkManager.GetDishwasherCycleSeconds();
            cycleRemaining = cycleDuration;
        }

        UpdateUI();
    }

    private void TriggerAutoWash()
    {
        DishData dish = null;
        if (dishClicker != null) dish = dishClicker.GetCurrentDishData();

        if (dish == null)
        {
            // Fallback: try score manager's starter list
            if (scoreManager.allDishes != null && scoreManager.allDishes.Count > 0)
                dish = scoreManager.allDishes[0];
        }

        if (dish == null) return;

        long baseDishes = 1;
        if (dishClicker != null)
        {
            // Uses same logic as manual completion but does not change visuals.
            baseDishes = dishClicker.PreviewManualDishesAwarded();
        }
        else
        {
            baseDishes = Mathf.Max(1, scoreManager.GetDishCountIncrement());
        }

        float mult = sinkManager.GetDishwasherDishesMultiplier();
        long dishesAwarded = (long)System.Math.Max(1, System.Math.Round(baseDishes * mult));

        autoWashCount++;

        float cashMult = 1f;
        if (sinkManager.HasHeatDryBoostTechnique())
        {
            // Every 10th auto-wash yields double cash
            if (autoWashCount % 10 == 0) cashMult = 2f;
        }

        scoreManager.AwardBackgroundWash(dish, dishesAwarded, cashMult);

        if (statusText != null)
        {
            if (cashMult > 1f) statusText.text = "Heat Dry Boost x2";
        }
    }

    private void OnSinkStateChanged()
    {
        if (sinkManager == null) return;

        bool dishwasherActive = sinkManager.CurrentSinkType == SinkManager.SinkType.Dishwasher;
        if (!dishwasherActive)
        {
            SetUIVisible(false);
            autoWashCount = 0;
            cycleDuration = 300f;
            cycleRemaining = 300f;
            return;
        }

        cycleDuration = sinkManager.GetDishwasherCycleSeconds();
        cycleRemaining = Mathf.Clamp(cycleRemaining, 0f, cycleDuration);

        // If we just switched to dishwasher, start a full cycle.
        if (cycleRemaining <= 0.01f) cycleRemaining = cycleDuration;

        SetUIVisible(true);
        UpdateUI();
    }

    private void OnUpgradesChanged()
    {
        if (sinkManager == null) return;
        if (sinkManager.CurrentSinkType != SinkManager.SinkType.Dishwasher) return;

        // Adjust remaining time proportionally when cycle length changes
        float oldDuration = Mathf.Max(1f, cycleDuration);
        float newDuration = Mathf.Max(1f, sinkManager.GetDishwasherCycleSeconds());

        float progress = 1f - Mathf.Clamp01(cycleRemaining / oldDuration);
        cycleDuration = newDuration;
        cycleRemaining = Mathf.Clamp(newDuration * (1f - progress), 0f, newDuration);

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timerFill != null)
        {
            float t = (cycleDuration <= 0f) ? 0f : 1f - Mathf.Clamp01(cycleRemaining / cycleDuration);
            timerFill.fillAmount = t;
        }

        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, cycleRemaining));
            int min = seconds / 60;
            int sec = seconds % 60;
            timerText.text = $"Rinse {min}:{sec:00}";
        }

        if (statusText != null)
        {
            if (sinkManager == null || sinkManager.CurrentSinkType != SinkManager.SinkType.Dishwasher)
            {
                statusText.text = "";
                return;
            }

            if (sinkManager.HasHeatDryBoostTechnique())
            {
                int next = 10 - (autoWashCount % 10);
                if (next == 10) next = 0;
                statusText.text = next == 0 ? "Next auto-wash x2" : $"x2 in {next} auto";
            }
            else
            {
                statusText.text = "";
            }
        }
    }

    private void SetUIVisible(bool visible)
    {
        if (timerRoot != null) timerRoot.SetActive(visible);
        else
        {
            if (timerFill != null) timerFill.gameObject.SetActive(visible);
            if (timerText != null) timerText.gameObject.SetActive(visible);
            if (statusText != null) statusText.gameObject.SetActive(visible);
        }
    }

    // --- Optional save hooks ---
    public float GetCycleRemainingSeconds() => cycleRemaining;
    public int GetAutoWashCount() => autoWashCount;

    public void LoadDishwasherState(float remainingSeconds, int loadedAutoWashCount)
    {
        autoWashCount = Mathf.Max(0, loadedAutoWashCount);

        if (sinkManager == null) sinkManager = FindFirstObjectByType<SinkManager>();
        cycleDuration = sinkManager != null ? sinkManager.GetDishwasherCycleSeconds() : 300f;
        cycleRemaining = Mathf.Clamp(remainingSeconds, 0f, Mathf.Max(1f, cycleDuration));

        UpdateUI();
    }
}
