using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent save manager with one independent save bucket per kitchen id.
/// Drop this in the first-loaded scene. It persists and re-binds when scenes load.
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

    [Header("Kitchen Id")]
    [Tooltip("Used only if SaveManager cannot find a LoanManager or KitchenIdentity in the active scene.")]
    [SerializeField] private string fallbackKitchenId = "kitchen_1";

    [Header("Autosave")]
    [SerializeField] private bool autosave = true;
    [SerializeField] private float autosaveInterval = 20f;

    [Header("Debug")]
    [SerializeField] private bool logSaveEvents = false;

    private float timer;
    private SaveData loadedData;
    private bool hasLoadedFile;
    private bool hasAppliedToCurrentScene;

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
            loadedData.dishCountIncrement > 1 ||
            loadedData.dishProfitMultiplier > 1f ||
            loadedData.radioOwned ||
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
            dishCountIncrement = Mathf.Max(1, loadedData.dishCountIncrement),
            dishProfitMultiplier = loadedData.dishProfitMultiplier > 0f ? loadedData.dishProfitMultiplier : 1f,
            currentSoapIndex = Mathf.Max(0, loadedData.currentSoapIndex),
            currentGloveIndex = Mathf.Max(0, loadedData.currentGloveIndex),
            currentSpongeIndex = Mathf.Max(0, loadedData.currentSpongeIndex),
            radioOwned = loadedData.radioOwned,
            employees = loadedData.employees ?? new List<EmployeeSave>(),
            employeeProfitMultiplier = loadedData.employeeProfitMultiplier > 0f ? loadedData.employeeProfitMultiplier : 1f,
            currentSinkType = loadedData.currentSinkType,
            purchasedSinkNodeIds = loadedData.purchasedSinkNodeIds ?? new List<string>(),
            currentLoanIndex = loadedData.currentLoanIndex
        };

        loadedData.kitchens.Add(kitchenOne);
        loadedData.saveVersion = 2;

        if (logSaveEvents)
        {
            Debug.Log("[SaveManager] Migrated old single-kitchen save into kitchen_1.");
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
            dishProfitMultiplier = 1f,
            currentSoapIndex = 0,
            currentGloveIndex = 0,
            currentSpongeIndex = 0,
            radioOwned = false,
            employees = new List<EmployeeSave>(),
            employeeProfitMultiplier = 1f,
            currentSinkType = 0,
            purchasedSinkNodeIds = new List<string>(),
            currentLoanIndex = 0
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

        if (kitchen.dishProfitMultiplier <= 0f)
        {
            kitchen.dishProfitMultiplier = 1f;
        }

        if (kitchen.employeeProfitMultiplier <= 0f)
        {
            kitchen.employeeProfitMultiplier = 1f;
        }

        if (kitchen.employees == null)
        {
            kitchen.employees = new List<EmployeeSave>();
        }

        if (kitchen.purchasedSinkNodeIds == null)
        {
            kitchen.purchasedSinkNodeIds = new List<string>();
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
        KitchenSaveData kitchen = GetOrCreateKitchenData(kitchenId);

        score.LoadFromSave(
            kitchen.totalDishes,
            kitchen.totalProfit,
            kitchen.dishCountIncrement,
            kitchen.dishProfitMultiplier
        );

        upgrades.ApplySaveState(
            kitchen.currentSoapIndex,
            kitchen.currentGloveIndex,
            kitchen.currentSpongeIndex,
            kitchen.radioOwned
        );

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

        if (ProfitRate.Instance != null)
        {
            ProfitRate.Instance.ResetBaseline();
        }

        hasAppliedToCurrentScene = true;

        if (logSaveEvents)
        {
            Debug.Log($"[SaveManager] Applied save data for {kitchenId}.");
        }
    }

    public void Save()
    {
        TryBindRefs();

        if (!HaveGameplayRefs())
        {
            return;
        }

        if (loadedData == null)
        {
            loadedData = new SaveData();
        }

        loadedData.saveVersion = 2;

        string kitchenId = ResolveCurrentKitchenId();
        KitchenSaveData kitchen = GetOrCreateKitchenData(kitchenId);

        kitchen.totalDishes = score.GetTotalDishes();
        kitchen.totalProfit = score.GetTotalProfit();
        kitchen.dishCountIncrement = score.GetDishCountIncrement();
        kitchen.dishProfitMultiplier = score.dishProfitMultiplier;

        upgrades.GetSaveState(
            out kitchen.currentSoapIndex,
            out kitchen.currentGloveIndex,
            out kitchen.currentSpongeIndex,
            out kitchen.radioOwned
        );

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

        // Keep legacy fields mirroring kitchen_1 for older debugging tools and one-time fallback.
        if (kitchenId == "kitchen_1")
        {
            CopyKitchenOneToLegacyFields(kitchen);
        }

        try
        {
            string json = JsonUtility.ToJson(loadedData, true);
            File.WriteAllText(SavePath, json);
            hasLoadedFile = true;

            if (logSaveEvents)
            {
                Debug.Log($"[SaveManager] Saved {kitchenId} to: {SavePath}");
            }
        }
        catch (System.Exception e)
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
        loadedData.dishProfitMultiplier = kitchen.dishProfitMultiplier;
        loadedData.currentSoapIndex = kitchen.currentSoapIndex;
        loadedData.currentGloveIndex = kitchen.currentGloveIndex;
        loadedData.currentSpongeIndex = kitchen.currentSpongeIndex;
        loadedData.radioOwned = kitchen.radioOwned;
        loadedData.employees = kitchen.employees;
        loadedData.employeeProfitMultiplier = kitchen.employeeProfitMultiplier;
        loadedData.currentSinkType = kitchen.currentSinkType;
        loadedData.purchasedSinkNodeIds = kitchen.purchasedSinkNodeIds;
        loadedData.currentLoanIndex = kitchen.currentLoanIndex;
    }

    public void Load()
    {
        ReadSaveFileToMemory();
        hasAppliedToCurrentScene = false;
        TryBindRefs();
        TryApplyLoadedDataToScene();
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
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SaveManager] Failed to delete save file. {e.Message}");
        }
    }
}
