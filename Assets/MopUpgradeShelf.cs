using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shelf-style upgrade controller for the Mop.
/// Kept separate from Upgrades.cs so visual-filter progression can grow without making the main upgrade file larger.
/// Purchases still route through ScoreManager.SubtractProfit(..., isPurchase: true).
///
/// Flow:
/// - Tier 0: dirty grease filter.
/// - Tier 1: clean filter.
/// - Final tier: unlocks a prefab-driven filter selector/shop.
/// </summary>
public class MopUpgradeShelf : MonoBehaviour
{
    public const string CleanFilterId = "clean";
    public const string DirtyGreaseFilterId = "dirty_grease";
    public const string StaticColorFilterId = "static_color";

    [Serializable]
    public class MopTier
    {
        public string tierName;
        [TextArea] public string description;
        [TextArea] public string loreDescription;

        [Tooltip("Cost to buy this tier. The first/base tier should usually cost 0.")]
        public float cost = 0f;

        [Tooltip("Total completed dishes required before this tier can be purchased.")]
        public int requiredDishes = 0;

        public Sprite icon;

        [Tooltip("Filter preset applied when this tier is active. The final tier usually stays Clean because it unlocks the filter selector.")]
        public GameFilterOverlay.FilterPreset filterPreset = GameFilterOverlay.FilterPreset.DirtyGrease;
    }

    [Serializable]
    public class FilterOption
    {
        [Tooltip("Stable save id. Do not rename after release unless you also migrate old saves.")]
        public string filterId = CleanFilterId;

        public string displayName = "Clean";
        [TextArea] public string description = "No extra filter.";
        public float cost = 0f;
        public bool startsPurchased = false;
        public Sprite icon;
        public GameFilterOverlay.FilterPreset preset = GameFilterOverlay.FilterPreset.Clean;
    }

    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private GameFilterOverlay gameFilterOverlay;

    [Header("Mop Tiers")]
    [SerializeField] private List<MopTier> mopTiers = new List<MopTier>();

    [Header("Mop Upgrade UI")]
    [SerializeField] private GameObject mopMenuPanel;
    [SerializeField] private TMP_Text mopNameText;
    [SerializeField] private TMP_Text mopDescText;
    [SerializeField] private TMP_Text mopCostText;
    [SerializeField] private Button mopUpgradeButton;
    [SerializeField] private Button mopCloseButton;

    [Header("Filter Selection UI")]
    [SerializeField] private GameObject filterMenuPanel;
    [SerializeField] private Button filterCloseButton;
    [SerializeField] private RectTransform filterContentParent;
    [SerializeField] private MopFilterCardUI filterCardPrefab;
    [SerializeField] private ToggleGroup filterToggleGroup;

    [Tooltip("When true, this adds/repairs VerticalLayoutGroup, ContentSizeFitter, and ToggleGroup on the Content object.")]
    [SerializeField] private bool autoConfigureFilterContentLayout = true;

    [Header("Filter Options")]
    [SerializeField] private List<FilterOption> filterOptions = new List<FilterOption>();

    [Header("Static Color Filter")]
    [Tooltip("The currently selected color for the Static Color filter. Saved per kitchen through SaveManager.")]
    [SerializeField] private Color selectedStaticColor = new Color(0f, 0f, 0f, 0.30f);

    [Tooltip("Player-selectable color swatches for the Static Color filter. These get spawned on the Static Color card if its swatch parent/prefab are assigned.")]
    [SerializeField]
    private List<Color> staticColorPalette = new List<Color>
    {
        new Color(0f, 0f, 0f, 0.30f),
        new Color(0.55f, 0.12f, 0.12f, 0.28f),
        new Color(0.90f, 0.42f, 0.05f, 0.24f),
        new Color(0.08f, 0.42f, 0.34f, 0.26f),
        new Color(0.06f, 0.16f, 0.55f, 0.26f),
        new Color(0.38f, 0.08f, 0.55f, 0.26f),
        new Color(1f, 1f, 1f, 0.18f)
    };

    [Header("HUD Button Image")]
    [SerializeField] private Image mopButtonImage;

    [Header("Optional Full-Screen Transparent Button Behind Panel")]
    [SerializeField] private Button backgroundOverlayButton;

    [Header("Progress")]
    [SerializeField] private int currentMopIndex = 0;
    [SerializeField] private string selectedFilterId = CleanFilterId;

    [Tooltip("Use only for quick test scenes without SaveManager. For real kitchen saves, leave false so each kitchen stays independent through SaveManager.")]
    [SerializeField] private bool usePlayerPrefsPersistence = false;
    [SerializeField] private string prefsKey = "MopUpgradeTierIndex";
    [SerializeField] private string selectedFilterPrefsKey = "MopSelectedFilterId";
    [SerializeField] private string purchasedFiltersPrefsKey = "MopPurchasedFilterIds";
    [SerializeField] private string selectedStaticColorPrefsKey = "MopSelectedStaticColor";

    private readonly HashSet<string> purchasedFilterIds = new HashSet<string>();
    private readonly List<MopFilterCardUI> spawnedFilterCards = new List<MopFilterCardUI>();
    private readonly List<FilterOption> spawnedFilterOptions = new List<FilterOption>();
    private bool filterCardsBuilt;

    private void Reset()
    {
        scoreManager = FindFirstObjectByType<ScoreManager>();
        gameFilterOverlay = FindFirstObjectByType<GameFilterOverlay>();
    }

    private void Awake()
    {
        CacheReferences();
        EnsureDefaultTiers();
        EnsureDefaultFilterOptions();
        LoadProgress();
        ClampCurrentIndex();
        EnsureDefaultPurchasedFilters();
        ValidateSelectedFilter();
        WireButtons();
        ApplyCurrentFilter();
        CloseMopMenu();
    }

    private void OnEnable()
    {
        ScoreManager.OnProfitChanged += RefreshOpenUI;
        RefreshOpenUI();
    }

    private void OnDisable()
    {
        ScoreManager.OnProfitChanged -= RefreshOpenUI;
    }

    private void Update()
    {
        if (mopMenuPanel != null && mopMenuPanel.activeSelf)
        {
            UpdateMopMenuUI();
        }

        if (filterMenuPanel != null && filterMenuPanel.activeSelf)
        {
            RefreshFilterCards();
        }
    }

    private void CacheReferences()
    {
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }

        if (gameFilterOverlay == null)
        {
            gameFilterOverlay = GameFilterOverlay.Instance != null
                ? GameFilterOverlay.Instance
                : FindFirstObjectByType<GameFilterOverlay>();
        }
    }

    private void EnsureDefaultTiers()
    {
        if (mopTiers == null)
        {
            mopTiers = new List<MopTier>();
        }

        if (mopTiers.Count == 0)
        {
            mopTiers.Add(new MopTier
            {
                tierName = "Greasy Floor",
                description = "The kitchen is still filmed through fryer haze and old dishwater.",
                loreDescription = "Default filter: Dirty grease haze.",
                cost = 0f,
                requiredDishes = 0,
                icon = null,
                filterPreset = GameFilterOverlay.FilterPreset.DirtyGrease
            });
        }

        if (mopTiers.Count == 1)
        {
            mopTiers.Add(new MopTier
            {
                tierName = "Mop",
                description = "Cuts through the grease haze and makes the kitchen look clean.",
                loreDescription = "Removes the dirty grease filter.",
                cost = 250f,
                requiredDishes = 0,
                icon = null,
                filterPreset = GameFilterOverlay.FilterPreset.Clean
            });
        }

        if (mopTiers.Count == 2)
        {
            // If this is the old default Mop tier, make it the clean tier for the new flow.
            if (mopTiers[1] != null && mopTiers[1].tierName == "Mop")
            {
                mopTiers[1].filterPreset = GameFilterOverlay.FilterPreset.Clean;
            }

            mopTiers.Add(new MopTier
            {
                tierName = "Filter Caddy",
                description = "Unlocks a shelf of visual filters for the kitchen.",
                loreDescription = "Once owned, the Mop button opens the filter selector.",
                cost = 750f,
                requiredDishes = 0,
                icon = null,
                filterPreset = GameFilterOverlay.FilterPreset.Clean
            });
        }
    }

    private void EnsureDefaultFilterOptions()
    {
        if (filterOptions == null)
        {
            filterOptions = new List<FilterOption>();
        }

        if (filterOptions.Count > 0)
        {
            return;
        }

        filterOptions.Add(new FilterOption
        {
            filterId = CleanFilterId,
            displayName = "Clean",
            description = "No extra grime or color treatment. Just the cleaned-up kitchen.",
            cost = 0f,
            startsPurchased = true,
            preset = GameFilterOverlay.FilterPreset.Clean
        });

        filterOptions.Add(new FilterOption
        {
            filterId = DirtyGreaseFilterId,
            displayName = "Greasy Memory",
            description = "Brings back the brown edge grime and old-kitchen haze.",
            cost = 500f,
            startsPurchased = false,
            preset = GameFilterOverlay.FilterPreset.DirtyGrease
        });

        filterOptions.Add(new FilterOption
        {
            filterId = "late_shift_neon",
            displayName = "Late Shift Neon",
            description = "Dark teal and pink glow with a soft sign flicker.",
            cost = 750f,
            startsPurchased = false,
            preset = GameFilterOverlay.FilterPreset.LateShiftNeon
        });

        filterOptions.Add(new FilterOption
        {
            filterId = "pulsing_violet_crt",
            displayName = "Pulsing Violet CRT",
            description = "Violet arcade wash, scanlines, light grain, and slow pulse.",
            cost = 1000f,
            startsPurchased = false,
            preset = GameFilterOverlay.FilterPreset.PulsingVioletCRT
        });

        filterOptions.Add(new FilterOption
        {
            filterId = "noir",
            displayName = "Noir",
            description = "Dark vignette, muted overlay, and a harsher old-film mood.",
            cost = 1250f,
            startsPurchased = false,
            preset = GameFilterOverlay.FilterPreset.Noir
        });

        filterOptions.Add(new FilterOption
        {
            filterId = "heat_lamp",
            displayName = "Heat Lamp",
            description = "Warm red-orange food-service glow with slow pulsing edges.",
            cost = 1500f,
            startsPurchased = false,
            preset = GameFilterOverlay.FilterPreset.HeatLamp
        });

        filterOptions.Add(new FilterOption
        {
            filterId = "steam_fog",
            displayName = "Steam Fog",
            description = "Pale drifting haze, soft stains, and a cooler steamed-glass feel.",
            cost = 1750f,
            startsPurchased = false,
            preset = GameFilterOverlay.FilterPreset.SteamFog
        });

        filterOptions.Add(new FilterOption
        {
            filterId = "arcade_scanlines",
            displayName = "Arcade Scanlines",
            description = "Low-lit blue arcade overlay with stronger scanlines and mild flicker.",
            cost = 2000f,
            startsPurchased = false,
            preset = GameFilterOverlay.FilterPreset.ArcadeScanlines
        });

        filterOptions.Add(new FilterOption
        {
            filterId = "dishwasher_dream",
            displayName = "Dishwasher Dream",
            description = "Cool blue-white glow, water spots, and a soft washed-out shimmer.",
            cost = 2500f,
            startsPurchased = false,
            preset = GameFilterOverlay.FilterPreset.DishwasherDream
        });

        filterOptions.Add(new FilterOption
        {
            filterId = StaticColorFilterId,
            displayName = "Static Color",
            description = "A plain color overlay. Pick a swatch after buying it.",
            cost = 500f,
            startsPurchased = false,
            preset = GameFilterOverlay.FilterPreset.StaticColor
        });
    }

    private void WireButtons()
    {
        if (mopUpgradeButton != null)
        {
            mopUpgradeButton.onClick.RemoveAllListeners();
            mopUpgradeButton.onClick.AddListener(OnMopUpgradeButton);
        }

        if (mopCloseButton != null)
        {
            mopCloseButton.onClick.RemoveAllListeners();
            mopCloseButton.onClick.AddListener(CloseMopMenu);
        }

        if (filterCloseButton != null)
        {
            filterCloseButton.onClick.RemoveAllListeners();
            filterCloseButton.onClick.AddListener(CloseMopMenu);
        }

        if (backgroundOverlayButton != null)
        {
            backgroundOverlayButton.onClick.RemoveAllListeners();
            backgroundOverlayButton.onClick.AddListener(CloseMopMenu);

            if (backgroundOverlayButton.gameObject.activeSelf)
            {
                backgroundOverlayButton.gameObject.SetActive(false);
            }
        }
    }

    public void OpenMopMenu()
    {
        if (IsFilterSelectorUnlocked())
        {
            OpenFilterMenu();
            return;
        }

        OpenUpgradeMenu();
    }

    private void OpenUpgradeMenu()
    {
        if (mopMenuPanel == null)
        {
            return;
        }

        if (filterMenuPanel != null)
        {
            filterMenuPanel.SetActive(false);
        }

        UpdateMopMenuUI();
        SetBackgroundOverlayActive(true);
        mopMenuPanel.SetActive(true);
    }

    private void OpenFilterMenu()
    {
        if (filterMenuPanel == null)
        {
            Debug.LogWarning("[MopUpgradeShelf] Filter menu panel is not assigned.");
            return;
        }

        if (mopMenuPanel != null)
        {
            mopMenuPanel.SetActive(false);
        }

        EnsureFilterContentSetup();
        BuildFilterCardsIfNeeded();
        RefreshFilterCards();
        SetBackgroundOverlayActive(true);
        filterMenuPanel.SetActive(true);
    }

    public void CloseMopMenu()
    {
        if (mopMenuPanel != null)
        {
            mopMenuPanel.SetActive(false);
        }

        if (filterMenuPanel != null)
        {
            filterMenuPanel.SetActive(false);
        }

        SetBackgroundOverlayActive(false);
    }

    public void ToggleMopMenu()
    {
        bool anyOpen = (mopMenuPanel != null && mopMenuPanel.activeSelf)
            || (filterMenuPanel != null && filterMenuPanel.activeSelf);

        if (anyOpen)
        {
            CloseMopMenu();
        }
        else
        {
            OpenMopMenu();
        }
    }

    private void UpdateMopMenuUI()
    {
        if (mopTiers == null || mopTiers.Count == 0)
        {
            return;
        }

        ClampCurrentIndex();

        MopTier current = mopTiers[currentMopIndex];

        if (mopNameText != null)
        {
            mopNameText.text = current.tierName;
        }

        if (mopDescText != null)
        {
            mopDescText.text = current.description;
        }

        if (mopCostText != null)
        {
            mopCostText.text = string.IsNullOrEmpty(current.loreDescription) ? string.Empty : current.loreDescription;
        }

        if (mopButtonImage != null && current.icon != null)
        {
            mopButtonImage.sprite = current.icon;
        }

        if (mopUpgradeButton == null)
        {
            return;
        }

        TMP_Text buttonText = mopUpgradeButton.GetComponentInChildren<TMP_Text>();
        bool hasNext = currentMopIndex < mopTiers.Count - 1;

        if (!hasNext)
        {
            mopUpgradeButton.interactable = false;

            if (buttonText != null)
            {
                buttonText.SetText("Filters Unlocked");
            }

            return;
        }

        MopTier next = mopTiers[currentMopIndex + 1];
        float wallet = scoreManager != null ? scoreManager.GetTotalProfit() : 0f;
        long totalDishes = scoreManager != null ? scoreManager.GetTotalDishes() : 0L;
        bool unlocked = totalDishes >= next.requiredDishes;
        bool affordable = wallet >= next.cost;

        mopUpgradeButton.interactable = scoreManager != null && unlocked && affordable;

        if (buttonText != null)
        {
            if (!unlocked)
            {
                buttonText.SetText($"Locked: {BigNumberFormatter.FormatNumber(next.requiredDishes)} dishes");
            }
            else
            {
                buttonText.SetText($"Upgrade for {BigNumberFormatter.FormatMoney((double)next.cost)}");
            }
        }
    }

    private void OnMopUpgradeButton()
    {
        SetMopTierIndex(currentMopIndex + 1, spend: true);
    }

    public void SetMopTierIndex(int targetIndex, bool spend = false)
    {
        if (mopTiers == null || mopTiers.Count == 0)
        {
            return;
        }

        targetIndex = Mathf.Clamp(targetIndex, 0, mopTiers.Count - 1);

        if (targetIndex == currentMopIndex)
        {
            ApplyCurrentFilter();
            RefreshOpenUI();
            return;
        }

        if (targetIndex < currentMopIndex)
        {
            currentMopIndex = targetIndex;
            SaveProgress();
            ApplyCurrentFilter();
            RefreshOpenUI();
            NotifySaveManager();
            return;
        }

        float totalCost = 0f;

        for (int i = currentMopIndex + 1; i <= targetIndex; i++)
        {
            MopTier tier = mopTiers[i];

            if (scoreManager != null && scoreManager.GetTotalDishes() < tier.requiredDishes)
            {
                Debug.Log($"[MopUpgradeShelf] {tier.tierName} locked.");
                return;
            }

            totalCost += tier.cost;
        }

        if (spend)
        {
            if (scoreManager == null)
            {
                Debug.LogWarning("[MopUpgradeShelf] ScoreManager not found.");
                return;
            }

            if (scoreManager.GetTotalProfit() < totalCost)
            {
                Debug.Log("[MopUpgradeShelf] Not enough profit to buy mop upgrade.");
                return;
            }

            scoreManager.SubtractProfit(totalCost, isPurchase: true);
        }

        currentMopIndex = targetIndex;
        EnsureDefaultPurchasedFilters();
        ValidateSelectedFilter();
        SaveProgress();
        ApplyCurrentFilter();
        RefreshOpenUI();
        NotifySaveManager();

        Debug.Log($"[MopUpgradeShelf] Mop tier set to {mopTiers[currentMopIndex].tierName}.");

        if (IsFilterSelectorUnlocked() && mopMenuPanel != null && mopMenuPanel.activeSelf)
        {
            OpenFilterMenu();
        }
    }

    private void EnsureFilterContentSetup()
    {
        if (filterContentParent == null)
        {
            return;
        }

        if (filterToggleGroup == null)
        {
            filterToggleGroup = filterContentParent.GetComponent<ToggleGroup>();

            if (filterToggleGroup == null)
            {
                filterToggleGroup = filterContentParent.gameObject.AddComponent<ToggleGroup>();
            }
        }

        filterToggleGroup.allowSwitchOff = false;

        if (!autoConfigureFilterContentLayout)
        {
            return;
        }

        VerticalLayoutGroup layoutGroup = filterContentParent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = filterContentParent.gameObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 8f;
        }

        ContentSizeFitter fitter = filterContentParent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = filterContentParent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private void BuildFilterCardsIfNeeded()
    {
        if (filterCardsBuilt)
        {
            return;
        }

        if (filterContentParent == null || filterCardPrefab == null)
        {
            Debug.LogWarning("[MopUpgradeShelf] Filter content parent or filter card prefab is not assigned.");
            return;
        }

        EnsureFilterContentSetup();

        for (int i = spawnedFilterCards.Count - 1; i >= 0; i--)
        {
            if (spawnedFilterCards[i] != null)
            {
                Destroy(spawnedFilterCards[i].gameObject);
            }
        }

        spawnedFilterCards.Clear();
        spawnedFilterOptions.Clear();

        for (int i = 0; i < filterOptions.Count; i++)
        {
            FilterOption option = filterOptions[i];
            if (option == null || string.IsNullOrWhiteSpace(option.filterId))
            {
                continue;
            }

            MopFilterCardUI card = Instantiate(filterCardPrefab, filterContentParent);
            card.name = $"FilterCard_{option.filterId}";
            card.gameObject.SetActive(true);
            card.Initialize(
                option,
                filterToggleGroup,
                SelectFilterFromCard,
                BuyFilterFromCard,
                option.filterId == StaticColorFilterId ? staticColorPalette : null,
                SelectStaticColorFromCard,
                GetSelectedStaticColor
            );
            spawnedFilterCards.Add(card);
            spawnedFilterOptions.Add(option);
        }

        filterCardsBuilt = true;
    }

    public void RebuildFilterCards()
    {
        filterCardsBuilt = false;
        BuildFilterCardsIfNeeded();
        RefreshFilterCards();
    }

    private void RefreshFilterCards()
    {
        float wallet = scoreManager != null ? scoreManager.GetTotalProfit() : 0f;

        for (int i = 0; i < spawnedFilterCards.Count; i++)
        {
            if (spawnedFilterCards[i] == null || i >= spawnedFilterOptions.Count)
            {
                continue;
            }

            FilterOption option = spawnedFilterOptions[i];
            bool purchased = IsFilterPurchased(option.filterId);
            bool selected = purchased && option.filterId == selectedFilterId;
            spawnedFilterCards[i].Refresh(purchased, selected, wallet);
        }
    }

    private void SelectFilterFromCard(FilterOption option)
    {
        if (option == null)
        {
            return;
        }

        SelectFilter(option.filterId, saveAndNotify: true);
    }

    private void SelectStaticColorFromCard(Color color)
    {
        SetSelectedStaticColor(color, saveAndNotify: true);
    }

    public Color GetSelectedStaticColor()
    {
        return selectedStaticColor;
    }

    public string GetSelectedStaticColorHtml()
    {
        return "#" + ColorUtility.ToHtmlStringRGBA(selectedStaticColor);
    }

    public void SetSelectedStaticColor(Color color, bool saveAndNotify = true)
    {
        selectedStaticColor = color;
        ApplyStaticColorToOverlayIfPossible();
        RefreshFilterCards();

        if (saveAndNotify)
        {
            SaveProgress();
            NotifySaveManager();
        }
    }

    public void SetSelectedStaticColorFromHtml(string htmlColor, bool saveAndNotify = true)
    {
        if (string.IsNullOrWhiteSpace(htmlColor))
        {
            return;
        }

        if (!htmlColor.StartsWith("#"))
        {
            htmlColor = "#" + htmlColor;
        }

        if (ColorUtility.TryParseHtmlString(htmlColor, out Color parsed))
        {
            SetSelectedStaticColor(parsed, saveAndNotify);
        }
    }

    private void ApplyStaticColorToOverlayIfPossible()
    {
        if (gameFilterOverlay == null)
        {
            gameFilterOverlay = GameFilterOverlay.Instance != null
                ? GameFilterOverlay.Instance
                : FindFirstObjectByType<GameFilterOverlay>();
        }

        if (gameFilterOverlay != null)
        {
            gameFilterOverlay.SetStaticColor(selectedStaticColor);
        }
    }

    private void BuyFilterFromCard(FilterOption option)
    {
        if (option == null || string.IsNullOrWhiteSpace(option.filterId))
        {
            return;
        }

        if (IsFilterPurchased(option.filterId))
        {
            SelectFilter(option.filterId, saveAndNotify: true);
            return;
        }

        if (scoreManager == null)
        {
            Debug.LogWarning("[MopUpgradeShelf] ScoreManager not found.");
            return;
        }

        if (scoreManager.GetTotalProfit() < option.cost)
        {
            Debug.Log($"[MopUpgradeShelf] Not enough profit to buy filter {option.displayName}.");
            return;
        }

        if (option.cost > 0f)
        {
            scoreManager.SubtractProfit(option.cost, isPurchase: true);
        }

        purchasedFilterIds.Add(option.filterId);
        SelectFilter(option.filterId, saveAndNotify: false);
        SaveProgress();
        RefreshFilterCards();
        NotifySaveManager();
    }

    private void SelectFilter(string filterId, bool saveAndNotify)
    {
        FilterOption option = FindFilterOption(filterId);
        if (option == null || !IsFilterPurchased(option.filterId))
        {
            return;
        }

        selectedFilterId = option.filterId;
        ApplyCurrentFilter();
        RefreshFilterCards();

        if (saveAndNotify)
        {
            SaveProgress();
            NotifySaveManager();
        }
    }

    private void ApplyCurrentFilter()
    {
        if (gameFilterOverlay == null)
        {
            gameFilterOverlay = GameFilterOverlay.Instance != null
                ? GameFilterOverlay.Instance
                : FindFirstObjectByType<GameFilterOverlay>();
        }

        if (gameFilterOverlay == null)
        {
            return;
        }

        if (IsFilterSelectorUnlocked())
        {
            FilterOption selected = FindFilterOption(selectedFilterId);
            if (selected == null || !IsFilterPurchased(selected.filterId))
            {
                ValidateSelectedFilter();
                selected = FindFilterOption(selectedFilterId);
            }

            if (selected != null)
            {
                if (selected.filterId == StaticColorFilterId)
                {
                    gameFilterOverlay.SetStaticColor(selectedStaticColor);
                }

                gameFilterOverlay.ApplyPreset(selected.preset);
                return;
            }
        }

        if (mopTiers == null || mopTiers.Count == 0)
        {
            return;
        }

        ClampCurrentIndex();
        gameFilterOverlay.ApplyPreset(mopTiers[currentMopIndex].filterPreset);
    }

    private void RefreshOpenUI()
    {
        if (mopMenuPanel != null && mopMenuPanel.activeSelf)
        {
            UpdateMopMenuUI();
        }

        if (filterMenuPanel != null && filterMenuPanel.activeSelf)
        {
            RefreshFilterCards();
        }
    }

    private bool IsFilterSelectorUnlocked()
    {
        return mopTiers != null && mopTiers.Count > 0 && currentMopIndex >= mopTiers.Count - 1;
    }

    private bool IsFilterPurchased(string filterId)
    {
        return !string.IsNullOrWhiteSpace(filterId) && purchasedFilterIds.Contains(filterId);
    }

    private FilterOption FindFilterOption(string filterId)
    {
        if (filterOptions == null || string.IsNullOrWhiteSpace(filterId))
        {
            return null;
        }

        for (int i = 0; i < filterOptions.Count; i++)
        {
            if (filterOptions[i] != null && filterOptions[i].filterId == filterId)
            {
                return filterOptions[i];
            }
        }

        return null;
    }

    private void EnsureDefaultPurchasedFilters()
    {
        if (purchasedFilterIds == null)
        {
            return;
        }

        if (filterOptions != null)
        {
            for (int i = 0; i < filterOptions.Count; i++)
            {
                FilterOption option = filterOptions[i];
                if (option != null && option.startsPurchased && !string.IsNullOrWhiteSpace(option.filterId))
                {
                    purchasedFilterIds.Add(option.filterId);
                }
            }
        }

        // Clean should always be available once the filter selector is unlocked.
        purchasedFilterIds.Add(CleanFilterId);
    }

    private void ValidateSelectedFilter()
    {
        EnsureDefaultPurchasedFilters();

        if (!string.IsNullOrWhiteSpace(selectedFilterId) && IsFilterPurchased(selectedFilterId) && FindFilterOption(selectedFilterId) != null)
        {
            return;
        }

        if (FindFilterOption(CleanFilterId) != null)
        {
            selectedFilterId = CleanFilterId;
            return;
        }

        if (filterOptions != null && filterOptions.Count > 0 && filterOptions[0] != null)
        {
            selectedFilterId = filterOptions[0].filterId;
        }
    }

    private void LoadProgress()
    {
        purchasedFilterIds.Clear();

        if (!usePlayerPrefsPersistence)
        {
            currentMopIndex = 0;
            selectedFilterId = CleanFilterId;
            EnsureDefaultPurchasedFilters();
            ApplyStaticColorToOverlayIfPossible();
            return;
        }

        currentMopIndex = PlayerPrefs.GetInt(prefsKey, 0);
        selectedFilterId = PlayerPrefs.GetString(selectedFilterPrefsKey, CleanFilterId);
        SetSelectedStaticColorFromHtml(PlayerPrefs.GetString(selectedStaticColorPrefsKey, GetSelectedStaticColorHtml()), saveAndNotify: false);

        string purchasedRaw = PlayerPrefs.GetString(purchasedFiltersPrefsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(purchasedRaw))
        {
            string[] pieces = purchasedRaw.Split('|');
            for (int i = 0; i < pieces.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(pieces[i]))
                {
                    purchasedFilterIds.Add(pieces[i]);
                }
            }
        }

        EnsureDefaultPurchasedFilters();
    }

    private void SaveProgress()
    {
        if (!usePlayerPrefsPersistence)
        {
            return;
        }

        PlayerPrefs.SetInt(prefsKey, currentMopIndex);
        PlayerPrefs.SetString(selectedFilterPrefsKey, selectedFilterId ?? CleanFilterId);
        PlayerPrefs.SetString(purchasedFiltersPrefsKey, string.Join("|", GetPurchasedFilterIdsForSave()));
        PlayerPrefs.SetString(selectedStaticColorPrefsKey, GetSelectedStaticColorHtml());
        PlayerPrefs.Save();
    }

    private void ClampCurrentIndex()
    {
        if (mopTiers == null || mopTiers.Count == 0)
        {
            currentMopIndex = 0;
            return;
        }

        currentMopIndex = Mathf.Clamp(currentMopIndex, 0, mopTiers.Count - 1);
    }

    private void SetBackgroundOverlayActive(bool active)
    {
        if (backgroundOverlayButton != null)
        {
            backgroundOverlayButton.gameObject.SetActive(active);
        }
    }

    private void NotifySaveManager()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
        }
    }

    private List<string> GetPurchasedFilterIdsForSave()
    {
        EnsureDefaultPurchasedFilters();
        return new List<string>(purchasedFilterIds);
    }

    public int GetSaveState()
    {
        ClampCurrentIndex();
        return currentMopIndex;
    }

    public void GetSaveState(out int mopIndex, out string selectedId, out List<string> purchasedIds)
    {
        GetSaveState(out mopIndex, out selectedId, out purchasedIds, out _);
    }

    public void GetSaveState(out int mopIndex, out string selectedId, out List<string> purchasedIds, out string staticColorHtml)
    {
        ClampCurrentIndex();
        ValidateSelectedFilter();

        mopIndex = currentMopIndex;
        selectedId = selectedFilterId;
        purchasedIds = GetPurchasedFilterIdsForSave();
        staticColorHtml = GetSelectedStaticColorHtml();
    }

    public void ApplySaveState(int savedIndex)
    {
        ApplySaveState(savedIndex, CleanFilterId, null);
    }

    public void ApplySaveState(int savedIndex, string savedSelectedFilterId, List<string> savedPurchasedFilterIds)
    {
        ApplySaveState(savedIndex, savedSelectedFilterId, savedPurchasedFilterIds, GetSelectedStaticColorHtml());
    }

    public void ApplySaveState(int savedIndex, string savedSelectedFilterId, List<string> savedPurchasedFilterIds, string savedStaticColorHtml)
    {
        EnsureDefaultTiers();
        EnsureDefaultFilterOptions();
        SetSelectedStaticColorFromHtml(savedStaticColorHtml, saveAndNotify: false);

        currentMopIndex = Mathf.Clamp(savedIndex, 0, mopTiers.Count - 1);

        purchasedFilterIds.Clear();
        if (savedPurchasedFilterIds != null)
        {
            for (int i = 0; i < savedPurchasedFilterIds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(savedPurchasedFilterIds[i]))
                {
                    purchasedFilterIds.Add(savedPurchasedFilterIds[i]);
                }
            }
        }

        selectedFilterId = string.IsNullOrWhiteSpace(savedSelectedFilterId)
            ? CleanFilterId
            : savedSelectedFilterId;

        EnsureDefaultPurchasedFilters();
        ValidateSelectedFilter();
        SaveProgress();
        ApplyCurrentFilter();
        RefreshOpenUI();
    }

    public void ResetMopProgress()
    {
        currentMopIndex = 0;
        selectedFilterId = CleanFilterId;
        purchasedFilterIds.Clear();
        EnsureDefaultPurchasedFilters();
        SaveProgress();
        ApplyCurrentFilter();
        RefreshOpenUI();
    }

    public void ClearSavedMopProgress()
    {
        PlayerPrefs.DeleteKey(prefsKey);
        PlayerPrefs.DeleteKey(selectedFilterPrefsKey);
        PlayerPrefs.DeleteKey(purchasedFiltersPrefsKey);
        PlayerPrefs.DeleteKey(selectedStaticColorPrefsKey);
        PlayerPrefs.Save();
        ResetMopProgress();
    }
}
