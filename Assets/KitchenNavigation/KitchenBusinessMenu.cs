using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the Other Businesses button and the kitchen list menu.
/// This version manually positions cards and updates only their stat text while open.
/// </summary>
public class KitchenBusinessMenu : MonoBehaviour
{
    [Serializable]
    public class KitchenDefinition
    {
        [Header("Identity")]
        public string kitchenId = "kitchen_1";
        public string kitchenName = "Kitchen 1";

        [Header("Scenes")]
        public string kitchenSceneName = "Kitchen1";
        public string discoveryCutsceneSceneName = "";

        [Header("Menu Visual")]
        [Tooltip("The picture shown on the child Image inside the business card.")]
        public Sprite kitchenPicture;

        [Tooltip("The color of the root Button/card background.")]
        public Color cardColor = new Color(0.18f, 0.42f, 0.38f, 1f);

        [Header("Starting State")]
        public bool startsDiscovered = false;
    }

    [Header("Current Kitchen")]
    [SerializeField] private string currentKitchenId = "kitchen_1";
    [SerializeField] private LoanManager currentLoanManager;

    [Header("UI")]
    [SerializeField] private GameObject otherBusinessesButtonRoot;
    [SerializeField] private Button otherBusinessesButton;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform cardParent;
    [SerializeField] private KitchenBusinessCardUI cardPrefab;
    [SerializeField] private Button closeButton;

    [Header("Kitchen List")]
    [SerializeField] private List<KitchenDefinition> kitchens = new List<KitchenDefinition>();

    [Header("Manual Card Layout")]
    [SerializeField] private float cardHeight = 150f;
    [SerializeField] private float cardSpacing = 15f;
    [SerializeField] private float topPadding = 25f;
    [SerializeField] private float bottomPadding = 25f;
    [SerializeField] private float sidePadding = 10f;

    [Header("Open Menu Text Refresh")]
    [Tooltip("How often the open menu updates only money and money-per-second text.")]
    [SerializeField] private float openMenuRefreshSeconds = 0.5f;

    private readonly Dictionary<string, KitchenBusinessCardUI> discoveredCardsByKitchenId =
        new Dictionary<string, KitchenBusinessCardUI>();

    private KitchenBusinessCardUI ventureForthCard;
    private float openMenuRefreshTimer;

    private void Reset()
    {
        currentLoanManager = FindFirstObjectByType<LoanManager>();
    }

    private void Awake()
    {
        if (currentLoanManager == null)
        {
            currentLoanManager = FindFirstObjectByType<LoanManager>();
        }

        if (otherBusinessesButton == null && otherBusinessesButtonRoot != null)
        {
            otherBusinessesButton = otherBusinessesButtonRoot.GetComponent<Button>();
        }

        if (otherBusinessesButton != null)
        {
            otherBusinessesButton.onClick.RemoveAllListeners();
            otherBusinessesButton.onClick.AddListener(OpenMenu);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseMenu);
        }

        DisableAutoLayoutOnContent();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        EnsureStartingKitchensAreMarked();
        RefreshOtherBusinessesButtonVisibility();
    }

    private void OnEnable()
    {
        LoanManager.OnKitchenLoanStateChanged += HandleKitchenLoanStateChanged;
    }

    private void OnDisable()
    {
        LoanManager.OnKitchenLoanStateChanged -= HandleKitchenLoanStateChanged;
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
        {
            return;
        }

        openMenuRefreshTimer += Time.unscaledDeltaTime;

        if (openMenuRefreshTimer >= openMenuRefreshSeconds)
        {
            openMenuRefreshTimer = 0f;
            RefreshVisibleStatsOnly();
        }
    }

    private void HandleKitchenLoanStateChanged(string kitchenId, bool allLoansPaid)
    {
        if (kitchenId != currentKitchenId)
        {
            return;
        }

        RefreshOtherBusinessesButtonVisibility();

        if (panelRoot != null && panelRoot.activeSelf)
        {
            // Loan state can add the Venture Forth card, so this still rebuilds the structure.
            BuildMenu();
        }
    }

    private void DisableAutoLayoutOnContent()
    {
        if (cardParent == null)
        {
            return;
        }

        VerticalLayoutGroup verticalLayoutGroup = cardParent.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup != null)
        {
            verticalLayoutGroup.enabled = false;
        }

        ContentSizeFitter contentSizeFitter = cardParent.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = false;
        }

        LayoutGroup layoutGroup = cardParent.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }
    }

    private void EnsureStartingKitchensAreMarked()
    {
        for (int i = 0; i < kitchens.Count; i++)
        {
            KitchenDefinition kitchen = kitchens[i];

            if (kitchen != null && kitchen.startsDiscovered)
            {
                KitchenBusinessProgress.MarkKitchenDiscovered(kitchen.kitchenId);
            }
        }
    }

    private void RefreshOtherBusinessesButtonVisibility()
    {
        if (otherBusinessesButtonRoot == null)
        {
            return;
        }

        otherBusinessesButtonRoot.SetActive(KitchenBusinessProgress.AreOtherBusinessesUnlocked());
    }

    public void OpenMenu()
    {
        if (!KitchenBusinessProgress.AreOtherBusinessesUnlocked())
        {
            RefreshOtherBusinessesButtonVisibility();
            return;
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        openMenuRefreshTimer = 0f;
        BuildMenu();
        RefreshVisibleStatsOnly();
    }

    public void CloseMenu()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void BuildMenu()
    {
        if (cardParent == null || cardPrefab == null)
        {
            Debug.LogWarning("[KitchenBusinessMenu] Missing cardParent or cardPrefab.");
            return;
        }

        DisableAutoLayoutOnContent();
        ClearCards();

        int highestDiscoveredIndex = GetHighestDiscoveredKitchenIndex();
        int cardIndex = 0;

        if (highestDiscoveredIndex >= 0)
        {
            for (int i = 0; i <= highestDiscoveredIndex; i++)
            {
                KitchenDefinition kitchen = kitchens[i];

                if (kitchen == null)
                {
                    continue;
                }

                CreateDiscoveredCard(kitchen, cardIndex);
                cardIndex++;
            }

            int nextIndex = highestDiscoveredIndex + 1;

            if (nextIndex < kitchens.Count && ShouldShowVentureForthCard(highestDiscoveredIndex))
            {
                CreateVentureForthCard(kitchens[nextIndex], cardIndex);
                cardIndex++;
            }
        }

        UpdateContentHeight(cardIndex);
    }

    private void ClearCards()
    {
        discoveredCardsByKitchenId.Clear();
        ventureForthCard = null;

        if (cardParent == null)
        {
            return;
        }

        for (int i = cardParent.childCount - 1; i >= 0; i--)
        {
            GameObject child = cardParent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private int GetHighestDiscoveredKitchenIndex()
    {
        int highest = -1;

        for (int i = 0; i < kitchens.Count; i++)
        {
            KitchenDefinition kitchen = kitchens[i];

            if (kitchen == null)
            {
                continue;
            }

            if (KitchenBusinessProgress.IsKitchenDiscovered(kitchen.kitchenId, kitchen.startsDiscovered))
            {
                highest = i;
            }
        }

        return highest;
    }

    private bool ShouldShowVentureForthCard(int highestDiscoveredIndex)
    {
        if (highestDiscoveredIndex < 0 || highestDiscoveredIndex >= kitchens.Count)
        {
            return false;
        }

        KitchenDefinition newestDiscoveredKitchen = kitchens[highestDiscoveredIndex];

        if (newestDiscoveredKitchen == null)
        {
            return false;
        }

        if (newestDiscoveredKitchen.kitchenId != currentKitchenId)
        {
            return false;
        }

        if (currentLoanManager != null && currentLoanManager.AllLoansPaid())
        {
            return true;
        }

        return KitchenBusinessProgress.AreKitchenLoansPaid(currentKitchenId);
    }

    private void CreateDiscoveredCard(KitchenDefinition kitchen, int cardIndex)
    {
        KitchenBusinessCardUI card = Instantiate(cardPrefab, cardParent, false);
        PrepareSpawnedCard(card, cardIndex);

        bool statsAvailable = TryGetKitchenStats(
            kitchen.kitchenId,
            out float money,
            out float moneyPerSecond
        );

        card.Initialize(
            kitchen.kitchenName,
            kitchen.kitchenPicture,
            kitchen.cardColor,
            false,
            statsAvailable,
            money,
            moneyPerSecond,
            () => OnDiscoveredKitchenPressed(kitchen)
        );

        discoveredCardsByKitchenId[kitchen.kitchenId] = card;
    }

    private void CreateVentureForthCard(KitchenDefinition kitchen, int cardIndex)
    {
        KitchenBusinessCardUI card = Instantiate(cardPrefab, cardParent, false);
        PrepareSpawnedCard(card, cardIndex);

        card.Initialize(
            "Venture Forth",
            kitchen.kitchenPicture,
            kitchen.cardColor,
            true,
            false,
            0f,
            0f,
            () => OnVentureForthPressed(kitchen)
        );

        ventureForthCard = card;
    }

    private void PrepareSpawnedCard(KitchenBusinessCardUI card, int cardIndex)
    {
        if (card == null)
        {
            return;
        }

        GameObject cardObject = card.gameObject;
        cardObject.SetActive(true);
        cardObject.name = "BusinessCard";

        RectTransform rectTransform = card.transform as RectTransform;

        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;

            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);

            float y = topPadding + cardIndex * (cardHeight + cardSpacing);

            rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, y, cardHeight);

            rectTransform.offsetMin = new Vector2(sidePadding, rectTransform.offsetMin.y);
            rectTransform.offsetMax = new Vector2(-sidePadding, rectTransform.offsetMax.y);
        }

        LayoutElement layoutElement = cardObject.GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement = cardObject.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = true;

        Button button = cardObject.GetComponent<Button>();

        if (button != null)
        {
            button.interactable = true;
        }
    }

    private void UpdateContentHeight(int cardCount)
    {
        if (cardParent == null)
        {
            return;
        }

        float totalHeight = topPadding + bottomPadding;

        if (cardCount > 0)
        {
            totalHeight += cardCount * cardHeight;
            totalHeight += Mathf.Max(0, cardCount - 1) * cardSpacing;
        }

        cardParent.anchorMin = new Vector2(0f, 1f);
        cardParent.anchorMax = new Vector2(1f, 1f);
        cardParent.pivot = new Vector2(0f, 1f);

        cardParent.anchoredPosition = Vector2.zero;
        cardParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);

        Canvas.ForceUpdateCanvases();
    }

    private void RefreshVisibleStatsOnly()
    {
        foreach (KeyValuePair<string, KitchenBusinessCardUI> pair in discoveredCardsByKitchenId)
        {
            KitchenBusinessCardUI card = pair.Value;

            if (card == null)
            {
                continue;
            }

            bool statsAvailable = TryGetKitchenStats(
                pair.Key,
                out float money,
                out float moneyPerSecond
            );

            card.RefreshStats(false, statsAvailable, money, moneyPerSecond);
        }

        if (ventureForthCard != null)
        {
            ventureForthCard.RefreshStats(true, false, 0f, 0f);
        }
    }

    private bool TryGetKitchenStats(string kitchenId, out float money, out float moneyPerSecond)
    {
        money = 0f;
        moneyPerSecond = 0f;

        if (SaveManager.Instance != null &&
            SaveManager.Instance.TryGetKitchenBusinessStats(
                kitchenId,
                out float savedMoney,
                out float savedMoneyPerSecond,
                out long unusedDishes,
                out float unusedDishesPerSecond))
        {
            money = savedMoney;
            moneyPerSecond = savedMoneyPerSecond;
            return true;
        }

        if (kitchenId == currentKitchenId)
        {
            if (ScoreManager.Instance != null)
            {
                money = ScoreManager.Instance.GetTotalProfit();
                moneyPerSecond = ScoreManager.Instance.GetDisplayedProfitPerSecond();
                return true;
            }
        }

        return false;
    }

    private void OnDiscoveredKitchenPressed(KitchenDefinition kitchen)
    {
        if (kitchen == null)
        {
            return;
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
        }

        if (kitchen.kitchenId == currentKitchenId)
        {
            CloseMenu();
            return;
        }

        if (!string.IsNullOrEmpty(kitchen.kitchenSceneName))
        {
            SceneManager.LoadScene(kitchen.kitchenSceneName);
        }
    }

    private void OnVentureForthPressed(KitchenDefinition kitchen)
    {
        if (kitchen == null)
        {
            return;
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
        }

        bool shouldPlayDiscoveryCutscene =
            !KitchenBusinessProgress.HasSeenDiscoveryCutscene(kitchen.kitchenId) &&
            !string.IsNullOrEmpty(kitchen.discoveryCutsceneSceneName);

        KitchenBusinessProgress.MarkKitchenDiscovered(kitchen.kitchenId);

        if (shouldPlayDiscoveryCutscene)
        {
            KitchenBusinessProgress.MarkDiscoveryCutsceneSeen(kitchen.kitchenId);
            SceneManager.LoadScene(kitchen.discoveryCutsceneSceneName);
            return;
        }

        if (!string.IsNullOrEmpty(kitchen.kitchenSceneName))
        {
            SceneManager.LoadScene(kitchen.kitchenSceneName);
        }
    }
}
