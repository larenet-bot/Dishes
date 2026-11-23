using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dishwasher V2. Opens the packing UI, builds independent dirty-sprite dishes,
/// validates polygon placement, and starts the real DishwasherCycleController.
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

    private readonly List<DishwasherDraggableDish> dishes = new List<DishwasherDraggableDish>();
    private DishwasherDraggableDish selectedDish;
    private bool pileGenerated;
    private bool loadLocked;
    private bool hasWiredButtons;
    private DishwasherSaveState pendingRestore;

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
        if (menuRoot != null) menuRoot.SetActive(false);
        if (sinkManager != null)
        {
            sinkManager.OnSinkTypeChanged += OnSinkTypeChanged;
            sinkManager.OnSinkReset += OnSinkReset;
        }
        if (cycleController != null) cycleController.CycleCompleted += HandleCycleCompleted;
    }

    private void Start() => RefreshBranchVisibility();

    private void OnDestroy()
    {
        if (sinkManager != null)
        {
            sinkManager.OnSinkTypeChanged -= OnSinkTypeChanged;
            sinkManager.OnSinkReset -= OnSinkReset;
        }
        if (cycleController != null) cycleController.CycleCompleted -= HandleCycleCompleted;
        if (!hasWiredButtons) return;
        if (openMenuButton != null) openMenuButton.onClick.RemoveListener(OpenMenu);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseMenu);
        if (startCycleButton != null) startCycleButton.onClick.RemoveListener(StartLoad);
        if (resetPrototypeButton != null) resetPrototypeButton.onClick.RemoveListener(ResetPrototype);
    }

    private void WireButtons()
    {
        if (hasWiredButtons) return;
        if (openMenuButton != null) openMenuButton.onClick.AddListener(OpenMenu);
        if (closeButton != null) closeButton.onClick.AddListener(CloseMenu);
        if (startCycleButton != null) startCycleButton.onClick.AddListener(StartLoad);
        if (resetPrototypeButton != null) resetPrototypeButton.onClick.AddListener(ResetPrototype);
        hasWiredButtons = true;
    }

    private void Update()
    {
        if (menuRoot == null || !menuRoot.activeInHierarchy) return;
        if (!loadLocked && selectedDish != null && Input.GetKeyDown(KeyCode.R))
            selectedDish.RotateClockwise();
        if (loadLocked && cycleController != null && cycleController.IsRunning)
        {
            int remaining = Mathf.CeilToInt(cycleController.SecondsRemaining);
            SetStatus($"Washing {cycleController.LoadedDishCount} dishes — {remaining / 60}:{remaining % 60:00} remaining");
        }
    }

    private bool IsAllowedBranch() => !requireDishwasherSink ||
        (sinkManager != null && sinkManager.CurrentSinkType == SinkManager.SinkType.Dishwasher);

    private void OnSinkTypeChanged(SinkManager.SinkType _) => HandleSinkChange();
    private void OnSinkReset() => HandleSinkChange();
    private void HandleSinkChange()
    {
        if (!IsAllowedBranch())
        {
            ClearPile();
            pendingRestore = null;
            CloseMenu();
        }
        RefreshBranchVisibility();
    }

    private void RefreshBranchVisibility()
    {
        bool enabled = IsAllowedBranch();
        if (openMenuButton != null) openMenuButton.gameObject.SetActive(enabled);
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
        if (menuRoot != null && menuRoot.activeInHierarchy) GeneratePile();
        else SetStatus("Load cleared. Open the menu for a new pile.");
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
                DishData data = ResolveDish(dishSpawner, state);
                DishwasherDraggableDish dish = CreateDish(data, i);
                if (dish == null) continue;
                RectTransform targetArea = state.inDishwasher ? dishwasherArea : dirtyPileArea;
                Vector2 local = targetArea.rect.center + new Vector2(
                    state.normalizedX * targetArea.rect.width,
                    state.normalizedY * targetArea.rect.height);
                dish.RectTransform.anchoredPosition = dishLayer.InverseTransformPoint(targetArea.TransformPoint(local));
                dish.ApplyQuarterTurns(state.quarterTurns);
            }
            pileGenerated = dishes.Count > 0;
        }
        else
        {
            pendingRestore = null;
            long total = scoreManager != null ? scoreManager.GetTotalDishes() : 0L;
            Vector2 center = dishLayer.InverseTransformPoint(dirtyPileArea.TransformPoint(dirtyPileArea.rect.center));
            for (int i = 0; i < dishesPerPile; i++)
            {
                DishData data = dishSpawner.GetRandomDish(total);
                DishwasherDraggableDish dish = CreateDish(data, i);
                if (dish == null) continue;
                RectTransform rt = dish.RectTransform;
                float rx = Mathf.Max(0f, (dirtyPileArea.rect.width - rt.rect.width) * 0.5f);
                float ry = Mathf.Max(0f, (dirtyPileArea.rect.height - rt.rect.height) * 0.5f);
                rt.anchoredPosition = center + new Vector2(UnityEngine.Random.Range(-rx, rx), UnityEngine.Random.Range(-ry, ry));
                dish.ApplyQuarterTurns(UnityEngine.Random.Range(0, 4));
            }
            pileGenerated = dishes.Count > 0;
        }

        loadLocked = cycleController != null && cycleController.IsRunning;
        if (loadLocked) LockForRunningCycle();
        else SetStatus("Drag dishes into the dishwasher. Click a dish and press R to rotate.");
        RefreshButtons();
    }

    public void Select(DishwasherDraggableDish dish)
    {
        if (loadLocked && dish != null) return;
        if (selectedDish != null) selectedDish.SetSelected(false);
        selectedDish = dish;
        if (selectedDish != null)
        {
            selectedDish.SetSelected(true);
            string name = string.IsNullOrEmpty(dish.Dish.displayName) ? dish.Dish.name : dish.Dish.displayName;
            SetStatus($"Selected: {name} | R = rotate 90 degrees clockwise");
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
        if (!CenterInside(dish, dirtyPileArea) && !CenterInside(dish, dishwasherArea))
            dish.RectTransform.anchoredPosition = previousPosition;
        RefreshButtons();
    }

    public void NotifyDishMoved() => RefreshButtons();

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
            if (dish != null && CenterInside(dish, dishwasherArea)) loaded.Add(dish);
        return loaded;
    }

    public void StartLoad()
    {
        if (loadLocked || (cycleController != null && cycleController.IsRunning)) return;
        List<DishwasherDraggableDish> loaded = GetLoadedDishes();
        if (loaded.Count == 0) { RefreshButtons(); return; }

        List<DishwasherDraggableDish> invalid = DishwasherPlacementValidator.FindInvalidLoadedDishes(
            loaded, dishwasherArea, dishLayer);
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
        foreach (DishwasherDraggableDish dish in loaded) loadedData.Add(dish.Dish);
        if (!cycleController.TryStartLoadedCycle(loadedData))
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
            bool loaded = CenterInside(dish, dishwasherArea);
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
        if (menuRoot != null && menuRoot.activeInHierarchy)
            GeneratePile();
        SetStatus($"Cycle complete: {cleanedCount} loaded dishes, +{BigNumberFormatter.FormatMoney(earned)}. Ready for a new load.");
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        bool washing = loadLocked || (cycleController != null && cycleController.IsRunning);
        if (startCycleButton != null) startCycleButton.interactable = !washing && GetLoadedDishes().Count > 0;
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
            return pendingRestore != null ? pendingRestore : new DishwasherSaveState();

        var state = new DishwasherSaveState { hasPile = true };
        foreach (DishwasherDraggableDish dish in dishes)
        {
            if (dish == null || dish.Dish == null) continue;
            bool inside = IsStableCenterInside(dish, dishwasherArea);
            RectTransform area = inside ? dishwasherArea : dirtyPileArea;
            Vector3 worldCenter = dishLayer.TransformPoint(dish.StablePosition);
            Vector2 center = area.InverseTransformPoint(worldCenter);
            state.dishes.Add(new DishwasherSavedDish
            {
                dishAssetName = dish.Dish.name,
                fallbackPoolIndex = dishSpawner != null && dishSpawner.allDishes != null ?
                    dishSpawner.allDishes.IndexOf(dish.Dish) : -1,
                inDishwasher = inside,
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
        // Restore the UI only when opened, once RectTransform layout exists.
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
