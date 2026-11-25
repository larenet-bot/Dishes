using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SinkMenuUI : MonoBehaviour
{
    [Header("Scene Refs")]
    public SinkManager sinkManager;
    public ScoreManager scoreManager;

    [Header("Menu Root")]
    public GameObject menuPanel;
    public Button closeButton;

    [Header("Optional Overlay Button (click outside to close)")]
    public Button backgroundOverlayButton;

    [Serializable]
    public class SinkChoiceCard
    {
        public Button buyButton;
        public TMP_Text buyButtonText;
        public TMP_Text titleText;
        public TMP_Text descriptionText;
        public TMP_Text costText;
        public Image previewImage;
        [Tooltip("Optional custom description. Leave empty to use the current unlock node description.")]
        [TextArea(3, 8)] public string descriptionOverride;
    }

    [Header("Sink Selection (new)")]
    [Tooltip("Child of Menu Panel containing all three sink choice cards.")]
    public GameObject sinkSelectionPanel;
    [Tooltip("Child of Menu Panel containing the existing tree, details, purchase and sell controls.")]
    public GameObject upgradeTreePanel;
    public SinkChoiceCard powerWasherChoice = new SinkChoiceCard();
    public SinkChoiceCard washBasinChoice = new SinkChoiceCard();
    public SinkChoiceCard dishwasherChoice = new SinkChoiceCard();
    [Tooltip("Optional extra close button inside the selection panel. Not needed if Close Button is shared.")]
    public Button selectionCloseButton;

    [Header("Branch Tabs (legacy fallback; hidden when selection is configured)")]
    public Button powerWasherTabButton;
    public Button washBasinTabButton;
    public Button dishwasherTabButton;

    [Header("Branch Label")]
    public TMP_Text committedText;

    [Header("Node List")]
    public Transform nodeButtonContainer;
    public Button nodeButtonPrefab;

    [Header("Tier Rows (for tree layout)")]
    public RectTransform tierRowPrefab;

    [Header("Node Details")]
    public TMP_Text nodeNameText;
    public TMP_Text nodeDescText;
    public TMP_Text nodeLoreText;
    public TMP_Text nodeCostText;
    public Image nodeIconImage;

    [Header("Node Lock UI (optional)")]
    public TMP_Text nodeLockReasonText;
    public TMP_Text nodeRequirementsText;

    [Header("Node Visual States (alpha only)")]
    [Range(0f, 1f)] public float canBuyAlpha = 1f;
    [Range(0f, 1f)] public float ownedAlpha = 0.55f;
    [Range(0f, 1f)] public float lockedAlpha = 0.25f;
    [Range(0f, 1f)] public float noMoneyAlpha = 0.5f;

    [Header("Actions")]
    public Button purchaseButton;
    public Button resetButton;

    [Header("Sink HUD Image (optional)")]
    public Image sinkHudImage;

    private SinkManager.SinkType activeBranch = SinkManager.SinkType.PowerWasher;
    private string selectedNodeId;
    private readonly List<Button> spawnedButtons = new List<Button>();
    private readonly List<RectTransform> spawnedTierRows = new List<RectTransform>();
    private readonly List<SinkManager.SinkNode> displayedNodes = new List<SinkManager.SinkNode>();
    private bool useSelector;
    private bool menuWasOpen;
    private bool showingSelection;
    private bool treeBuilt;
    private bool initialized;
    private float nextRefresh;
    private Action<SinkManager.SinkType> onSinkTypeChangedHandler;
    private Action<string> onNodePurchasedHandler;
    private Action onSinkResetHandler;

    private enum NodeVisualState { Owned, Buyable, NoMoney, Locked }

    private void Reset()
    {
        sinkManager = FindAnyObjectByType<SinkManager>();
        scoreManager = FindAnyObjectByType<ScoreManager>();
    }

    private void Awake()
    {
        if (sinkManager == null) sinkManager = FindAnyObjectByType<SinkManager>();
        if (scoreManager == null) scoreManager = FindAnyObjectByType<ScoreManager>();

        useSelector = SelectorIsConfigured();
        if (!useSelector && (sinkSelectionPanel != null || upgradeTreePanel != null))
            Debug.LogWarning("[SinkMenuUI] Incomplete sink selector setup. Using legacy branch tabs.", this);

        WireButton(closeButton, CloseMenu);
        WireButton(backgroundOverlayButton, CloseMenu);
        WireButton(purchaseButton, OnPurchaseClicked);
        WireButton(resetButton, OnResetClicked);
        if (useSelector)
        {
            WireButton(selectionCloseButton, CloseMenu);
            WireChoice(powerWasherChoice, SinkManager.SinkType.PowerWasher);
            WireChoice(washBasinChoice, SinkManager.SinkType.WashBasin);
            WireChoice(dishwasherChoice, SinkManager.SinkType.Dishwasher);
            SetTabVisible(powerWasherTabButton, false);
            SetTabVisible(washBasinTabButton, false);
            SetTabVisible(dishwasherTabButton, false);
        }
        else
        {
            WireTab(powerWasherTabButton, SinkManager.SinkType.PowerWasher);
            WireTab(washBasinTabButton, SinkManager.SinkType.WashBasin);
            WireTab(dishwasherTabButton, SinkManager.SinkType.Dishwasher);
            if (sinkSelectionPanel != null && sinkSelectionPanel != menuPanel &&
                sinkSelectionPanel != upgradeTreePanel) sinkSelectionPanel.SetActive(false);
            if (upgradeTreePanel != null && upgradeTreePanel != menuPanel) upgradeTreePanel.SetActive(true);
        }

        if (sinkManager != null)
        {
            onSinkTypeChangedHandler = _ => OnSinkStateChanged();
            onNodePurchasedHandler = _ => OnSinkStateChanged();
            onSinkResetHandler = OnSinkStateChanged;
            sinkManager.OnSinkTypeChanged += onSinkTypeChangedHandler;
            sinkManager.OnNodePurchased += onNodePurchasedHandler;
            sinkManager.OnSinkReset += onSinkResetHandler;
        }
        initialized = true;
        CloseMenu();
    }

    private void Start() { RefreshSinkHudSprite(); }

    private void OnDestroy()
    {
        if (sinkManager == null) return;
        if (onSinkTypeChangedHandler != null) sinkManager.OnSinkTypeChanged -= onSinkTypeChangedHandler;
        if (onNodePurchasedHandler != null) sinkManager.OnNodePurchased -= onNodePurchasedHandler;
        if (onSinkResetHandler != null) sinkManager.OnSinkReset -= onSinkResetHandler;
    }

    private bool SelectorIsConfigured()
    {
        if (menuPanel == null || sinkSelectionPanel == null || upgradeTreePanel == null ||
            sinkSelectionPanel == upgradeTreePanel || sinkSelectionPanel == menuPanel || upgradeTreePanel == menuPanel)
            return false;
        if (!sinkSelectionPanel.transform.IsChildOf(menuPanel.transform) ||
            !upgradeTreePanel.transform.IsChildOf(menuPanel.transform) ||
            sinkSelectionPanel.transform.IsChildOf(upgradeTreePanel.transform) ||
            upgradeTreePanel.transform.IsChildOf(sinkSelectionPanel.transform)) return false;
        return ChoiceIsConfigured(powerWasherChoice) && ChoiceIsConfigured(washBasinChoice) &&
            ChoiceIsConfigured(dishwasherChoice) && powerWasherChoice.buyButton != washBasinChoice.buyButton &&
            powerWasherChoice.buyButton != dishwasherChoice.buyButton && washBasinChoice.buyButton != dishwasherChoice.buyButton;
    }

    private bool ChoiceIsConfigured(SinkChoiceCard card)
    {
        return card != null && card.buyButton != null &&
            card.buyButton.transform.IsChildOf(sinkSelectionPanel.transform);
    }

    private void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void SetTabVisible(Button button, bool visible)
    {
        if (button != null) button.gameObject.SetActive(visible);
    }

    private void WireChoice(SinkChoiceCard card, SinkManager.SinkType branch)
    {
        WireButton(card.buyButton, () => PurchaseBranch(branch));
        if (card.buyButtonText == null)
            card.buyButtonText = card.buyButton.GetComponentInChildren<TMP_Text>(true);
    }

    private bool IsMenuOpen => menuPanel != null && menuPanel.activeInHierarchy;

    public void OpenMenu()
    {
        if (menuPanel == null) return;
        menuPanel.SetActive(true);
        menuWasOpen = IsMenuOpen;
        if (backgroundOverlayButton != null) backgroundOverlayButton.gameObject.SetActive(true);
        SyncView(true);
    }

    public void CloseMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (backgroundOverlayButton != null) backgroundOverlayButton.gameObject.SetActive(false);
        menuWasOpen = false;
        treeBuilt = false;
    }

    private void Update()
    {
        if (!initialized) return;
        bool open = IsMenuOpen;
        // Support MenuManager or another existing caller activating/deactivating the root directly.
        if (open != menuWasOpen)
        {
            menuWasOpen = open;
            if (backgroundOverlayButton != null) backgroundOverlayButton.gameObject.SetActive(open);
            if (open) SyncView(true);
            else treeBuilt = false;
        }
        if (!open || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.2f;
        // Passive earnings continue while the menu is open. Refresh affordability without recreating rows.
        SyncView(false);
    }

    private void OnSinkStateChanged()
    {
        RefreshSinkHudSprite();
        if (IsMenuOpen) SyncView(false);
        else treeBuilt = false;
    }

    private void SyncView(bool forceBuild)
    {
        if (sinkManager == null) return;
        var current = sinkManager.CurrentSinkType;
        if (useSelector && current == SinkManager.SinkType.Basic)
        {
            if (!showingSelection || treeBuilt || forceBuild)
            {
                upgradeTreePanel.SetActive(false);
                sinkSelectionPanel.SetActive(true);
                ClearSpawnedButtons();
                treeBuilt = false;
                selectedNodeId = null;
            }
            showingSelection = true;
            RefreshChoiceCards();
            RefreshSinkHudSprite();
            return;
        }

        bool branchChanged = current != SinkManager.SinkType.Basic && activeBranch != current;
        if (current != SinkManager.SinkType.Basic) activeBranch = current;
        if (branchChanged || showingSelection) selectedNodeId = null;
        if (useSelector)
        {
            sinkSelectionPanel.SetActive(false);
            upgradeTreePanel.SetActive(true);
        }
        if (forceBuild || !treeBuilt || branchChanged || showingSelection)
        {
            BuildNodeButtons();
            AutoSelectFirstNodeIfNeeded();
            treeBuilt = true;
        }
        showingSelection = false;
        RefreshAll();
    }

    private void RefreshChoiceCards()
    {
        RefreshChoice(powerWasherChoice, SinkManager.SinkType.PowerWasher, sinkManager.powerWasherSinkSprite);
        RefreshChoice(washBasinChoice, SinkManager.SinkType.WashBasin, sinkManager.washBasinSinkSprite);
        RefreshChoice(dishwasherChoice, SinkManager.SinkType.Dishwasher, sinkManager.dishwasherSinkSprite);
    }

    private void RefreshChoice(SinkChoiceCard card, SinkManager.SinkType branch, Sprite defaultSprite)
    {
        var node = GetBranchUnlockNode(branch);
        SetText(card.titleText, BranchName(branch));
        SetText(card.descriptionText, !string.IsNullOrWhiteSpace(card.descriptionOverride)
            ? card.descriptionOverride : node != null ? node.description : "Unavailable");
        if (card.previewImage != null && card.previewImage.sprite == null && defaultSprite != null)
            card.previewImage.sprite = defaultSprite;
        SetText(card.costText, node == null ? "Unavailable" : BigNumberFormatter.FormatMoney((double)node.cost));
        bool canBuy = node != null && sinkManager.CurrentSinkType == SinkManager.SinkType.Basic &&
            sinkManager.CanPurchase(node.id) && Wallet >= node.cost;
        card.buyButton.interactable = canBuy;
        SetText(card.buyButtonText, node == null ? "Unavailable" :
            $"Buy {BigNumberFormatter.FormatMoney((double)node.cost)}");
    }

    private void PurchaseBranch(SinkManager.SinkType branch)
    {
        if (!useSelector || !IsMenuOpen || sinkManager == null ||
            sinkManager.CurrentSinkType != SinkManager.SinkType.Basic) return;
        var node = GetBranchUnlockNode(branch);
        if (node != null && sinkManager.TryPurchase(node.id))
        {
            // SinkManager commits the branch and raises its existing purchase events.
            // Only a successful purchase advances to the upgrade tree.
            SyncView(false);
        }
        else RefreshChoiceCards();
    }

    private static string BranchName(SinkManager.SinkType branch)
    {
        switch (branch)
        {
            case SinkManager.SinkType.PowerWasher: return "Power Washer";
            case SinkManager.SinkType.WashBasin: return "Wash Basin";
            case SinkManager.SinkType.Dishwasher: return "Dishwasher";
            default: return "Basic";
        }
    }

    private double Wallet
    {
        get
        {
            var score = ScoreManager.Instance != null ? ScoreManager.Instance : scoreManager;
            return score != null ? score.GetTotalProfitDouble() : 0d;
        }
    }

    private void WireTab(Button button, SinkManager.SinkType branch)
    {
        WireButton(button, () =>
        {
            activeBranch = branch;
            BuildNodeButtons();
            AutoSelectFirstNodeIfNeeded();
            treeBuilt = true;
            RefreshAll();
        });
    }

    private bool IsBranchLockedOut(SinkManager.SinkType branch)
    {
        return sinkManager != null && sinkManager.CurrentSinkType != SinkManager.SinkType.Basic &&
            sinkManager.CurrentSinkType != branch;
    }

    private SinkManager.SinkNode GetBranchUnlockNode(SinkManager.SinkType branch)
    {
        if (sinkManager == null) return null;
        foreach (var node in sinkManager.GetNodesForBranch(branch))
            if (node != null && node.unlocksSinkType) return node;
        return null;
    }

    private string GetNodeDisplayName(string nodeId)
    {
        var node = sinkManager != null ? sinkManager.GetNode(nodeId) : null;
        return node != null && !string.IsNullOrWhiteSpace(node.displayName) ? node.displayName : nodeId ?? "";
    }

    private List<string> GetMissingPrereqNames(SinkManager.SinkNode node)
    {
        var missing = new List<string>();
        if (node == null || node.requires == null) return missing;
        foreach (var req in node.requires)
            if (!string.IsNullOrWhiteSpace(req) && sinkManager != null && !sinkManager.IsPurchased(req))
                missing.Add(GetNodeDisplayName(req));
        return missing;
    }

    private void GetLockInfo(SinkManager.SinkNode node, out string lockReason, out string requirements, out NodeVisualState state)
    {
        lockReason = "";
        requirements = "";
        state = NodeVisualState.Locked;
        if (sinkManager == null || node == null) return;
        if (sinkManager.IsPurchased(node.id)) { state = NodeVisualState.Owned; return; }
        if (IsBranchLockedOut(node.branch))
        {
            lockReason = $"Locked. Committed to {BranchName(sinkManager.CurrentSinkType)}.";
            return;
        }
        if (sinkManager.CurrentSinkType == SinkManager.SinkType.Basic && !node.unlocksSinkType)
        {
            var unlock = GetBranchUnlockNode(node.branch);
            lockReason = "Locked. Choose a sink first.";
            if (unlock != null) requirements = $"Requires: {unlock.displayName}";
            return;
        }
        var missing = GetMissingPrereqNames(node);
        if (missing.Count > 0)
        {
            lockReason = "Locked. Missing requirements.";
            requirements = "Requires: " + string.Join(", ", missing);
            return;
        }
        if (!sinkManager.CanPurchase(node.id)) { lockReason = "Locked."; return; }
        if (Wallet < node.cost)
        {
            lockReason = "Not enough profit.";
            requirements = $"Need {BigNumberFormatter.FormatMoney(Math.Max(0d, node.cost - Wallet))} more.";
            state = NodeVisualState.NoMoney;
            return;
        }
        state = NodeVisualState.Buyable;
    }

    private void SetButtonAlpha(Button btn, float alpha)
    {
        if (btn == null) return;
        Graphic graphic = btn.targetGraphic != null ? btn.targetGraphic : btn.GetComponent<Image>();
        if (graphic != null)
        {
            Color c = graphic.color;
            if (!Mathf.Approximately(c.a, alpha)) { c.a = alpha; graphic.color = c; }
        }
        float textAlpha = Mathf.Clamp01(alpha * 0.9f + 0.1f);
        foreach (var label in btn.GetComponentsInChildren<TMP_Text>(true))
        {
            Color c = label.color;
            if (!Mathf.Approximately(c.a, textAlpha)) { c.a = textAlpha; label.color = c; }
        }
    }

    private void RefreshNodeButton(Button btn, SinkManager.SinkNode node)
    {
        if (btn == null || node == null) return;
        GetLockInfo(node, out _, out _, out var state);
        string status = state == NodeVisualState.Owned ? "OWNED" : state == NodeVisualState.Buyable ? "BUY" :
            state == NodeVisualState.NoMoney ? "COST" : "LOCKED";
        float alpha = state == NodeVisualState.Owned ? ownedAlpha : state == NodeVisualState.Buyable ? canBuyAlpha :
            state == NodeVisualState.NoMoney ? noMoneyAlpha : lockedAlpha;
        string separator = tierRowPrefab == null ? "  " : "\n";
        string techniqueTag = node.isTechnique ? " TECH" : "";
        SetText(btn.GetComponentInChildren<TMP_Text>(true), $"T{node.tier}{techniqueTag}{separator}{node.displayName}{separator}{status}".Trim());
        SetButtonAlpha(btn, alpha);
        btn.interactable = !IsBranchLockedOut(activeBranch);
    }

    private void BuildNodeButtons()
    {
        ClearSpawnedButtons();
        if (sinkManager == null || nodeButtonContainer == null || nodeButtonPrefab == null) return;
        var tiers = new SortedDictionary<int, List<SinkManager.SinkNode>>();
        foreach (var node in sinkManager.GetNodesForBranch(activeBranch))
        {
            if (node == null) continue;
            // Tier-one sink unlock was already purchased on the selection screen.
            if (useSelector && node.unlocksSinkType && sinkManager.IsPurchased(node.id)) continue;
            if (!tiers.TryGetValue(node.tier, out var bucket))
            {
                bucket = new List<SinkManager.SinkNode>();
                tiers.Add(node.tier, bucket);
            }
            bucket.Add(node);
        }
        foreach (var kvp in tiers)
        {
            Transform parent = nodeButtonContainer;
            if (tierRowPrefab != null)
            {
                var row = Instantiate(tierRowPrefab, nodeButtonContainer);
                row.name = $"TierRow_{kvp.Key}";
                spawnedTierRows.Add(row);
                row.anchorMin = new Vector2(0f, row.anchorMin.y);
                row.anchorMax = new Vector2(1f, row.anchorMax.y);
                row.offsetMin = new Vector2(0f, row.offsetMin.y);
                row.offsetMax = new Vector2(0f, row.offsetMax.y);
                parent = row;
            }
            foreach (var node in kvp.Value)
            {
                var btn = Instantiate(nodeButtonPrefab, parent);
                spawnedButtons.Add(btn);
                displayedNodes.Add(node);
                RefreshNodeButton(btn, node);
                string capturedId = node.id;
                WireButton(btn, () => { selectedNodeId = capturedId; RefreshAll(); });
            }
        }
        ForceLayoutRefresh();
    }

    private void ClearSpawnedButtons()
    {
        foreach (var button in spawnedButtons)
        {
            if (button == null) continue;
            button.gameObject.SetActive(false);
            Destroy(button.gameObject);
        }
        spawnedButtons.Clear();
        displayedNodes.Clear();
        foreach (var row in spawnedTierRows)
        {
            if (row == null) continue;
            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }
        if (nodeButtonContainer != null)
        {
            for (int i = nodeButtonContainer.childCount - 1; i >= 0; i--)
            {
                var child = nodeButtonContainer.GetChild(i);
                if (child == null || !child.name.StartsWith("TierRow_") || spawnedTierRows.Contains(child as RectTransform)) continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
        spawnedTierRows.Clear();
    }

    private void ForceLayoutRefresh()
    {
        if (!(nodeButtonContainer is RectTransform content)) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
    }

    private void AutoSelectFirstNodeIfNeeded()
    {
        foreach (var node in displayedNodes)
            if (node.id == selectedNodeId) return;
        selectedNodeId = displayedNodes.Count > 0 ? displayedNodes[0].id : null;
    }

    private void RefreshAll()
    {
        if (sinkManager == null) return;
        SetText(committedText, sinkManager.CurrentSinkType == SinkManager.SinkType.Basic
            ? "Sink: Basic (choose a branch to commit)" : $"Sink: {BranchName(sinkManager.CurrentSinkType)}");
        if (!useSelector)
        {
            if (powerWasherTabButton != null) powerWasherTabButton.interactable = !IsBranchLockedOut(SinkManager.SinkType.PowerWasher);
            if (washBasinTabButton != null) washBasinTabButton.interactable = !IsBranchLockedOut(SinkManager.SinkType.WashBasin);
            if (dishwasherTabButton != null) dishwasherTabButton.interactable = !IsBranchLockedOut(SinkManager.SinkType.Dishwasher);
        }
        for (int i = 0; i < spawnedButtons.Count; i++) RefreshNodeButton(spawnedButtons[i], displayedNodes[i]);
        RefreshDetailsPanel();
        RefreshActionButtons();
        RefreshSinkHudSprite();
    }

    private void RefreshDetailsPanel()
    {
        var node = sinkManager.GetNode(selectedNodeId);
        SetText(nodeNameText, node != null ? node.displayName : "");
        SetText(nodeDescText, node != null ? node.description : "");
        SetText(nodeCostText, node != null ? $"Cost: {BigNumberFormatter.FormatMoney((double)node.cost)}" : "");
        if (nodeIconImage != null) nodeIconImage.gameObject.SetActive(node != null && nodeIconImage.sprite != null);
        GetLockInfo(node, out string lockReason, out string reqText, out _);
        SetText(nodeLockReasonText, lockReason);
        SetText(nodeRequirementsText, reqText);
        if (node != null && nodeLockReasonText == null && nodeRequirementsText == null && nodeLoreText != null)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(node.loreDescription)) sb.Append(node.loreDescription.Trim());
            if (!string.IsNullOrWhiteSpace(lockReason))
            {
                if (sb.Length > 0) sb.Append("\n\n");
                sb.Append(lockReason);
            }
            if (!string.IsNullOrWhiteSpace(reqText))
            {
                if (sb.Length > 0) sb.Append("\n");
                sb.Append(reqText);
            }
            SetText(nodeLoreText, sb.ToString());
        }
        else SetText(nodeLoreText, node != null ? node.loreDescription : "");
    }

    private void RefreshActionButtons()
    {
        var node = sinkManager.GetNode(selectedNodeId);
        if (purchaseButton != null)
        {
            GetLockInfo(node, out _, out _, out var state);
            purchaseButton.interactable = state == NodeVisualState.Buyable;
            string label = node == null || state == NodeVisualState.Locked ? "Locked" :
                state == NodeVisualState.Owned ? "Purchased" : $"Buy {BigNumberFormatter.FormatMoney((double)node.cost)}";
            SetText(purchaseButton.GetComponentInChildren<TMP_Text>(true), label);
        }
        if (resetButton != null)
        {
            bool canReset = sinkManager.CurrentSinkType != SinkManager.SinkType.Basic || sinkManager.GetPurchasedNodeIds().Count > 0;
            resetButton.interactable = canReset;
            string refund = BigNumberFormatter.FormatMoney((double)sinkManager.GetRefundAmount());
            SetText(resetButton.GetComponentInChildren<TMP_Text>(true), !canReset ? "Reset (No sink chosen)" :
                useSelector ? $"Sell & Change Sink ({refund} refund)" : $"Sell Sink ({refund} refund)");
        }
    }

    private void RefreshSinkHudSprite()
    {
        if (sinkHudImage == null || sinkManager == null) return;
        var sprite = sinkManager.GetCurrentSinkSprite();
        if (sprite != null && sinkHudImage.sprite != sprite) sinkHudImage.sprite = sprite;
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null && target.text != (value ?? "")) target.SetText(value ?? "");
    }

    private void OnPurchaseClicked()
    {
        if (sinkManager == null || string.IsNullOrWhiteSpace(selectedNodeId)) return;
        sinkManager.TryPurchase(selectedNodeId);
        SyncView(false);
    }

    private void OnResetClicked()
    {
        if (sinkManager == null || !sinkManager.ResetToBasicAndRefund()) return;
        selectedNodeId = null;
        if (!useSelector) activeBranch = SinkManager.SinkType.PowerWasher;
        SyncView(!useSelector);
    }
}
