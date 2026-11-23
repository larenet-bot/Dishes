using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dishwasher V2. Does NOT start cycles until DishwasherLoadMenu submits a
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
    [Tooltip("Until the upgrade tree is reworked, preserve purchased dw_amount bonuses as extra washes of loaded dishes.")]
    [SerializeField] private bool honorLegacyDishCountUpgrades = true;

    [Header("Completion (optional)")]
    [SerializeField] private AudioClip completionDing;
    [Tooltip("Use the EXISTING RewardTextSpawner component, not a new prefab.")]
    [SerializeField] private RewardTextSpawner rewardTextSpawner;
    [Tooltip("A world-space transform above the dishwasher scene button/image.")]
    [SerializeField] private Transform payoutWorldAnchor;

    private readonly List<DishData> activeLoad = new List<DishData>();
    private float cycleDuration;
    private float cycleRemaining;
    private int completedCycles;
    private bool isRunning;
    private Action<SinkManager.SinkType> sinkTypeHandler;
    private Action<string> nodePurchasedHandler;
    private Action sinkResetHandler;

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
        nodePurchasedHandler = _ => OnUpgradesChanged();
        sinkResetHandler = OnSinkStateChanged;
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

    public bool TryStartLoadedCycle(IReadOnlyList<DishData> load)
    {
        if (isRunning || sinkManager == null || scoreManager == null ||
            sinkManager.CurrentSinkType != SinkManager.SinkType.Dishwasher || load == null || load.Count == 0)
            return false;

        for (int i = 0; i < load.Count; i++)
            if (load[i] == null) return false;

        activeLoad.Clear();
        for (int i = 0; i < load.Count; i++) activeLoad.Add(load[i]);
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

        float cashMultiplier = (sinkManager != null && sinkManager.HasHeatDryBoostTechnique() &&
            completedCycles % 10 == 0) ? 2f : 1f;

        double moneyBefore = scoreManager != null ? scoreManager.GetTotalProfitDouble() : 0d;
        if (scoreManager != null)
        {
            for (int i = 0; i < activeLoad.Count; i++)
                scoreManager.AwardBackgroundWash(activeLoad[i], 1L, cashMultiplier);

            // Keep old purchased amount-branch upgrades functional until their
            // replacement with dishwasher-size upgrades. Extra counted washes
            // use the most valuable dish already present in the load.
            if (honorLegacyDishCountUpgrades && activeLoad.Count > 0 && sinkManager != null)
            {
                double mult = Mathf.Max(1f, sinkManager.GetDishwasherDishesMultiplier());
                long targetCount = (long)Math.Round(loadedCount * mult, MidpointRounding.AwayFromZero);
                long extra = Math.Max(0L, targetCount - loadedCount);
                DishData mostValuable = activeLoad[0];
                for (int i = 1; i < activeLoad.Count; i++)
                    if (activeLoad[i].profitPerDish > mostValuable.profitPerDish)
                        mostValuable = activeLoad[i];
                if (extra > 0L) scoreManager.AwardBackgroundWash(mostValuable, extra, cashMultiplier);
            }
        }
        double moneyEarned = scoreManager != null
            ? Math.Max(0d, scoreManager.GetTotalProfitDouble() - moneyBefore) : 0d;

        activeLoad.Clear();
        if (completionDing != null && AudioManager.instance != null)
            AudioManager.instance.PlaySFX(completionDing);

        if (rewardTextSpawner != null && payoutWorldAnchor != null && moneyEarned > 0d)
        {
            float safeFloat = (float)Math.Min(float.MaxValue, moneyEarned);
            rewardTextSpawner.SpawnRewardTextAtWorld(safeFloat, payoutWorldAnchor.position);
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

    private void OnUpgradesChanged()
    {
        if (!isRunning || sinkManager == null) return;
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
            else if (sinkManager != null && sinkManager.CurrentSinkType == SinkManager.SinkType.Dishwasher &&
                sinkManager.HasHeatDryBoostTechnique())
                statusText.text = $"Heat Dry: {10 - (completedCycles % 10)} cycles to x2";
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
