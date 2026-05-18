using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Definitions")]
    public List<AchievementData> allAchievements = new List<AchievementData>();

    [Header("UI")]
    public GameObject popupPrefab; // assign a prefab with AchievementPopup component

    // unlocked ids are the authoritative runtime state (saved/loaded via SaveManager)
    private HashSet<string> unlockedIds = new HashSet<string>();

    // small polling to catch dish-only changes (ScoreManager exposes OnProfitChanged, but not dish events)
    private long lastKnownDishes = -1;
    private float pollInterval = 1f;
    private float pollTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        ScoreManager.OnProfitChanged += OnScoreChanged;
    }

    private void OnDisable()
    {
        ScoreManager.OnProfitChanged -= OnScoreChanged;
    }

    private void Start()
    {
        // initialize lastKnownDishes
        if (ScoreManager.Instance != null)
            lastKnownDishes = ScoreManager.Instance.GetTotalDishes();
        CheckAllAchievements(silent: true);
    }

    private void Update()
    {
        pollTimer += Time.deltaTime;
        if (pollTimer >= pollInterval)
        {
            pollTimer = 0f;
            PollDishes();
        }
    }

    private void PollDishes()
    {
        if (ScoreManager.Instance == null) return;
        long current = ScoreManager.Instance.GetTotalDishes();
        if (current != lastKnownDishes)
        {
            lastKnownDishes = current;
            CheckAllAchievements();
        }
    }

    private void OnScoreChanged()
    {
        CheckAllAchievements();
    }

    // Called to evaluate all not-yet-unlocked achievements
    public void CheckAllAchievements(bool silent = false)
    {
        if (ScoreManager.Instance == null) return;

        long totalDishes = ScoreManager.Instance.GetTotalDishes();
        float totalProfit = ScoreManager.Instance.GetTotalProfit();
        float profitPerDish = ScoreManager.Instance.GetProfitPerDish();

        int employeeCount = 0;
        var emp = FindFirstObjectByType<EmployeeManager>();
        if (emp != null) employeeCount = emp.GetTotalEmployeeCount(); // implement in EmployeeManager if not present

        foreach (var a in allAchievements)
        {
            if (a == null || string.IsNullOrWhiteSpace(a.id)) continue;
            if (unlockedIds.Contains(a.id)) continue;

            bool meets = false;
            switch (a.trigger)
            {
                case AchievementData.TriggerType.TotalDishes:
                    meets = totalDishes >= (long)a.threshold;
                    break;
                case AchievementData.TriggerType.TotalProfit:
                    meets = totalProfit >= (float)a.threshold;
                    break;
                case AchievementData.TriggerType.EmployeeCount:
                    meets = employeeCount >= (int)a.threshold;
                    break;
                case AchievementData.TriggerType.ProfitPerDish:
                    meets = profitPerDish >= (float)a.threshold;
                    break;
                case AchievementData.TriggerType.Custom:
                    // Custom triggers can be polled by other systems calling UnlockById when needed.
                    meets = false;
                    break;
                case AchievementData.TriggerType.EmployeeTypeCount:
                    {
                        if (!string.IsNullOrWhiteSpace(a.employeeTypeName))
                        {
                            var mgr = FindFirstObjectByType<EmployeeManager>();
                            int countByName = mgr != null ? mgr.GetEmployeeCountByName(a.employeeTypeName) : 0;
                            meets = countByName >= (int)a.threshold;
                        }
                        else
                        {
                            meets = false;
                        }
                    }
                    break;
            }

            if (meets)
            {
                Unlock(a, silent);
            }
        }
    }

    public void UnlockById(string id)
    {
        var a = allAchievements.FirstOrDefault(x => x != null && x.id == id);
        if (a != null && !unlockedIds.Contains(id))
        {
            Unlock(a);
        }
    }

    private void Unlock(AchievementData a, bool silent = false)
    {
        unlockedIds.Add(a.id);
        Debug.Log($"[AchievementManager] Unlocked: {a.id} - {a.title}");

        if (!silent)
        {
            ShowPopup(a);
        }
    }

    private void ShowPopup(AchievementData a)
    {
        if (popupPrefab == null) return;

        // Instantiate prefab (no position/rotation so prefab's UI layout is preserved)
        GameObject go = Instantiate(popupPrefab);

        // If there is a Canvas in scene, parent the popup so screen-space canvases render correctly.
        var sceneCanvas = FindObjectOfType<Canvas>();
        if (sceneCanvas != null)
        {
            go.transform.SetParent(sceneCanvas.transform, worldPositionStays: false);
        }

        // Find AchievementPopup on the root or any child.
        var popup = go.GetComponentInChildren<AchievementPopup>();
        if (popup != null)
        {
            popup.Show(a.title, a.description);
        }
        else
        {
            Debug.LogWarning("[AchievementManager] popupPrefab does not contain an AchievementPopup component.");
        }

        // Safety destroy in case the prefab doesn't self-destroy.
        Destroy(go, 6f);
    }

    // Save / Load helpers used by SaveManager
    public List<string> GetUnlockedAchievementIds()
    {
        return new List<string>(unlockedIds);
    }

    public void LoadFromSave(List<string> ids)
    {
        unlockedIds.Clear();
        if (ids == null) return;
        foreach (var id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id)) unlockedIds.Add(id);
        }
    }

    // Utility to check unlocked status
    public bool IsUnlocked(string id) => unlockedIds.Contains(id);

    // New: expose all achievement definitions (readonly list reference)
    public List<AchievementData> GetAllAchievements()
    {
        return allAchievements;
    }
}