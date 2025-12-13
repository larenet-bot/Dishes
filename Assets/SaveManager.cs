using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drop this in your FIRST-loaded scene (your opening menu).
/// Leave the reference fields empty if the managers live in other scenes.
/// This object persists and will re-bind to the managers whenever a new scene loads.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Refs (leave empty if these exist in other scenes)")]
    [SerializeField] private ScoreManager score;
    [SerializeField] private EmployeeManager employees;
    [SerializeField] private Upgrades upgrades;

    [Header("Autosave")]
    [SerializeField] private bool autosave = true;
    [SerializeField] private float autosaveInterval = 20f;

    // Optional: if you want to see the path in editor logs.
    [SerializeField] private bool logSaveEvents = false;

    [SerializeField] private LoanManager loans;

    private float _timer;
    private SaveData _loadedData;
    private bool _hasLoadedFile;
    private bool _hasAppliedToCurrentScene;


    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // If you happened to place managers in the same scene, bind now.
        TryBindRefs();

        // Read save file into memory right away (safe even if managers aren't present yet).
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
        // Try to apply immediately in case the boot scene already contains the managers.
        TryApplyLoadedDataToScene();
    }

    private void Update()
    {
        if (!autosave) return;

        _timer += Time.unscaledDeltaTime;
        if (_timer >= autosaveInterval)
        {
            _timer = 0f;
            Save();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // New scene means new manager instances (if they live in that scene).
        score = null;
        employees = null;
        upgrades = null;
        loans = null;

        _hasAppliedToCurrentScene = false;

        TryBindRefs();
        TryApplyLoadedDataToScene();
    }

    private void TryBindRefs()
    {
        if (score == null) score = FindFirstObjectByType<ScoreManager>();
        if (employees == null) employees = FindFirstObjectByType<EmployeeManager>();
        if (upgrades == null) upgrades = FindFirstObjectByType<Upgrades>();
        if (loans == null) loans = FindFirstObjectByType<LoanManager>();

    }

    private void ReadSaveFileToMemory()
    {
        _hasLoadedFile = false;
        _loadedData = null;

        if (!File.Exists(SavePath))
        {
            if (logSaveEvents) Debug.Log($"[SaveManager] No save file at: {SavePath}");
            return;
        }

        try
        {
            var json = File.ReadAllText(SavePath);
            _loadedData = JsonUtility.FromJson<SaveData>(json);
            _hasLoadedFile = (_loadedData != null);

            if (logSaveEvents) Debug.Log($"[SaveManager] Loaded save file from: {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SaveManager] Failed to read save file. {e.Message}");
            _hasLoadedFile = false;
            _loadedData = null;
        }
    }

    private bool HaveAllRefs()
    {
        return score != null && employees != null && upgrades != null && loans != null;
    }

    private void TryApplyLoadedDataToScene()
    {
        if (_hasAppliedToCurrentScene) return;
        if (!_hasLoadedFile || _loadedData == null) return;

        // Don’t apply until the target managers exist (likely only in gameplay scene).
        if (!HaveAllRefs()) return;

        // ---- Score ----
        score.LoadFromSave(
            _loadedData.totalDishes,
            _loadedData.totalProfit,
            _loadedData.dishCountIncrement,
            _loadedData.dishProfitMultiplier
        );

        // ---- Upgrades ----
        upgrades.ApplySaveState(
            _loadedData.currentSoapIndex,
            _loadedData.currentGloveIndex,
            _loadedData.currentSpongeIndex
        );

        // ---- Employees ----
        employees.ApplySaveState(_loadedData.employees);

        // ---- Loans ----
        loans.ApplyLoanIndexFromSave(_loadedData.currentLoanIndex);

        _hasAppliedToCurrentScene = true;

        if (logSaveEvents) Debug.Log("[SaveManager] Applied loaded save data to current scene managers.");
    }

    public void Save()
    {
        // If we’re in a scene where the managers don’t exist, don’t overwrite the file.
        TryBindRefs();
        if (!HaveAllRefs()) return;

        var data = new SaveData();

        data.currentLoanIndex = loans.GetLoanIndexForSave();

        // ---- Score ----
        data.totalDishes = score.GetTotalDishes();
        data.totalProfit = score.GetTotalProfit();
        data.dishCountIncrement = score.GetDishCountIncrement();
        data.dishProfitMultiplier = score.dishProfitMultiplier;

        // ---- Upgrades ----
        upgrades.GetSaveState(out data.currentSoapIndex, out data.currentGloveIndex, out data.currentSpongeIndex);

        // ---- Employees ----
        data.employees = employees.GetSaveState();

        try
        {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);

            // Keep an in-memory copy so scene transitions can re-apply without re-reading.
            _loadedData = data;
            _hasLoadedFile = true;

            if (logSaveEvents) Debug.Log($"[SaveManager] Saved to: {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SaveManager] Failed to write save file. {e.Message}");
        }
    }

    public void Load()
    {
        // Re-read from disk and apply (if managers exist).
        ReadSaveFileToMemory();
        _hasAppliedToCurrentScene = false;
        TryBindRefs();
        TryApplyLoadedDataToScene();
    }

    public void WipeSave()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            _loadedData = null;
            _hasLoadedFile = false;
            _hasAppliedToCurrentScene = false;

            if (logSaveEvents) Debug.Log($"[SaveManager] Wiped save file at: {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SaveManager] Failed to delete save file. {e.Message}");
        }
    }
}
