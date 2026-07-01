using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent save manager with one independent save bucket per kitchen id.
/// Also applies background earnings to unloaded kitchens using the rate shown
/// in the Other Businesses menu.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Refs (leave empty if these exist in gameplay scenes)")]
    [SerializeField] private ScoreManager score;
    [SerializeField] private EmployeeManager employees;
    [SerializeField] private Upgrades upgrades;
    [SerializeField] private SinkManager sinks;
    [SerializeField] private LoanManager loans;
    [SerializeField] private MopUpgradeShelf mop;

    [Header("Kitchen Id")]
    [Tooltip("Used only if SaveManager cannot find a LoanManager or KitchenIdentity in the active scene.")]
    [SerializeField] private string fallbackKitchenId = "kitchen_1";

    [Header("Autosave")]
    [SerializeField] private bool autosave = true;
    [SerializeField] private float autosaveInterval = 20f;

    [Header("Background Earnings")]
    [Tooltip("How often unloaded kitchens are advanced in memory while the game is running.")]
    [SerializeField] private float backgroundEarningsUpdateInterval = 1f;

    [Tooltip("If true, total dishes also rise for unloaded kitchens using their saved dishes/sec rate.")]
    [SerializeField] private bool backgroundEarningsAddDishes = true;

    [Tooltip("Safety cap so a corrupted clock or long absence does not instantly create impossible amounts. 0 means no global cap.")]
    [SerializeField] private int maxBackgroundEarningSecondsPerTick = 0;

    [Header("Offline Earnings")]
    [Tooltip("When true, the first kitchen scene loaded after reopening the game grants saved offline earnings for every kitchen.")]
    [SerializeField] private bool enableOfflineEarnings = true;

    [Tooltip("When true, a panel is shown after offline earnings are granted.")]
    [SerializeField] private bool showOfflineEarningsPanel = true;

    [Tooltip("Optional safety cap for offline earnings. 0 means no global cap. Per-kitchen piggy bank tier value overrides this.")]
    [SerializeField] private int maxOfflineEarningSeconds = 0;

    [Tooltip("Do not show the offline panel unless total earnings are at least this much.")]
    [SerializeField] private float minimumOfflineEarningsToShow = 0.01f;

    [Header("Debug")]
    [SerializeField] private bool logSaveEvents = false;

    private float timer;
    private float backgroundTimer;
    private SaveData loadedData;
    private bool hasLoadedFile;
    private bool hasAppliedToCurrentScene;
    private bool offlineEarningsAppliedThisSession;

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        TryBindRefs();
        ReadSaveFileToMemory();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        TryApplyLoadedDataToScene();
    }

    private void Update()
    {
        backgroundTimer += Time.unscaledDeltaTime;
        if (backgroundTimer >= backgroundEarningsUpdateInterval)
        {
            backgroundTimer = 0f;
            UpdateBackgroundEarningsInMemory();
        }

        if (!autosave)
        {
            return;
        }

        timer += Time.unscaledDeltaTime;

        if (timer >= autosaveInterval)
        {
            timer = 0f;
            Save();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Save();
        }
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        score = null;
        employees = null;
        upgrades = null;
        sinks = null;
        loans = null;
        mop = null;

        hasAppliedToCurrentScene = false;

        TryBindRefs();
        TryApplyLoadedDataToScene();
    }

    private void TryBindRefs()
    {
        if (score == null) score = FindFirstObjectByType<ScoreManager>();
        if (employees == null) employees = FindFirstObjectByType<EmployeeManager>();
        if (upgrades == null) upgrades = FindFirstObjectByType<Upgrades>();
        if (sinks == null) sinks = FindFirstObjectByType<SinkManager>();
        if (loans == null) loans = FindFirstObjectByType<LoanManager>();
        if (mop == null) mop = FindFirstObjectByType<MopUpgradeShelf>();
    }

    private bool HaveGameplayRefs()
    {
        // These three are the minimum refs needed to save/restore the core kitchen economy.
        // Sinks and loans are optional so cut-down test scenes don't block saving.
        return score != null && employees != null && upgrades != null;
    }

    private string ResolveCurrentKitchenId()
    {
        if (loans != null && !string.IsNullOrWhiteSpace(loans.GetKitchenId()))
        {
            return loans.GetKitchenId();
        }

        KitchenIdentity identity = FindFirstObjectByType<KitchenIdentity>();
        if (identity != null && !string.IsNullOrWhiteSpace(identity.KitchenId))
        {
            return identity.KitchenId;
        }

        return string.IsNullOrWhiteSpace(fallbackKitchenId) ? "kitchen_1" : fallbackKitchenId;
    }

    private long NowUnixSeconds()
    {
        return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private static float ToSafeFloat(double value)
    {
        if (double.IsNaN(value) || value <= 0d)
        {
            return 0f;
        }

        if (double.IsInfinity(value) || value > float.MaxValue)
        {
            return float.MaxValue;
        }

        return (float)value;
    }

    private void ReadSaveFileToMemory()
    {
        hasLoadedFile = false;
        loadedData = null;

        if (!File.Exists(SavePath))
        {
            loadedData = new SaveData();
            hasLoadedFile = true;

            if (logSaveEvents)
            {
                Debug.Log($"[SaveManager] No save file. New save data started at: {SavePath}");
            }

            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            loadedData = JsonUtility.FromJson<SaveData>(json);

            if (loadedData == null)
            {
                loadedData = new SaveData();
            }

            if (loadedData.kitchens == null)
            {
                loadedData.kitchens = new List<KitchenSaveData>();
            }

            MigrateLegacySingleKitchenSaveIfNeeded();
            NormalizeAllKitchenData();
            hasLoadedFile = true;

            if (logSaveEvents)
            {
                Debug.Log($"[SaveManager] Loaded save file from: {SavePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SaveManager] Failed to read save file. {e.Message}");
            loadedData = new SaveData();
            hasLoadedFile = true;
        }
    }

    private void MigrateLegacySingleKitchenSaveIfNeeded()
    {
        if (loadedData == null)
        {
            loadedData = new SaveData();
        }

        if (loadedData.kitchens == null)
        {
            loadedData.kitchens = new List<KitchenSaveData>();
        }

        if (loadedData.kitchens.Count > 0)
        {
            return;
        }

        bool hasLegacyProgress =
            loadedData.totalDishes > 0 ||
            loadedData.totalProfit > 0f ||
            loadedData.currentLoanIndex > 0 ||
            loadedData.currentSoapIndex > 0 ||
            loadedData.currentGloveIndex > 0 ||
            loadedData.currentSpongeIndex > 0 ||
            loadedData.currentPiggyBankIndex > 0 ||
            loadedData.currentMopIndex > 0 ||
            loadedData.dishCountIncrement > 1 ||
            loadedData.dishProfitMultiplier > 1f ||
            loadedData.radioOwned ||
            loadedData.cachedMoneyPerSecond > 0f ||
            loadedData.cachedDishesPerSecond > 0f ||
            (loadedData.employees != null && loadedData.employees.Count > 0) ||
            (loadedData.purchasedSinkNodeIds != null && loadedData.purchasedSinkNodeIds.Count > 0);

        if (!hasLegacyProgress)
        {
            return;
        }

        KitchenSaveData kitchenOne = new KitchenSaveData
        {
            kitchenId = "kitchen_1",
            totalDishes = loadedData.totalDishes,
            totalProfit = loadedData.totalProfit,
            dishCountIncrement = System.Math.Max(1, loadedData.dishCountIncrement),
            profitPerDish = loadedData.profitPerDish > 0f ? loadedData.profitPerDish : 1f,
            dishProfitMultiplier = loadedData.dishProfitMultiplier > 0f ? loadedData.dishProfitMultiplier : 1f,
            currentSoapIndex = System.Math.Max(0, loadedData.currentSoapIndex),
            currentGloveIndex = System.Math.Max(0, loadedData.currentGloveIndex),
            currentSpongeIndex = System.Math.Max(0, loadedData.currentSpongeIndex),
            currentPiggyBankIndex = System.Math.Max(0, loadedData.currentPiggyBankIndex),
            currentMopIndex = System.Math.Max(0, loadedData.currentMopIndex),
            selectedMopFilterId = string.IsNullOrWhiteSpace(loadedData.selectedMopFilterId) ? MopUpgradeShelf.CleanFilterId : loadedData.selectedMopFilterId,
            purchasedMopFilterIds = loadedData.purchasedMopFilterIds ?? new List<string>(),
            selectedMopStaticColorHtml = string.IsNullOrWhiteSpace(loadedData.selectedMopStaticColorHtml) ? "#0000004D" : loadedData.selectedMopStaticColorHtml,
            radioOwned = loadedData.radioOwned,
            employees = loadedData.employees ?? new List<EmployeeSave>(),
            employeeProfitMultiplier = loadedData.employeeProfitMultiplier > 0f ? loadedData.employeeProfitMultiplier : 1f,
            currentSinkType = loadedData.currentSinkType,
            purchasedSinkNodeIds = loadedData.purchasedSinkNodeIds ?? new List<string>(),
            currentLoanIndex = loadedData.currentLoanIndex,
            cachedMoneyPerSecond = System.Math.Max(0f, loadedData.cachedMoneyPerSecond),
            cachedDishesPerSecond = System.Math.Max(0f, loadedData.cachedDishesPerSecond),
            lastBackgroundEarningsUnixSeconds = loadedData.lastBackgroundEarningsUnixSeconds,
            backgroundDishFraction = Mathf.Clamp01(loadedData.backgroundDishFraction)
        };

        loadedData.kitchens.Add(kitchenOne);
        loadedData.saveVersion = 6;

        if (logSaveEvents)
        {
            Debug.Log("[SaveManager] Migrated old single-kitchen save into kitchen_1.");
        }
    }

    private void NormalizeAllKitchenData()
    {
        if (loadedData == null || loadedData.kitchens == null)
        {
            return;
        }

        for (int i = 0; i < loadedData.kitchens.Count; i++)
        {
            NormalizeKitchenData(loadedData.kitchens[i]);
        }
    }

    private KitchenSaveData GetOrCreateKitchenData(string kitchenId)
    {
        if (loadedData == null)
        {
            loadedData = new SaveData();
        }

        if (loadedData.kitchens == null)
        {
            loadedData.kitchens = new List<KitchenSaveData>();
        }

        for (int i = 0; i < loadedData.kitchens.Count; i++)
        {
            KitchenSaveData kitchen = loadedData.kitchens[i];

            if (kitchen != null && kitchen.kitchenId == kitchenId)
            {
                NormalizeKitchenData(kitchen);
                return kitchen;
            }
        }

        KitchenSaveData created = CreateDefaultKitchenData(kitchenId);
        loadedData.kitchens.Add(created);
        return created;
    }

    private KitchenSaveData CreateDefaultKitchenData(string kitchenId)
    {
        KitchenSaveData kitchen = new KitchenSaveData
        {
            kitchenId = string.IsNullOrWhiteSpace(kitchenId) ? "kitchen_1" : kitchenId,
            totalDishes = 0,
            totalProfit = 0f,
            dishCountIncrement = 1,
            profitPerDish = 1f,
            dishProfitMultiplier = 1f,
            currentSoapIndex = 0,
            currentGloveIndex = 0,
            currentSpongeIndex = 0,
            currentPiggyBankIndex = 0,
            currentMopIndex = 0,
            selectedMopFilterId = MopUpgradeShelf.CleanFilterId,
            purchasedMopFilterIds = new List<string> { MopUpgradeShelf.CleanFilterId },
            selectedMopStaticColorHtml = "#0000004D",
            radioOwned = false,
            employees = new List<EmployeeSave>(),
            employeeProfitMultiplier = 1f,
            currentSinkType = 0,
            purchasedSinkNodeIds = new List<string>(),
            currentLoanIndex = 0,
            cachedMoneyPerSecond = 0f,
            cachedDishesPerSecond = 0f,
            lastBackgroundEarningsUnixSeconds = NowUnixSeconds(),
            backgroundDishFraction = 0f,
            achievedAchievementIds = new List<string>()
        };

        return kitchen;
    }

    private void NormalizeKitchenData(KitchenSaveData kitchen)
    {
        if (kitchen == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(kitchen.kitchenId))
        {
            kitchen.kitchenId = "kitchen_1";
        }

        if (kitchen.dishCountIncrement < 1)
        {
            kitchen.dishCountIncrement = 1;
        }

        if (kitchen.profitPerDish <= 0f)
        {
            kitchen.profitPerDish = 1f;
        }

        if (kitchen.dishProfitMultiplier <= 0f)
        {
            kitchen.dishProfitMultiplier = 1f;
        }

        if (kitchen.employeeProfitMultiplier <= 0f)
        {
            kitchen.employeeProfitMultiplier = 1f;
        }

        if (kitchen.currentMopIndex < 0)
        {
            kitchen.currentMopIndex = 0;
        }

        if (string.IsNullOrWhiteSpace(kitchen.selectedMopFilterId))
        {
            kitchen.selectedMopFilterId = MopUpgradeShelf.CleanFilterId;
        }

        if (string.IsNullOrWhiteSpace(kitchen.selectedMopStaticColorHtml))
        {
            kitchen.selectedMopStaticColorHtml = "#0000004D";
        }

        if (kitchen.purchasedMopFilterIds == null)
        {
            kitchen.purchasedMopFilterIds = new List<string>();
        }

        if (!kitchen.purchasedMopFilterIds.Contains(MopUpgradeShelf.CleanFilterId))
        {
            kitchen.purchasedMopFilterIds.Add(MopUpgradeShelf.CleanFilterId);
        }

        if (kitchen.currentMopIndex >= 2 && !kitchen.purchasedMopFilterIds.Contains(kitchen.selectedMopFilterId))
        {
            kitchen.purchasedMopFilterIds.Add(kitchen.selectedMopFilterId);
        }

        if (kitchen.employees == null)
        {
            kitchen.employees = new List<EmployeeSave>();
        }

        if (kitchen.purchasedSinkNodeIds == null)
        {
            kitchen.purchasedSinkNodeIds = new List<string>();
        }

        if (kitchen.achievedAchievementIds == null)
        {
            kitchen.achievedAchievementIds = new List<string>();
        }

        if (kitchen.cachedMoneyPerSecond < 0f)
        {
            kitchen.cachedMoneyPerSecond = 0f;
        }

        if (kitchen.cachedDishesPerSecond < 0f)
        {
            kitchen.cachedDishesPerSecond = 0f;
        }

        if (kitchen.backgroundDishFraction < 0f || kitchen.backgroundDishFraction >= 1f)
        {
            kitchen.backgroundDishFraction = Mathf.Repeat(kitchen.backgroundDishFraction, 1f);
        }

        if (kitchen.lastBackgroundEarningsUnixSeconds <= 0)
        {
            kitchen.lastBackgroundEarningsUnixSeconds = NowUnixSeconds();
        }
    }

    private void TryApplyLoadedDataToScene()
    {
        if (hasAppliedToCurrentScene)
        {
            return;
        }

        if (!hasLoadedFile || loadedData == null)
        {
            return;
        }

        TryBindRefs();

        if (!HaveGameplayRefs())
        {
            return;
        }

        string kitchenId = ResolveCurrentKitchenId();

        OfflineEarningsReport offlineReport = ApplyOfflineEarningsOnceForAllKitchens();

        KitchenSaveData kitchen = GetOrCreateKitchenData(kitchenId);

        // The offline pass catches up every kitchen once. This call is harmless if nothing changed.
        ApplyBackgroundEarningsToKitchen(kitchen, NowUnixSeconds());

        score.LoadFromSave(
            kitchen.totalDishes,
            kitchen.totalProfit,
            kitchen.dishCountIncrement,
            kitchen.profitPerDish,
            kitchen.dishProfitMultiplier
        );

        upgrades.ApplySaveState(
            kitchen.currentSoapIndex,
            kitchen.currentGloveIndex,
            kitchen.currentSpongeIndex,
            kitchen.radioOwned,
            kitchen.currentPiggyBankIndex
        );

        if (mop != null)
        {
            mop.ApplySaveState(
                kitchen.currentMopIndex,
                kitchen.selectedMopFilterId,
                kitchen.purchasedMopFilterIds,
                kitchen.selectedMopStaticColorHtml
            );
        }

        employees.ApplySaveState(kitchen.employees, kitchen.employeeProfitMultiplier);

        if (loans != null)
        {
            loans.ApplyLoanIndexFromSave(kitchen.currentLoanIndex);
        }

        if (sinks != null)
        {
            List<string> purchasedSinkNodes = kitchen.purchasedSinkNodeIds ?? new List<string>();
            sinks.LoadFromSave((SinkManager.SinkType)kitchen.currentSinkType, purchasedSinkNodes);
        }

        if (AchievementManager.Instance != null)
        {
            kitchen.achievedAchievementIds = AchievementManager.Instance.GetUnlockedAchievementIds() ?? new List<string>();
        }

        RefreshCurrentKitchenRatesInMemory(kitchenId);

        if (ProfitRate.Instance != null)
        {
            ProfitRate.Instance.ResetBaseline();
        }

        hasAppliedToCurrentScene = true;

        TryShowOfflineEarningsReport(offlineReport);

        if (logSaveEvents)
        {
            Debug.Log($"[SaveManager] Applied save data for {kitchenId}.");
        }
    }

    private void UpdateBackgroundEarningsInMemory()
    {
        if (loadedData == null || loadedData.kitchens == null)
        {
            return;
        }

        TryBindRefs();

        bool hasGameplayRefs = HaveGameplayRefs();
        string currentKitchenId = hasGameplayRefs ? ResolveCurrentKitchenId() : string.Empty;
        long now = NowUnixSeconds();

        ApplyBackgroundEarningsToAllKitchensExcept(currentKitchenId, now);

        if (hasGameplayRefs)
        {
            RefreshCurrentKitchenRatesInMemory(currentKitchenId, now);
        }
    }

    private OfflineEarningsReport ApplyOfflineEarningsOnceForAllKitchens()
    {
        if (!enableOfflineEarnings || offlineEarningsAppliedThisSession)
        {
            return null;
        }

        offlineEarningsAppliedThisSession = true;

        if (loadedData == null || loadedData.kitchens == null || loadedData.kitchens.Count == 0)
        {
            return null;
        }

        long now = NowUnixSeconds();
        OfflineEarningsReport report = new OfflineEarningsReport();

        for (int i = 0; i < loadedData.kitchens.Count; i++)
        {
            KitchenSaveData kitchen = loadedData.kitchens[i];

            if (kitchen == null)
                continue;

            // Determine per-kitchen offline cap:
            // - If the saved piggy bank tier has value > 0, use that (seconds).
            // - Otherwise fall back to global maxOfflineEarningSeconds.
            // - If both are 0, effective cap == 0 => NO offline earnings.
            int effectiveMaxSeconds = 0;

            if (upgrades != null && upgrades.piggyBankTiers != null && upgrades.piggyBankTiers.Count > 0)
            {
                int tierIndex = Mathf.Clamp(kitchen.currentPiggyBankIndex, 0, upgrades.piggyBankTiers.Count - 1);
                float tierValue = upgrades.piggyBankTiers[tierIndex].value;
                if (tierValue > 0f)
                {
                    effectiveMaxSeconds = Mathf.FloorToInt(tierValue);
                }
                else
                {
                    // fallback to global cap (0 means none allowed)
                    effectiveMaxSeconds = maxOfflineEarningSeconds;
                }
            }
            else
            {
                effectiveMaxSeconds = maxOfflineEarningSeconds;
            }

            if (logSaveEvents)
            {
                Debug.Log($"[SaveManager] Offline cap for '{kitchen.kitchenId}' (piggy tier {kitchen.currentPiggyBankIndex}): {effectiveMaxSeconds} seconds");
            }

            OfflineKitchenEarnings kitchenResult = ApplyEarningsToKitchen(
                kitchen,
                now,
                effectiveMaxSeconds,
                includeInReport: true
            );

            if (kitchenResult == null)
            {
                continue;
            }

            report.kitchens.Add(kitchenResult);
            report.totalMoneyEarned += kitchenResult.moneyEarned;
            report.totalDishesEarned += kitchenResult.dishesEarned;

            if (kitchenResult.elapsedSeconds > report.elapsedSeconds)
            {
                report.elapsedSeconds = kitchenResult.elapsedSeconds;
            }

            if (kitchenResult.secondsApplied > report.secondsApplied)
            {
                report.secondsApplied = kitchenResult.secondsApplied;
            }
        }

        if (!report.HasAnyEarnings)
        {
            return null;
        }

        WriteSaveFile();
        return report;
    }

    private void TryShowOfflineEarningsReport(OfflineEarningsReport report)
    {
        if (!showOfflineEarningsPanel || report == null)
        {
            return;
        }

        if (report.totalMoneyEarned < minimumOfflineEarningsToShow && report.totalDishesEarned <= 0)
        {
            return;
        }

        OfflineEarningsUI.ShowReport(report);
    }

    private void ApplyBackgroundEarningsToAllKitchensExcept(string skipKitchenId, long now)
    {
        if (loadedData == null || loadedData.kitchens == null)
        {
            return;
        }

        for (int i = 0; i < loadedData.kitchens.Count; i++)
        {
            KitchenSaveData kitchen = loadedData.kitchens[i];

            if (kitchen == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(skipKitchenId) && kitchen.kitchenId == skipKitchenId)
            {
                continue;
            }

            ApplyBackgroundEarningsToKitchen(kitchen, now);
        }
    }

    private void ApplyBackgroundEarningsToKitchen(KitchenSaveData kitchen, long now)
    {
        ApplyEarningsToKitchen(kitchen, now, maxBackgroundEarningSecondsPerTick, includeInReport: false);
    }

    private OfflineKitchenEarnings ApplyEarningsToKitchen(
        KitchenSaveData kitchen,
        long now,
        int maxSeconds,
        bool includeInReport)
    {
        if (kitchen == null)
        {
            return null;
        }

        NormalizeKitchenData(kitchen);

        long last = kitchen.lastBackgroundEarningsUnixSeconds;
        if (last <= 0 || now <= last)
        {
            kitchen.lastBackgroundEarningsUnixSeconds = now;
            return null;
        }

        long elapsedSeconds = now - last;
        long secondsApplied = elapsedSeconds;

        // New semantics:
        // - maxSeconds == 0 => NO offline earnings applied.
        // - maxSeconds > 0 => cap to maxSeconds.
        // - maxSeconds < 0  => negative (unused) -> treat as no cap (apply full elapsed).
        if (maxSeconds == 0)
        {
            secondsApplied = 0;
        }
        else if (maxSeconds > 0)
        {
            secondsApplied = System.Math.Min(secondsApplied, maxSeconds);
        }
        // else maxSeconds < 0 -> no cap

        if (secondsApplied <= 0)
        {
            kitchen.lastBackgroundEarningsUnixSeconds = now;
            if (logSaveEvents && includeInReport)
            {
                Debug.Log($"[SaveManager] No offline earnings applied for '{kitchen.kitchenId}' (elapsed {elapsedSeconds}s, applied {secondsApplied}s).");
            }
            return null;
        }

        float moneyEarned = 0f;
        long dishesEarned = 0L;

        if (kitchen.cachedMoneyPerSecond > 0f)
        {
            moneyEarned = kitchen.cachedMoneyPerSecond * secondsApplied;
            kitchen.totalProfit += moneyEarned;
        }

        if (backgroundEarningsAddDishes && kitchen.cachedDishesPerSecond > 0f)
        {
            double dishProgress = kitchen.backgroundDishFraction + ((double)kitchen.cachedDishesPerSecond * secondsApplied);
            dishesEarned = (long)System.Math.Floor(dishProgress);

            if (dishesEarned > 0)
            {
                kitchen.totalDishes += dishesEarned;
            }

            kitchen.backgroundDishFraction = (float)(dishProgress - dishesEarned);
        }

        kitchen.lastBackgroundEarningsUnixSeconds = now;

        if (logSaveEvents && includeInReport)
        {
            Debug.Log($"[SaveManager] Applied offline for '{kitchen.kitchenId}': elapsed={elapsedSeconds}s, applied={secondsApplied}s, money={moneyEarned}, dishes={dishesEarned}");
        }

        if (!includeInReport || (moneyEarned <= 0f && dishesEarned <= 0L))
        {
            return null;
        }

        return new OfflineKitchenEarnings
        {
            kitchenId = kitchen.kitchenId,
            elapsedSeconds = elapsedSeconds,
            secondsApplied = secondsApplied,
            moneyPerSecond = kitchen.cachedMoneyPerSecond,
            dishesPerSecond = kitchen.cachedDishesPerSecond,
            moneyEarned = moneyEarned,
            dishesEarned = dishesEarned
        };
    }

    private void RefreshCurrentKitchenRatesInMemory(string kitchenId)
    {
        RefreshCurrentKitchenRatesInMemory(kitchenId, NowUnixSeconds());
    }

    private void RefreshCurrentKitchenRatesInMemory(string kitchenId, long now)
    {
        if (string.IsNullOrWhiteSpace(kitchenId) || !HaveGameplayRefs())
        {
            return;
        }

        KitchenSaveData kitchen = GetOrCreateKitchenData(kitchenId);
        kitchen.cachedMoneyPerSecond = score != null ? Mathf.Max(0f, score.GetDisplayedProfitPerSecond()) : 0f;
        kitchen.cachedDishesPerSecond = score != null ? Mathf.Max(0f, score.GetDisplayedDishesPerSecond()) : 0f;
        kitchen.lastBackgroundEarningsUnixSeconds = now;
    }

    private void CaptureCurrentKitchenToData(KitchenSaveData kitchen, long now)
    {
        if (kitchen == null || !HaveGameplayRefs())
        {
            return;
        }

        kitchen.totalDishes = score.GetTotalDishes();
        kitchen.totalProfit = score.GetTotalProfitDouble();
        kitchen.dishCountIncrement = score.GetDishCountIncrement();
        kitchen.profitPerDish = score.GetProfitPerDish();
        kitchen.dishProfitMultiplier = score.dishProfitMultiplier;

        upgrades.GetSaveState(
            out kitchen.currentSoapIndex,
            out kitchen.currentGloveIndex,
            out kitchen.currentSpongeIndex,
            out kitchen.currentPiggyBankIndex,
            out kitchen.radioOwned
        );

        if (mop != null)
        {
            mop.GetSaveState(
                out kitchen.currentMopIndex,
                out kitchen.selectedMopFilterId,
                out kitchen.purchasedMopFilterIds,
                out kitchen.selectedMopStaticColorHtml
            );
        }

        kitchen.employees = employees.GetSaveState();
        kitchen.employeeProfitMultiplier = employees.GetGlobalEmployeeProfitMultiplierForSave();

        if (loans != null)
        {
            kitchen.currentLoanIndex = loans.GetLoanIndexForSave();
        }

        if (sinks != null)
        {
            kitchen.currentSinkType = (int)sinks.CurrentSinkType;
            kitchen.purchasedSinkNodeIds = sinks.GetPurchasedNodeIds();
        }

        // Persist achievements from runtime manager into save data
        if (AchievementManager.Instance != null)
        {
            kitchen.achievedAchievementIds = AchievementManager.Instance.GetUnlockedAchievementIds() ?? new List<string>();
        }

        kitchen.cachedMoneyPerSecond = Mathf.Max(0f, score.GetDisplayedProfitPerSecond());
        kitchen.cachedDishesPerSecond = Mathf.Max(0f, score.GetDisplayedDishesPerSecond());
        kitchen.lastBackgroundEarningsUnixSeconds = now;
    }

    public void Save()
    {
        TryBindRefs();

        if (loadedData == null)
        {
            loadedData = new SaveData();
        }

        loadedData.saveVersion = 6;

        bool hasGameplayRefs = HaveGameplayRefs();
        string kitchenId = hasGameplayRefs ? ResolveCurrentKitchenId() : string.Empty;
        long now = NowUnixSeconds();

        ApplyBackgroundEarningsToAllKitchensExcept(kitchenId, now);

        KitchenSaveData currentKitchen = null;
        if (hasGameplayRefs)
        {
            currentKitchen = GetOrCreateKitchenData(kitchenId);
            CaptureCurrentKitchenToData(currentKitchen, now);
        }

        // Keep legacy fields mirroring kitchen_1 for older debugging tools and one-time fallback.
        KitchenSaveData kitchenOne = FindKitchenData("kitchen_1");
        if (kitchenOne != null)
        {
            CopyKitchenOneToLegacyFields(kitchenOne);
        }
        else if (currentKitchen != null && kitchenId == "kitchen_1")
        {
            CopyKitchenOneToLegacyFields(currentKitchen);
        }

        WriteSaveFile();
    }

    private KitchenSaveData FindKitchenData(string kitchenId)
    {
        if (loadedData == null || loadedData.kitchens == null)
        {
            return null;
        }

        for (int i = 0; i < loadedData.kitchens.Count; i++)
        {
            KitchenSaveData kitchen = loadedData.kitchens[i];
            if (kitchen != null && kitchen.kitchenId == kitchenId)
            {
                return kitchen;
            }
        }

        return null;
    }

    private void WriteSaveFile()
    {
        try
        {
            string json = JsonUtility.ToJson(loadedData, true);
            File.WriteAllText(SavePath, json);
            hasLoadedFile = true;

            if (logSaveEvents)
            {
                Debug.Log($"[SaveManager] Saved to: {SavePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Failed to write save file. {e.Message}");
        }
    }

    private void CopyKitchenOneToLegacyFields(KitchenSaveData kitchen)
    {
        if (loadedData == null || kitchen == null)
        {
            return;
        }

        loadedData.totalDishes = kitchen.totalDishes;
        loadedData.totalProfit = kitchen.totalProfit;
        loadedData.dishCountIncrement = kitchen.dishCountIncrement;
        loadedData.profitPerDish = kitchen.profitPerDish;
        loadedData.dishProfitMultiplier = kitchen.dishProfitMultiplier;
        loadedData.currentSoapIndex = kitchen.currentSoapIndex;
        loadedData.currentGloveIndex = kitchen.currentGloveIndex;
        loadedData.currentSpongeIndex = kitchen.currentSpongeIndex;
        loadedData.currentPiggyBankIndex = kitchen.currentPiggyBankIndex;
        loadedData.currentMopIndex = kitchen.currentMopIndex;
        loadedData.selectedMopFilterId = kitchen.selectedMopFilterId;
        loadedData.selectedMopStaticColorHtml = string.IsNullOrWhiteSpace(kitchen.selectedMopStaticColorHtml) ? "#0000004D" : kitchen.selectedMopStaticColorHtml;
        loadedData.purchasedMopFilterIds = kitchen.purchasedMopFilterIds != null
            ? new List<string>(kitchen.purchasedMopFilterIds)
            : new List<string> { MopUpgradeShelf.CleanFilterId };
        loadedData.radioOwned = kitchen.radioOwned;
        loadedData.employees = kitchen.employees;
        loadedData.employeeProfitMultiplier = kitchen.employeeProfitMultiplier;
        loadedData.currentSinkType = kitchen.currentSinkType;
        loadedData.purchasedSinkNodeIds = kitchen.purchasedSinkNodeIds;
        loadedData.currentLoanIndex = kitchen.currentLoanIndex;
        loadedData.cachedMoneyPerSecond = kitchen.cachedMoneyPerSecond;
        loadedData.cachedDishesPerSecond = kitchen.cachedDishesPerSecond;
        loadedData.lastBackgroundEarningsUnixSeconds = kitchen.lastBackgroundEarningsUnixSeconds;
        loadedData.backgroundDishFraction = kitchen.backgroundDishFraction;
    }

    public void Load()
    {
        ReadSaveFileToMemory();
        hasAppliedToCurrentScene = false;
        TryBindRefs();
        TryApplyLoadedDataToScene();
    }

    public bool TryGetKitchenBusinessStats(
        string kitchenId,
        out float totalProfit,
        out float moneyPerSecond,
        out long totalDishes,
        out float dishesPerSecond)
    {
        totalProfit = 0f;
        moneyPerSecond = 0f;
        totalDishes = 0L;
        dishesPerSecond = 0f;

        if (string.IsNullOrWhiteSpace(kitchenId))
        {
            return false;
        }

        if (loadedData == null)
        {
            loadedData = new SaveData();
        }

        TryBindRefs();

        bool hasGameplayRefs = HaveGameplayRefs();
        string currentKitchenId = hasGameplayRefs ? ResolveCurrentKitchenId() : string.Empty;

        if (hasGameplayRefs && kitchenId == currentKitchenId)
        {
            totalProfit = score != null ? score.GetTotalProfit() : 0f;
            moneyPerSecond = score != null ? Mathf.Max(0f, score.GetDisplayedProfitPerSecond()) : 0f;
            totalDishes = score != null ? score.GetTotalDishes() : 0L;
            dishesPerSecond = score != null ? Mathf.Max(0f, score.GetDisplayedDishesPerSecond()) : 0f;
            RefreshCurrentKitchenRatesInMemory(kitchenId);
            return true;
        }

        KitchenSaveData kitchen = GetOrCreateKitchenData(kitchenId);
        ApplyBackgroundEarningsToKitchen(kitchen, NowUnixSeconds());

        totalProfit = ToSafeFloat(kitchen.totalProfit);
        moneyPerSecond = kitchen.cachedMoneyPerSecond;
        totalDishes = kitchen.totalDishes;
        dishesPerSecond = kitchen.cachedDishesPerSecond;
        return true;
    }

    public void WipeSave()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }

            loadedData = new SaveData();
            hasLoadedFile = true;
            hasAppliedToCurrentScene = false;
            offlineEarningsAppliedThisSession = false;

            KitchenBusinessProgress.ClearAllBusinessProgress();

            if (PlayerPrefs.HasKey(AudioManager.AmbientDisabledKey))
            {
                PlayerPrefs.DeleteKey(AudioManager.AmbientDisabledKey);
                PlayerPrefs.Save();
            }

            if (AudioManager.instance != null)
            {
                AudioManager.instance.EnableAmbientLooping();
            }

            PlayerPrefs.SetInt("HasSeenIntro", 0);
            PlayerPrefs.Save();

            if (logSaveEvents)
            {
                Debug.Log($"[SaveManager] Wiped save file at: {SavePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Failed to delete save file. {e.Message}");
        }
    }
}