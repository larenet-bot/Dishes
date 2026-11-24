using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dishwasher V4. Does NOT start cycles until DishwasherLoadMenu submits a
/// valid arrangement. Rewards the exact loaded dish types at cycle completion.
/// Replaces the old timer-only auto-wash controller without modifying ScoreManager.
/// </summary>
public class DishwasherCycleController : MonoBehaviour
{
    [Header("Existing References")]
    public SinkManager sinkManager;
    public ScoreManager scoreManager;
    public DishClicker dishClicker; // Retained for existing inspector links; not used for loaded rewards.

    [Header("Loading Menu")]
    [SerializeField] private DishwasherLoadMenu loadMenu;

    [Header("Existing Timer UI")]
    public GameObject timerRoot;
    public Image timerFill;
    public TMP_Text timerText;
    public TMP_Text statusText;

    [Header("Cycle Behavior")]
    public bool runWhilePaused = false;
    [Header("Dishpod Technique")]
    [Tooltip("A dishpod placed in the load multiplies the final payout of every loaded dish.")]
    [SerializeField, Min(1f)] private float dishpodProfitMultiplier = 1.25f;

    [Header("Completion (optional)")]
    [SerializeField] private AudioClip completionDing;
    [Tooltip("Use the EXISTING RewardTextSpawner component, not a new prefab.")]
    [SerializeField] private RewardTextSpawner rewardTextSpawner;
    [Tooltip("Legacy V3 reference; no longer needed. V4 displays profit at the cursor.")]
    [SerializeField] private Transform payoutWorldAnchor;
    [Tooltip("Optional Canvas used for a floating UI reward above the cursor. If unassigned, V4 uses the dishwasher menu's Canvas.")]
    [SerializeField] private Canvas cursorRewardCanvas;
    [Tooltip("Optional TMP style source for the reward (font and color). Default: menu StatusText.")]
    [SerializeField] private TMP_Text cursorRewardTextStyle;

    private readonly List<DishData> activeLoad = new List<DishData>();
    private float cycleDuration;
    private float cycleRemaining;
    private int completedCycles;
    private bool isRunning;
    private bool activeHasDishpod;
    private Action<SinkManager.SinkType> sinkTypeHandler;
    private Action<string> nodePurchasedHandler;
    private Action sinkResetHandler;

    public float DishpodProfitMultiplier => Mathf.Max(1f, dishpodProfitMultiplier);
    public bool IsRunning => isRunning;
    public float SecondsRemaining => isRunning ? cycleRemaining : 0f;
    public int LoadedDishCount => activeLoad.Count;
    public event Action<int, double> CycleCompleted;

    private void Reset()
    {
        sinkManager = FindAnyObjectByType<SinkManager>();
        scoreManager = FindAnyObjectByType<ScoreManager>();
        dishClicker = FindAnyObjectByType<DishClicker>();
        loadMenu = FindAnyObjectByType<DishwasherLoadMenu>();
    }

    private void Awake()
    {
        if (sinkManager == null) sinkManager = FindAnyObjectByType<SinkManager>();
        if (scoreManager == null) scoreManager = ScoreManager.Instance != null ? ScoreManager.Instance : FindAnyObjectByType<ScoreManager>();
        if (loadMenu == null) loadMenu = FindAnyObjectByType<DishwasherLoadMenu>();

        sinkTypeHandler = _ => OnSinkStateChanged();
        nodePurchasedHandler = OnUpgradesChanged;
        sinkResetHandler = OnSinkStateChanged;
        if (sinkManager != null)
        {
            sinkManager.OnSinkTypeChanged += sinkTypeHandler;
            sinkManager.OnNodePurchased += nodePurchasedHandler;
            sinkManager.OnSinkReset += sinkResetHandler;
        }
        // Reuse the same cursor-following reward spawner as manual dish clicks.
        // The former anchor requirement could silently suppress all payout text.
        if (rewardTextSpawner == null && dishClicker == null)
            dishClicker = FindAnyObjectByType<DishClicker>();
        if (rewardTextSpawner == null && dishClicker != null)
            rewardTextSpawner = dishClicker.rewardTextSpawner;
        if (rewardTextSpawner == null)
            rewardTextSpawner = FindAnyObjectByType<RewardTextSpawner>();
        OnSinkStateChanged();
    }

    private void OnDestroy()
    {
        if (sinkManager == null) return;
        if (sinkTypeHandler != null) sinkManager.OnSinkTypeChanged -= sinkTypeHandler;
        if (nodePurchasedHandler != null) sinkManager.OnNodePurchased -= nodePurchasedHandler;
        if (sinkResetHandler != null) sinkManager.OnSinkReset -= sinkResetHandler;
    }

    private void Update()
    {
        bool active = sinkManager != null && sinkManager.CurrentSinkType == SinkManager.SinkType.Dishwasher;
        SetUIVisible(active);
        if (!active) return;

        if (isRunning && (runWhilePaused || Time.timeScale != 0f))
        {
            cycleRemaining -= runWhilePaused ? Time.unscaledDeltaTime : Time.deltaTime;
            if (cycleRemaining <= 0f) CompleteCycle();
        }
        UpdateUI();
    }

    public bool TryStartLoadedCycle(IReadOnlyList<DishData> load) => TryStartLoadedCycle(load, false);

    public bool TryStartLoadedCycle(IReadOnlyList<DishData> load, bool hasDishpod)
    {
        if (isRunning || sinkManager == null || scoreManager == null ||
            sinkManager.CurrentSinkType != SinkManager.SinkType.Dishwasher || load == null || load.Count == 0)
            return false;

        for (int i = 0; i < load.Count; i++)
            if (load[i] == null) return false;

        activeLoad.Clear();
        for (int i = 0; i < load.Count; i++) activeLoad.Add(load[i]);
        activeHasDishpod = hasDishpod && sinkManager.HasDishpodTechnique();
        cycleDuration = Mathf.Max(1f, sinkManager.GetDishwasherCycleSeconds());
        cycleRemaining = cycleDuration;
        isRunning = true;
        UpdateUI();
        return true;
    }

    private void CompleteCycle()
    {
        if (!isRunning) return;
        isRunning = false;  // Mark complete BEFORE calling any other systems.
        cycleRemaining = 0f;
        int loadedCount = activeLoad.Count;
        completedCycles++;

        float cashMultiplier = activeHasDishpod ? Mathf.Max(1f, dishpodProfitMultiplier) : 1f;

        // Compute the awarded amount from the same inputs ScoreManager uses.
        // Subtracting two large wallet totals can round a valid reward to zero.
        double moneyEarned = 0d;
        if (scoreManager != null)
        {
            float effectiveMultiplier = scoreManager.GetEffectiveDishProfitMultiplier();
            for (int i = 0; i < activeLoad.Count; i++)
            {
                DishData dish = activeLoad[i];
                moneyEarned += (double)dish.profitPerDish * effectiveMultiplier * cashMultiplier;
                // ScoreManager remains the ONLY place that credits the wallet,
                // completed dishes, and the measured profit-rate income.
                scoreManager.AwardBackgroundWash(dish, 1L, cashMultiplier);
            }
        }

        activeLoad.Clear();
        activeHasDishpod = false;
        if (completionDing != null && AudioManager.instance != null)
            AudioManager.instance.PlaySFX(completionDing);

        if (moneyEarned > 0d)
        {
            Canvas canvas = cursorRewardCanvas != null ? cursorRewardCanvas :
                (loadMenu != null ? loadMenu.RewardCanvas : null);
            TMP_Text style = cursorRewardTextStyle != null ? cursorRewardTextStyle :
                (loadMenu != null ? loadMenu.RewardTextStyle : timerText);
            // UI first, so it renders on top of the dishwasher even while open.
            bool shown = DishwasherCursorRewardText.Show(canvas, style, moneyEarned);
            if (!shown)
            {
                if (rewardTextSpawner == null && dishClicker != null)
                    rewardTextSpawner = dishClicker.rewardTextSpawner;
                if (rewardTextSpawner == null)
                    rewardTextSpawner = FindAnyObjectByType<RewardTextSpawner>();
                if (rewardTextSpawner != null)
                    rewardTextSpawner.SpawnRewardText((float)Math.Min(float.MaxValue, moneyEarned));
                else
                    Debug.LogWarning("[Dishwasher] No reward Canvas or RewardTextSpawner available.", this);
            }
        }

        UpdateUI();
        CycleCompleted?.Invoke(loadedCount, moneyEarned);
    }

    private void OnSinkStateChanged()
    {
        bool active = sinkManager != null && sinkManager.CurrentSinkType == SinkManager.SinkType.Dishwasher;
        if (!active)
        {
            CancelCycle();
            completedCycles = 0;
        }
        SetUIVisible(active);
        UpdateUI();
    }

    private void OnUpgradesChanged(string nodeId)
    {
        if (!isRunning || sinkManager == null) return;
        // Buying Top Rack, Double Decker, Triple Decker, or Dishpod must NEVER
        // restart or adjust an already-running cycle. Only timer nodes alter timing.
        if (nodeId != "dw_time1" && nodeId != "dw_time2" && nodeId != "dw_time3") return;
        float oldDuration = Mathf.Max(1f, cycleDuration);
        float newDuration = Mathf.Max(1f, sinkManager.GetDishwasherCycleSeconds());
        float progress = 1f - Mathf.Clamp01(cycleRemaining / oldDuration);
        cycleDuration = newDuration;
        cycleRemaining = newDuration * (1f - progress);
        UpdateUI();
    }

    private void CancelCycle()
    {
        isRunning = false;
        activeHasDishpod = false;
        activeLoad.Clear();
        cycleRemaining = 0f;
        cycleDuration = 0f;
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

    private void UpdateUI()
    {
        if (timerFill != null)
            timerFill.fillAmount = isRunning && cycleDuration > 0f ?
                1f - Mathf.Clamp01(cycleRemaining / cycleDuration) : 0f;

        if (timerText != null)
        {
            if (isRunning)
            {
                int remaining = Mathf.CeilToInt(Mathf.Max(0f, cycleRemaining));
                timerText.text = $"Rinse {remaining / 60}:{remaining % 60:00}";
            }
            else timerText.text = "Ready to Load";
        }
        if (statusText != null)
        {
            if (isRunning) statusText.text = $"Washing {activeLoad.Count} dishes";
            else statusText.text = "";
        }
    }

    public void WriteSaveState(DishwasherSaveState save)
    {
        if (save == null) return;
        save.cycleRunning = isRunning;
        save.secondsRemaining = isRunning ? Mathf.Max(0f, cycleRemaining) : 0f;
        save.cycleDuration = isRunning ? cycleDuration : 0f;
        save.completedCycles = completedCycles;
        save.cycleHasDishpod = isRunning && activeHasDishpod;
    }

    public void ApplySaveState(DishwasherSaveState save, DishSpawner spawner)
    {
        CancelCycle();
        completedCycles = save != null ? Mathf.Max(0, save.completedCycles) : 0;
        if (save == null || !save.cycleRunning || spawner == null || sinkManager == null ||
            sinkManager.CurrentSinkType != SinkManager.SinkType.Dishwasher) { UpdateUI(); return; }

        if (save.dishes != null)
        {
            foreach (DishwasherSavedDish entry in save.dishes)
            {
                if (entry == null || !entry.inDishwasher) continue;
                DishData data = DishwasherLoadMenu.ResolveDish(spawner, entry);
                if (data != null) activeLoad.Add(data);
            }
        }
        if (activeLoad.Count == 0)
        {
            Debug.LogWarning("[Dishwasher] A saved cycle had no resolvable dishes. It was cancelled.", this);
            UpdateUI();
            return;
        }
        activeHasDishpod = save.cycleHasDishpod && sinkManager.HasDishpodTechnique();
        cycleDuration = save.cycleDuration > 0f ? save.cycleDuration :
            Mathf.Max(1f, sinkManager.GetDishwasherCycleSeconds());
        cycleRemaining = Mathf.Clamp(save.secondsRemaining, 0f, cycleDuration);
        isRunning = true;
        UpdateUI();
    }

    // Compatibility hooks for any older code calling these methods.
    public float GetCycleRemainingSeconds() => SecondsRemaining;
    public int GetAutoWashCount() => completedCycles;
    public void LoadDishwasherState(float remainingSeconds, int previousAutoWashCount)
    {
        // No historical load composition exists in the old save format, so
        // restoring only a timer must never create phantom money.
        completedCycles = Mathf.Max(0, previousAutoWashCount);
        if (isRunning) cycleRemaining = Mathf.Clamp(remainingSeconds, 0f, cycleDuration);
        UpdateUI();
    }
}
