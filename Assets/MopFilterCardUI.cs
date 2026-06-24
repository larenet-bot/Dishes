using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI controller for one spawned Mop filter card.
/// This script only owns presentation and button/toggle/color wiring for a single filter option.
/// The purchase/selection rules live in MopUpgradeShelf.
/// </summary>
public class MopFilterCardUI : MonoBehaviour
{
    [Header("Card UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Image iconImage;

    [Header("Purchased State")]
    [Tooltip("Unity UI Toggle used like a radio button. It appears only after the filter is purchased.")]
    [SerializeField] private Toggle toggle;

    [Header("Locked State")]
    [Tooltip("Shown while the filter is locked. Put your lock icon/object here, usually where the Toggle would be.")]
    [SerializeField] private GameObject lockRoot;

    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    [Header("Optional Static Color UI")]
    [Tooltip("Optional root object shown only for the Static Color filter after it is purchased.")]
    [SerializeField] private GameObject staticColorControlsRoot;

    [Tooltip("Optional parent where color swatch buttons will be spawned.")]
    [SerializeField] private Transform staticColorSwatchParent;

    [Tooltip("Optional button prefab for color swatches. Put an Image on the button so this script can tint it.")]
    [SerializeField] private Button staticColorSwatchButtonPrefab;

    [Tooltip("Optional image preview of the currently selected static filter color.")]
    [SerializeField] private Image staticColorPreview;

    private MopUpgradeShelf.FilterOption option;
    private Action<MopUpgradeShelf.FilterOption> selectCallback;
    private Action<MopUpgradeShelf.FilterOption> buyCallback;
    private Action<Color> staticColorCallback;
    private Func<Color> selectedStaticColorProvider;
    private IReadOnlyList<Color> staticColorChoices;
    private readonly List<Button> spawnedSwatchButtons = new List<Button>();
    private bool isWired;
    private bool swatchesBuilt;

    private void Reset()
    {
        CacheChildReferences();
    }

    private void Awake()
    {
        CacheChildReferences();
    }

    private void CacheChildReferences()
    {
        if (toggle == null)
        {
            toggle = GetComponentInChildren<Toggle>(true);
        }

        if (buyButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (toggle != null && buttons[i].gameObject == toggle.gameObject)
                {
                    continue;
                }

                buyButton = buttons[i];
                break;
            }
        }

        if (buyButtonText == null && buyButton != null)
        {
            buyButtonText = buyButton.GetComponentInChildren<TMP_Text>(true);
        }
    }

    public void Initialize(
        MopUpgradeShelf.FilterOption filterOption,
        ToggleGroup toggleGroup,
        Action<MopUpgradeShelf.FilterOption> onSelect,
        Action<MopUpgradeShelf.FilterOption> onBuy)
    {
        Initialize(filterOption, toggleGroup, onSelect, onBuy, null, null, null);
    }

    public void Initialize(
        MopUpgradeShelf.FilterOption filterOption,
        ToggleGroup toggleGroup,
        Action<MopUpgradeShelf.FilterOption> onSelect,
        Action<MopUpgradeShelf.FilterOption> onBuy,
        IReadOnlyList<Color> staticColors,
        Action<Color> onStaticColorSelected,
        Func<Color> getSelectedStaticColor)
    {
        option = filterOption;
        selectCallback = onSelect;
        buyCallback = onBuy;
        staticColorChoices = staticColors;
        staticColorCallback = onStaticColorSelected;
        selectedStaticColorProvider = getSelectedStaticColor;

        CacheChildReferences();
        WireEvents(toggleGroup);
        BuildStaticColorSwatchesIfNeeded();
        Refresh(option != null && option.startsPurchased, false, 0f);
    }

    private void WireEvents(ToggleGroup toggleGroup)
    {
        if (isWired)
        {
            return;
        }

        if (toggle != null)
        {
            toggle.group = toggleGroup;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        isWired = true;
    }

    private void BuildStaticColorSwatchesIfNeeded()
    {
        if (swatchesBuilt)
        {
            return;
        }

        swatchesBuilt = true;

        if (!IsStaticColorCard() || staticColorSwatchParent == null || staticColorSwatchButtonPrefab == null || staticColorChoices == null)
        {
            return;
        }

        for (int i = 0; i < staticColorChoices.Count; i++)
        {
            Color swatchColor = staticColorChoices[i];
            Button swatch = Instantiate(staticColorSwatchButtonPrefab, staticColorSwatchParent);
            swatch.gameObject.name = $"StaticColorSwatch_{ColorUtility.ToHtmlStringRGBA(swatchColor)}";
            swatch.gameObject.SetActive(true);

            Image swatchImage = swatch.GetComponent<Image>();
            if (swatchImage != null)
            {
                Color visibleColor = swatchColor;
                visibleColor.a = 1f;
                swatchImage.color = visibleColor;
            }

            swatch.onClick.RemoveAllListeners();
            swatch.onClick.AddListener(() => OnStaticColorSwatchClicked(swatchColor));
            spawnedSwatchButtons.Add(swatch);
        }
    }

    public void Refresh(bool purchased, bool selected, float wallet)
    {
        if (option == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (nameText != null)
        {
            nameText.SetText(option.displayName);
        }

        if (descriptionText != null)
        {
            descriptionText.SetText(option.description);
        }

        if (iconImage != null)
        {
            iconImage.sprite = option.icon;
            iconImage.enabled = option.icon != null;
        }

        if (toggle != null)
        {
            toggle.gameObject.SetActive(purchased);
            toggle.SetIsOnWithoutNotify(selected);
            toggle.interactable = purchased;
        }

        if (lockRoot != null)
        {
            lockRoot.SetActive(!purchased);
        }

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(!purchased);
            buyButton.interactable = wallet >= option.cost;
        }

        if (costText != null)
        {
            costText.SetText(purchased ? "Owned" : BigNumberFormatter.FormatMoney((double)option.cost));
        }

        if (buyButtonText != null)
        {
            buyButtonText.SetText(option.cost <= 0f ? "Unlock" : $"Buy {BigNumberFormatter.FormatMoney((double)option.cost)}");
        }

        RefreshStaticColorUI(purchased);
    }

    private void RefreshStaticColorUI(bool purchased)
    {
        bool showStaticColorControls = purchased && IsStaticColorCard();

        if (staticColorControlsRoot != null)
        {
            staticColorControlsRoot.SetActive(showStaticColorControls);
        }

        if (!showStaticColorControls)
        {
            return;
        }

        BuildStaticColorSwatchesIfNeeded();

        Color selectedColor = selectedStaticColorProvider != null
            ? selectedStaticColorProvider.Invoke()
            : Color.white;

        if (staticColorPreview != null)
        {
            staticColorPreview.color = selectedColor;
        }
    }

    private bool IsStaticColorCard()
    {
        return option != null && option.filterId == MopUpgradeShelf.StaticColorFilterId;
    }

    private void OnStaticColorSwatchClicked(Color color)
    {
        staticColorCallback?.Invoke(color);
        RefreshStaticColorUI(true);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (!isOn || option == null)
        {
            return;
        }

        selectCallback?.Invoke(option);
    }

    private void OnBuyClicked()
    {
        if (option == null)
        {
            return;
        }

        buyCallback?.Invoke(option);
    }
}
