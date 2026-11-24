using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dishwasher V7: optional persistent automatic packing alongside manual four-rack loading.
/// The loading menu has four distinct rectangular placement zones and optional Dishpod.
/// Lives on an ALWAYS-ACTIVE scene object outside the hidden menu root.
/// </summary>
public class DishwasherLoadMenu : MonoBehaviour
{
    [Serializable]
    public class DishSizeOverride
    {
        [Tooltip("Assign a DishData asset; no dependence on its order in DishSpawner.")]
        public DishData dish;
        [Min(0.01f)] public float multiplier = 1f;
    }

    [Header("References")]
    [SerializeField] private SinkManager sinkManager;
    [SerializeField] private DishSpawner dishSpawner;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private DishwasherCycleController cycleController;

    [Header("Existing Menu Layout")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private RectTransform dishwasherArea;
    [SerializeField] private RectTransform dirtyPileArea;
    [SerializeField] private RectTransform dishLayer;

    [Header("Optional Rack Upgrades (UI Images / RectTransforms)")]
    [Tooltip("Keep the original DishwasherArea as the bottom rack. Top Rack is a separate Image, the same width but shorter.")]
    [SerializeField] private RectTransform topRackArea;
    [Tooltip("Clone Top Rack's Image and RectTransform for Double Decker, but give it a distinct position.")]
    [SerializeField] private RectTransform doubleDeckerArea;
    [Tooltip("Clone the Top Rack image again for Triple Decker. It is its own independent loading area.")]
    [SerializeField] private RectTransform tripleDeckerArea;

    [Header("Dishpod Technique (requires your own sprite)")]
    [Tooltip("Assign a sprite imported by you. Nothing will be generated if this is missing.")]
    [SerializeField] private Sprite dishpodSprite;
    [Tooltip("Multiplies dishpod sprite's native width and height. Independent from dish scale.")]
    [SerializeField, Min(0.01f)] private float dishpodSizeMultiplier = 0.25f;

    [Header("Existing Buttons / Feedback")]
    [SerializeField] private Button openMenuButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button startCycleButton;
    [Tooltip("Only for testing: remove this button before release, to prevent rerolling the pile.")]
    [SerializeField] private Button resetPrototypeButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text startButtonLabel;

    [Header("Dish Selection and Size")]
    [SerializeField, Min(1)] private int dishesPerPile = 10;
    [Tooltip("Global multiplier on DishData.uiScale for the dishwasher menu.")]
    [SerializeField, Range(0.01f, 2f)] private float dishwasherSizeMultiplier = 0.3f;
    [Tooltip("Optional per-DishData size multipliers. Unlisted dishes use 1.0.")]
    [SerializeField] private List<DishSizeOverride> dishSizeOverrides = new List<DishSizeOverride>();
    [SerializeField, Range(0f, 24f)] private float selectionPadding = 7f;
    [SerializeField] private bool requireDishwasherSink = true;

    [Header("Auto-sort (HUD control outside the dishwasher menu)")]
    [Tooltip("Assign a Toggle on an always-active HUD Canvas, not inside menuRoot. Leave it unassigned to use manual loading only.")]
    [SerializeField] private Toggle autoSortToggle;
    [Tooltip("Optional HUD container for the Toggle and its label. The controller shows it only while the dishwasher branch is active.")]
    [SerializeField] private GameObject autoSortHudRoot;
    [Tooltip("Only this fraction of each rack's usable width and height is considered. Manual packing can use the entire rack.")]
    [SerializeField, Range(0.5f, 1f)] private float autoSortRackCoverage = 0.9f;
    [Tooltip("Minimum empty space between packed bounding rectangles (UI units). Larger values make automatic packing less efficient.")]
    [SerializeField, Range(0f, 50f)] private float autoSortDishGap = 14f;

    // The menu root may be inactive when a cycle finishes; the parent Canvas stays active.
    public Canvas RewardCanvas => menuRoot != null ? menuRoot.GetComponentInParent<Canvas>() : null;
    public TMP_Text RewardTextStyle => statusText;

    private readonly List<DishwasherDraggableDish> dishes = new List<DishwasherDraggableDish>();
    private DishwasherDraggableDish selectedDish;
    private string lastSelectionPreview;
    private bool pileGenerated;
    private bool loadLocked;
    private bool hasWiredButtons;
    private DishwasherSaveState pendingRestore;
    private bool autoSortEnabled;
    private bool autoPackPending;
    private float nextAutoPackAttempt;

    public bool AutoSortEnabled => autoSortEnabled;

    private void Reset()
    {
        sinkManager = FindAnyObjectByType<SinkManager>();
        dishSpawner = FindAnyObjectByType<DishSpawner>();
        scoreManager = FindAnyObjectByType<ScoreManager>();
        cycleController = FindAnyObjectByType<DishwasherCycleController>();
    }

    private void Awake()
    {
        if (sinkManager == null) sinkManager = FindAnyObjectByType<SinkManager>();
        if (scoreManager == null) scoreManager = ScoreManager.Instance != null ?
            ScoreManager.Instance : FindAnyObjectByType<ScoreManager>();
        if (dishSpawner == null) dishSpawner = scoreManager != null && scoreManager.dishSpawner != null ?
            scoreManager.dishSpawner : FindAnyObjectByType<DishSpawner>();
        if (cycleController == null) cycleController = FindAnyObjectByType<DishwasherCycleController>();
        WireButtons();
        if (autoSortToggle != null) autoSortToggle.SetIsOnWithoutNotify(autoSortEnabled);
        if (menuRoot != null) menuRoot.SetActive(false);
        if (sinkManager != null)
        {
            sinkManager.OnSinkTypeChanged += OnSinkTypeChanged;
            sinkManager.OnSinkReset += OnSinkReset;
            sinkManager.OnNodePurchased += OnNodePurchased;
        }
        if (cycleController != null) cycleController.CycleCompleted += HandleCycleCompleted;
    }

    private void Start()
    {
        RefreshBranchVisibility();
        RefreshRackVisibility();
    }

    private void OnDestroy()
    {
        if (sinkManager != null)
        {
            sinkManager.OnSinkTypeChanged -= OnSinkTypeChanged;
            sinkManager.OnSinkReset -= OnSinkReset;
            sinkManager.OnNodePurchased -= OnNodePurchased;
        }
        if (cycleController != null) cycleController.CycleCompleted -= HandleCycleCompleted;
        if (!hasWiredButtons) return;
        if (openMenuButton != null) openMenuButton.onClick.RemoveListener(OpenMenu);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseMenu);
        if (startCycleButton != null) startCycleButton.onClick.RemoveListener(StartLoad);
        if (resetPrototypeButton != null) resetPrototypeButton.onClick.RemoveListener(ResetPrototype);
        if (autoSortToggle != null) autoSortToggle.onValueChanged.RemoveListener(SetAutoSortEnabled);
    }

    private void WireButtons()
    {
        if (hasWiredButtons) return;
        if (openMenuButton != null) openMenuButton.onClick.AddListener(OpenMenu);
        if (closeButton != null) closeButton.onClick.AddListener(CloseMenu);
        if (startCycleButton != null) startCycleButton.onClick.AddListener(StartLoad);
        if (resetPrototypeButton != null) resetPrototypeButton.onClick.AddListener(ResetPrototype);
        if (autoSortToggle != null) autoSortToggle.onValueChanged.AddListener(SetAutoSortEnabled);
        hasWiredButtons = true;
    }

    private void Update()
    {
        // Auto-sort operates while menuRoot is hidden. A completed cycle is
        // automatically followed by a newly generated pile and a new load.
        TryRunAutoSort();
        if (menuRoot == null || !menuRoot.activeInHierarchy) return;
        if (!loadLocked && selectedDish != null)
        {
            if (Input.GetKeyDown(KeyCode.R)) selectedDish.RotateClockwise();
            UpdateSelectedDishStatus(); // Reflect soap/gloves, Dishpod placement and temporary buffs.
        }
        if (loadLocked && cycleController != null && cycleController.IsRunning && selectedDish == null)
        {
            int remaining = Mathf.CeilToInt(cycleController.SecondsRemaining);
            SetStatus($"Washing {cycleController.LoadedDishCount} dishes — {remaining / 60}:{remaining % 60:00} remaining");
        }
    }

    private bool IsAllowedBranch() => !requireDishwasherSink ||
        (sinkManager != null && sinkManager.CurrentSinkType == SinkManager.SinkType.Dishwasher);

    private void OnSinkTypeChanged(SinkManager.SinkType _) => HandleSinkChange();
    private void OnSinkReset() => HandleSinkChange();

    private void OnNodePurchased(string nodeId)
    {
        if (nodeId == "dw_amount1" || nodeId == "dw_amount2" || nodeId == "dw_amount3")
        {
            // Reveal new racks immediately without unlocking, clearing, or restarting the running load.
            RefreshRackVisibility();
        }
        if (nodeId == "dw_technique" && pileGenerated && !loadLocked &&
            (cycleController == null || !cycleController.IsRunning))
            EnsureDishpodInPile();
        if (autoSortEnabled && (nodeId == "dw_amount1" || nodeId == "dw_amount2" ||
            nodeId == "dw_amount3" || nodeId == "dw_technique")) autoPackPending = true;
        RefreshButtons();
    }

    private RectTransform GetRackArea(int index)
    {
        if (index == 0) return dishwasherArea;
        if (sinkManager == null) return null;
        if (index == 1 && sinkManager.IsPurchased("dw_amount1")) return topRackArea;
        if (index == 2 && sinkManager.IsPurchased("dw_amount2")) return doubleDeckerArea;
        if (index == 3 && sinkManager.IsPurchased("dw_amount3")) return tripleDeckerArea;
        return null;
    }

    private void RefreshRackVisibility()
    {
        if (topRackArea != null)
            topRackArea.gameObject.SetActive(sinkManager != null &&
                sinkManager.CurrentSinkType == SinkManager.SinkType.Dishwasher &&
                sinkManager.IsPurchased("dw_amount1"));
        if (doubleDeckerArea != null)
            doubleDeckerArea.gameObject.SetActive(sinkManager != null &&
                sinkManager.CurrentSinkType == SinkManager.SinkType.Dishwasher &&
                sinkManager.IsPurchased("dw_amount2"));
        if (tripleDeckerArea != null)
            tripleDeckerArea.gameObject.SetActive(sinkManager != null &&
                sinkManager.CurrentSinkType == SinkManager.SinkType.Dishwasher &&
                sinkManager.IsPurchased("dw_amount3"));
    }

    private int GetRackIndex(DishwasherDraggableDish dish, bool stable = false)
    {
        if (dish == null || dishLayer == null) return -1;
        // The UI loading regions must not overlap; if they do, the first wins.
        Vector3 center = stable ? dishLayer.TransformPoint(dish.StablePosition) : dish.RectTransform.position;
        for (int i = 0; i < 4; i++)
        {
            RectTransform rack = GetRackArea(i);
            if (rack != null && rack.rect.Contains(rack.InverseTransformPoint(center))) return i;
        }
        return -1;
    }

    private RectTransform GetRackForDish(DishwasherDraggableDish dish, bool stable = false)
    {
        return GetRackArea(GetRackIndex(dish, stable));
    }
    private void HandleSinkChange()
    {
        if (!IsAllowedBranch())
        {
            ClearPile();
            pendingRestore = null;
            CloseMenu();
        }
        RefreshBranchVisibility();
        RefreshRackVisibility();
        if (autoSortEnabled && IsAllowedBranch()) autoPackPending = true;
    }

    private void RefreshBranchVisibility()
    {
        bool enabled = IsAllowedBranch();
        if (openMenuButton != null) openMenuButton.gameObject.SetActive(enabled);
        if (autoSortHudRoot != null) autoSortHudRoot.SetActive(enabled);
        else if (autoSortToggle != null) autoSortToggle.gameObject.SetActive(enabled);
        if (!enabled && menuRoot != null && menuRoot.activeSelf) CloseMenu();
    }

    public void OpenMenu()
    {
        if (!IsAllowedBranch() || menuRoot == null) return;
        if (dishwasherArea == null || dirtyPileArea == null || dishLayer == null)
        {
            Debug.LogError("[Dishwasher] Assign Dishwasher Area, Dirty Pile Area and Dish Layer.", this);
            return;
        }
        if (MenuManager.Instance != null) MenuManager.Instance.OpenMenu(menuRoot);
        else menuRoot.SetActive(true);
        Canvas.ForceUpdateCanvases();
        RefreshRackVisibility();
        if (!pileGenerated) GeneratePile();
        RefreshButtons();
    }

    public void CloseMenu()
    {
        if (menuRoot == null || !menuRoot.activeSelf) return;
        if (MenuManager.Instance != null) MenuManager.Instance.CloseMenu(menuRoot);
        else menuRoot.SetActive(false);
    }

    public void ResetPrototype()
    {
        if (cycleController != null && cycleController.IsRunning)
        {
            SetStatus("Cannot reset a load while it is washing.");
            return;
        }
        ClearPile();
        pendingRestore = null;
        if ((menuRoot != null && menuRoot.activeInHierarchy) || autoSortEnabled) GeneratePile();
        else SetStatus("Load cleared. Open the menu for a new pile.");
        autoPackPending = autoSortEnabled;
    }

    private void ClearPile()
    {
        Select(null);
        foreach (DishwasherDraggableDish dish in dishes)
        {
            if (dish == null) continue;
            dish.gameObject.SetActive(false);
            Destroy(dish.gameObject);
        }
        dishes.Clear();
        pileGenerated = false;
        loadLocked = false;
    }

    private float GetMultiplier(DishData data)
    {
        float custom = 1f;
        if (dishSizeOverrides != null)
        {
            foreach (DishSizeOverride entry in dishSizeOverrides)
                if (entry != null && entry.dish == data) { custom = Mathf.Max(0.01f, entry.multiplier); break; }
        }
        return Mathf.Max(0.01f, dishwasherSizeMultiplier) * custom;
    }

    private DishwasherDraggableDish CreateDish(DishData data, int index)
    {
        if (data == null || data.stageSprites == null || data.stageSprites.Length == 0 || data.stageSprites[0] == null)
            return null;

        GameObject go = new GameObject($"DirtyDish_{index}_{data.name}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(DishwasherDraggableDish));
        go.transform.SetParent(dishLayer, false);
        DishwasherDraggableDish draggable = go.GetComponent<DishwasherDraggableDish>();
        draggable.SelectionPadding = selectionPadding;
        draggable.Initialize(this, data, GetMultiplier(data));
        dishes.Add(draggable);
        return draggable;
    }

    private DishwasherDraggableDish CreateDishpod(int index)
    {
        if (dishpodSprite == null)
        {
            Debug.LogWarning("[Dishwasher] Assign your Dishpod Sprite in DishwasherLoadMenu to enable the technique.", this);
            return null;
        }
        GameObject go = new GameObject($"Dishpod_{index}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(DishwasherDraggableDish));
        go.transform.SetParent(dishLayer, false);
        DishwasherDraggableDish pod = go.GetComponent<DishwasherDraggableDish>();
        pod.SelectionPadding = selectionPadding;
        pod.InitializeDishpod(this, dishpodSprite, dishpodSizeMultiplier);
        dishes.Add(pod);
        return pod;
    }

    private void PlaceInStaging(DishwasherDraggableDish dish)
    {
        if (dish == null) return;
        RectTransform rt = dish.RectTransform;
        Vector2 center = dishLayer.InverseTransformPoint(
            dirtyPileArea.TransformPoint(dirtyPileArea.rect.center));
        float rx = Mathf.Max(0f, (dirtyPileArea.rect.width - rt.rect.width) * 0.5f);
        float ry = Mathf.Max(0f, (dirtyPileArea.rect.height - rt.rect.height) * 0.5f);
        rt.anchoredPosition = center + new Vector2(
            UnityEngine.Random.Range(-rx, rx), UnityEngine.Random.Range(-ry, ry));
        dish.ApplyQuarterTurns(UnityEngine.Random.Range(0, 4));
    }

    private void EnsureDishpodInPile()
    {
        if (sinkManager == null || !sinkManager.HasDishpodTechnique()) return;
        foreach (DishwasherDraggableDish dish in dishes)
            if (dish != null && dish.IsDishpod) return;
        DishwasherDraggableDish pod = CreateDishpod(dishes.Count);
        if (pod != null) PlaceInStaging(pod);
    }

    private void GeneratePile()
    {
        if (dishSpawner == null || dirtyPileArea == null || dishLayer == null)
        {
            Debug.LogError("[Dishwasher] DishSpawner or UI references are missing.", this);
            SetStatus("DishSpawner or UI references missing.");
            return;
        }

        if (pendingRestore != null && pendingRestore.hasPile && pendingRestore.dishes != null &&
            pendingRestore.dishes.Count > 0)
        {
            DishwasherSaveState restore = pendingRestore;
            pendingRestore = null;
            for (int i = 0; i < restore.dishes.Count; i++)
            {
                DishwasherSavedDish state = restore.dishes[i];
                DishwasherDraggableDish dish = state.isDishpod
                    ? (sinkManager != null && sinkManager.HasDishpodTechnique() ? CreateDishpod(i) : null)
                    : CreateDish(ResolveDish(dishSpawner, state), i);
                if (dish == null) continue;
                RectTransform savedRack = state.inDishwasher ? GetRackArea(state.rackIndex) : null;
                RectTransform targetArea = savedRack != null ? savedRack : dirtyPileArea;
                Vector2 local = targetArea.rect.center + new Vector2(
                    state.normalizedX * targetArea.rect.width,
                    state.normalizedY * targetArea.rect.height);
                dish.RectTransform.anchoredPosition = dishLayer.InverseTransformPoint(targetArea.TransformPoint(local));
                dish.ApplyQuarterTurns(state.quarterTurns);
            }
            pileGenerated = dishes.Count > 0;
            if (!loadLocked && (cycleController == null || !cycleController.IsRunning)) EnsureDishpodInPile();
        }
        else
        {
            pendingRestore = null;
            long total = scoreManager != null ? scoreManager.GetTotalDishes() : 0L;
            for (int i = 0; i < dishesPerPile; i++)
            {
                DishData data = dishSpawner.GetRandomDish(total);
                DishwasherDraggableDish dish = CreateDish(data, i);
                if (dish == null) continue;
                PlaceInStaging(dish);
            }
            EnsureDishpodInPile();
            pileGenerated = dishes.Count > 0;
        }

        loadLocked = cycleController != null && cycleController.IsRunning;
        if (loadLocked) LockForRunningCycle();
        else SetStatus("Select a dish to see its current reward.");
        if (autoSortEnabled && !loadLocked) autoPackPending = true;
        RefreshButtons();
    }

    public void Select(DishwasherDraggableDish dish)
    {
        if (loadLocked && dish != null) return;
        if (selectedDish != null) selectedDish.SetSelected(false);
        selectedDish = dish;
        lastSelectionPreview = null;
        if (selectedDish == null) return;
        selectedDish.SetSelected(true);
        UpdateSelectedDishStatus();
    }

    private bool HasDishpodInRack()
    {
        foreach (DishwasherDraggableDish dish in dishes)
        {
            if (dish != null && dish.IsDishpod && GetRackIndex(dish) >= 0)
                return true;
        }
        return false;
    }

    private void UpdateSelectedDishStatus()
    {
        if (selectedDish == null) return;

        string message;
        if (selectedDish.IsDishpod)
        {
            float bonus = cycleController != null ?
                (cycleController.DishpodProfitMultiplier - 1f) * 100f : 25f;
            message = $"Dishpod: +{bonus:0.#}% profit on loaded dishes this cycle.";
        }
        else
        {
            DishData data = selectedDish.Dish;
            if (data == null) return;
            string displayName = string.IsNullOrWhiteSpace(data.displayName) ? data.name : data.displayName;
            long dishCount = cycleController != null ? cycleController.DishesAwardedPerLoadedDish :
                (scoreManager != null ? Math.Max(1L, (long)scoreManager.GetDishCountIncrement()) : 1L);
            bool hasPod = HasDishpodInRack();
            double reward = cycleController != null ? cycleController.PreviewDishReward(data, hasPod) :
                (scoreManager != null ? (double)data.profitPerDish * dishCount *
                    scoreManager.GetEffectiveDishProfitMultiplier() : (double)data.profitPerDish);
            string dishCountLabel = dishCount > 1 ? $" ({dishCount} dishes with gloves)" : "";
            string buffLabel = scoreManager != null && scoreManager.IsWellMotivatedActive ?
                " [Well Motivated active]" : "";
            message = $"{displayName}: {BigNumberFormatter.FormatMoney(reward)} current{dishCountLabel}{buffLabel}";
        }

        // Do not overwrite placement-error messages on every Update. Refresh
        // the preview only when a relevant multiplier or selection changes.
        if (message != lastSelectionPreview)
        {
            lastSelectionPreview = message;
            SetStatus(message);
        }
    }

    public bool TryGetLayerPoint(Vector2 screen, Camera camera, out Vector2 point)
    {
        point = Vector2.zero;
        return dishLayer != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dishLayer, screen, camera, out point);
    }

    public void FinishDrag(DishwasherDraggableDish dish, Vector2 previousPosition)
    {
        if (dish == null || loadLocked) return;
        if (!CenterInside(dish, dirtyPileArea) && GetRackIndex(dish) < 0)
            dish.RectTransform.anchoredPosition = previousPosition;
        RefreshButtons();
    }

    public void NotifyDishMoved()
    {
        RefreshButtons();
        UpdateSelectedDishStatus();
    }

    private static bool CenterInside(DishwasherDraggableDish dish, RectTransform area)
    {
        if (dish == null || area == null) return false;
        Vector2 center = area.InverseTransformPoint(dish.RectTransform.position);
        return area.rect.Contains(center);
    }

    private List<DishwasherDraggableDish> GetLoadedDishes()
    {
        var loaded = new List<DishwasherDraggableDish>();
        foreach (DishwasherDraggableDish dish in dishes)
            if (dish != null && GetRackIndex(dish) >= 0) loaded.Add(dish);
        return loaded;
    }

    public void StartLoad()
    {
        if (loadLocked || (cycleController != null && cycleController.IsRunning)) return;
        List<DishwasherDraggableDish> loaded = GetLoadedDishes();
        if (!loaded.Exists(d => !d.IsDishpod)) { RefreshButtons(); return; }

        var rackMap = new Dictionary<DishwasherDraggableDish, RectTransform>();
        foreach (DishwasherDraggableDish dish in loaded) rackMap[dish] = GetRackForDish(dish);
        List<DishwasherDraggableDish> invalid = DishwasherPlacementValidator.FindInvalidLoadedDishes(
            loaded, rackMap, dishLayer);
        if (invalid.Count > 0)
        {
            foreach (DishwasherDraggableDish dish in invalid) dish.Shake();
            SetStatus($"Cannot start: {invalid.Count} dish(es) overlap or extend outside the dishwasher.");
            return;
        }

        if (cycleController == null)
        {
            SetStatus("DishwasherCycleController reference is missing.");
            Debug.LogError("[Dishwasher] Assign the existing DishwasherCycleController to the load menu.", this);
            return;
        }

        List<DishData> loadedData = new List<DishData>(loaded.Count);
        bool hasDishpod = false;
        foreach (DishwasherDraggableDish dish in loaded)
        {
            if (dish.IsDishpod) hasDishpod = true;
            else if (dish.Dish != null) loadedData.Add(dish.Dish);
        }
        if (!cycleController.TryStartLoadedCycle(loadedData, hasDishpod))
        {
            SetStatus("Could not start the dishwasher cycle. Check the controller and branch unlock.");
            return;
        }

        loadLocked = true;
        LockForRunningCycle();
        RefreshButtons();
    }

    private void LockForRunningCycle()
    {
        Select(null);
        foreach (DishwasherDraggableDish dish in dishes)
        {
            bool loaded = GetRackIndex(dish) >= 0;
            dish.Locked = true;
            dish.SetWashingAnimation(loaded);
            // Hide leftover staging dishes until the wash completes.
            dish.gameObject.SetActive(loaded);
        }
    }

    private void HandleCycleCompleted(int cleanedCount, double earned)
    {
        ClearPile();
        pendingRestore = null;
        if ((menuRoot != null && menuRoot.activeInHierarchy) || autoSortEnabled)
            GeneratePile();
        // Start the next automatic cycle in Update, not inside CycleCompleted.
        // This keeps the controller's completion event and payout orderly.
        autoPackPending = autoSortEnabled;
        SetStatus($"Cycle complete: {cleanedCount} loaded dishes, +{BigNumberFormatter.FormatMoney(earned)}. Ready for a new load.");
        RefreshButtons();
    }

    public void SetAutoSortEnabled(bool enabled)
    {
        autoSortEnabled = enabled;
        if (autoSortToggle != null && autoSortToggle.isOn != enabled)
            autoSortToggle.SetIsOnWithoutNotify(enabled);

        autoPackPending = enabled;
        nextAutoPackAttempt = 0f;
        if (menuRoot != null && menuRoot.activeInHierarchy)
            SetStatus(enabled ? "Auto-sort enabled. The next load will start automatically." :
                "Auto-sort disabled. The current wash will finish; future loads are manual.");
    }

    private void TryRunAutoSort()
    {
        if (!autoSortEnabled || !autoPackPending || !IsAllowedBranch() ||
            cycleController == null || cycleController.IsRunning || loadLocked ||
            Time.unscaledTime < nextAutoPackAttempt) return;

        if (dishLayer == null || dirtyPileArea == null || dishwasherArea == null || dishSpawner == null)
            return;

        // Ensure UI transforms have been evaluated even when the menu is closed.
        Canvas.ForceUpdateCanvases();
        dishwasherArea.ForceUpdateRectTransforms();
        dirtyPileArea.ForceUpdateRectTransforms();
        dishLayer.ForceUpdateRectTransforms();
        if (dishwasherArea.rect.width < 1f || dishwasherArea.rect.height < 1f)
        {
            nextAutoPackAttempt = Time.unscaledTime + 0.5f;
            return;
        }

        if (!pileGenerated) GeneratePile();
        if (!pileGenerated) return;

        // Pack at most once for a given pile. Failed loads remain available to
        // the player, avoiding a zero-duration reroll or CPU-heavy retry loop.
        autoPackPending = false;
        Select(null);
        foreach (DishwasherDraggableDish dish in dishes)
            if (dish != null) PlaceInStaging(dish);

        var racks = new List<RectTransform>(4);
        for (int i = 0; i < 4; i++)
        {
            RectTransform rack = GetRackArea(i);
            if (rack != null) racks.Add(rack);
        }
        DishwasherAutoSorter.Pack(dishes, dishLayer, racks,
            autoSortRackCoverage, autoSortDishGap);

        List<DishwasherDraggableDish> loaded = GetLoadedDishes();
        var rackMap = new Dictionary<DishwasherDraggableDish, RectTransform>();
        foreach (DishwasherDraggableDish dish in loaded) rackMap[dish] = GetRackForDish(dish);
        foreach (DishwasherDraggableDish bad in DishwasherPlacementValidator.FindInvalidLoadedDishes(
            loaded, rackMap, dishLayer)) PlaceInStaging(bad);

        if (GetLoadedDishes().Exists(d => !d.IsDishpod))
            StartLoad();
        else
        {
            SetStatus("Auto-sort found no dishes that fit. Place dishes manually or unlock another rack.");
            Debug.LogWarning("[Dishwasher] Auto-sort couldn't fit a regular dish in any unlocked rack.", this);
        }
    }

    private void RefreshButtons()
    {
        bool washing = loadLocked || (cycleController != null && cycleController.IsRunning);
        if (startCycleButton != null) startCycleButton.interactable = !washing &&
            GetLoadedDishes().Exists(d => !d.IsDishpod);
        if (startButtonLabel != null) startButtonLabel.text = washing ? "Washing" : "Start Cycle";
        if (resetPrototypeButton != null) resetPrototypeButton.interactable = !washing;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    public DishwasherSaveState GetSaveState()
    {
        if (!pileGenerated)
        {
            DishwasherSaveState empty = pendingRestore ?? new DishwasherSaveState();
            empty.autoSortEnabled = autoSortEnabled;
            return empty;
        }

        var state = new DishwasherSaveState { hasPile = true, autoSortEnabled = autoSortEnabled };
        foreach (DishwasherDraggableDish dish in dishes)
        {
            if (dish == null || (!dish.IsDishpod && dish.Dish == null)) continue;
            int rackIndex = GetRackIndex(dish, true);
            bool inside = rackIndex >= 0;
            RectTransform area = inside ? GetRackArea(rackIndex) : dirtyPileArea;
            Vector3 worldCenter = dishLayer.TransformPoint(dish.StablePosition);
            Vector2 center = area.InverseTransformPoint(worldCenter);
            state.dishes.Add(new DishwasherSavedDish
            {
                dishAssetName = dish.IsDishpod ? "" : dish.Dish.name,
                fallbackPoolIndex = !dish.IsDishpod && dishSpawner != null && dishSpawner.allDishes != null ?
                    dishSpawner.allDishes.IndexOf(dish.Dish) : -1,
                isDishpod = dish.IsDishpod,
                inDishwasher = inside,
                rackIndex = inside ? rackIndex : -1,
                normalizedX = (center.x - area.rect.center.x) / Mathf.Max(0.01f, area.rect.width),
                normalizedY = (center.y - area.rect.center.y) / Mathf.Max(0.01f, area.rect.height),
                quarterTurns = dish.GetQuarterTurns()
            });
        }
        return state;
    }

    private bool IsStableCenterInside(DishwasherDraggableDish dish, RectTransform area)
    {
        if (dish == null || area == null || dishLayer == null) return false;
        return area.rect.Contains(area.InverseTransformPoint(dishLayer.TransformPoint(dish.StablePosition)));
    }

    public void ApplySaveState(DishwasherSaveState state)
    {
        ClearPile();
        pendingRestore = state;
        autoSortEnabled = state != null && state.autoSortEnabled;
        autoPackPending = autoSortEnabled;
        nextAutoPackAttempt = 0f;
        if (autoSortToggle != null) autoSortToggle.SetIsOnWithoutNotify(autoSortEnabled);
        // Restore the UI only when opened, once RectTransform layout exists.
        // Automatic mode will reconstruct the saved pile while the menu stays hidden.
        loadLocked = state != null && state.cycleRunning;
        RefreshButtons();
    }

    public static DishData ResolveDish(DishSpawner spawner, DishwasherSavedDish saved)
    {
        if (spawner == null || saved == null || spawner.allDishes == null) return null;
        if (!string.IsNullOrEmpty(saved.dishAssetName))
        {
            foreach (DishData dish in spawner.allDishes)
                if (dish != null && dish.name == saved.dishAssetName) return dish;
        }
        int i = saved.fallbackPoolIndex;
        return i >= 0 && i < spawner.allDishes.Count ? spawner.allDishes[i] : null;
    }
}
