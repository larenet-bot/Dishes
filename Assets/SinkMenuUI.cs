using System;
using System.Collections.Generic;
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

    [Header("Branch Tabs")]
    public Button powerWasherTabButton;
    public Button washBasinTabButton;
    public Button dishwasherTabButton;

    [Header("Branch Label")]
    public TMP_Text committedText;

    [Header("Node List")]
    public Transform nodeButtonContainer;
    public Button nodeButtonPrefab;

    [Header("Tier Rows (for tree layout)")]
    public RectTransform tierRowPrefab; // prefab with HorizontalLayoutGroup + ContentSizeFitter

    [Header("Node Details")]
    public TMP_Text nodeNameText;
    public TMP_Text nodeDescText;
    public TMP_Text nodeLoreText;
    public TMP_Text nodeCostText;
    public Image nodeIconImage;

    [Header("Actions")]
    public Button purchaseButton;
    public Button resetButton;

    [Header("Sink HUD Image (optional)")]
    public Image sinkHudImage;

    private SinkManager.SinkType activeBranch = SinkManager.SinkType.PowerWasher;
    private string selectedNodeId = null;

    private readonly List<Button> spawnedButtons = new List<Button>();

    private void Reset()
    {
        sinkManager = FindFirstObjectByType<SinkManager>();
        scoreManager = FindFirstObjectByType<ScoreManager>();
    }

    private void Awake()
    {
        if (sinkManager == null) sinkManager = FindFirstObjectByType<SinkManager>();
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseMenu);
        }

        if (backgroundOverlayButton != null)
        {
            backgroundOverlayButton.onClick.RemoveAllListeners();
            backgroundOverlayButton.onClick.AddListener(CloseMenu);
            if (backgroundOverlayButton.gameObject.activeSelf)
                backgroundOverlayButton.gameObject.SetActive(false);
        }

        WireTab(powerWasherTabButton, SinkManager.SinkType.PowerWasher);
        WireTab(washBasinTabButton, SinkManager.SinkType.WashBasin);
        WireTab(dishwasherTabButton, SinkManager.SinkType.Dishwasher);

        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(OnResetClicked);
        }

        if (sinkManager != null)
        {
            sinkManager.OnSinkTypeChanged += _ => RefreshAll();
            sinkManager.OnNodePurchased += _ => RefreshAll();
            sinkManager.OnSinkReset += RefreshAll;
        }

        CloseMenu();
    }

    private void OnDestroy()
    {
        if (sinkManager != null)
        {
            sinkManager.OnSinkTypeChanged -= _ => RefreshAll();
            sinkManager.OnNodePurchased -= _ => RefreshAll();
            sinkManager.OnSinkReset -= RefreshAll;
        }
    }

    // --------------------------
    // Public UI API
    // --------------------------

    public void OpenMenu()
    {
        if (menuPanel == null) return;

        menuPanel.SetActive(true);
        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(true);

        // Default branch selection:
        // If committed, show committed branch. Otherwise keep last.
        if (sinkManager != null && sinkManager.CurrentSinkType != SinkManager.SinkType.Basic)
            activeBranch = sinkManager.CurrentSinkType;

        BuildNodeButtons();
        AutoSelectFirstNodeIfNeeded();
        RefreshAll();
    }

    public void CloseMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (backgroundOverlayButton != null)
            backgroundOverlayButton.gameObject.SetActive(false);
    }

    // --------------------------
    // Tabs / Branch
    // --------------------------

    private void WireTab(Button button, SinkManager.SinkType branch)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            activeBranch = branch;
            BuildNodeButtons();
            AutoSelectFirstNodeIfNeeded();
            RefreshAll();
        });
    }

    private bool IsBranchLockedOut(SinkManager.SinkType branch)
    {
        if (sinkManager == null) return false;
        if (sinkManager.CurrentSinkType == SinkManager.SinkType.Basic) return false;
        return sinkManager.CurrentSinkType != branch;
    }

    // --------------------------
    // Node list
    // --------------------------

    private void BuildNodeButtons()
    {
        ClearSpawnedButtons();

        if (sinkManager == null || nodeButtonContainer == null || nodeButtonPrefab == null)
            return;

        if (tierRowPrefab == null)
        {
            Debug.LogWarning("[SinkMenuUI] tierRowPrefab not set. Falling back to vertical list.");
            BuildNodeButtons_FallbackVertical();
            return;
        }

        var list = sinkManager.GetNodesForBranch(activeBranch);

        // Group by tier
        var tiers = new SortedDictionary<int, List<SinkManager.SinkNode>>();
        for (int i = 0; i < list.Count; i++)
        {
            var node = list[i];
            if (node == null) continue;

            if (!tiers.TryGetValue(node.tier, out var bucket))
            {
                bucket = new List<SinkManager.SinkNode>();
                tiers.Add(node.tier, bucket);
            }
            bucket.Add(node);
        }

        bool lockedOut = IsBranchLockedOut(activeBranch);

        foreach (var kvp in tiers)
        {
            int tier = kvp.Key;
            var tierNodes = kvp.Value;

            // Make a row for this tier
            var row = Instantiate(tierRowPrefab, nodeButtonContainer);
            row.name = $"TierRow_{tier}";

            // Optional: if you want row to stretch full width of content
            row.anchorMin = new Vector2(0f, row.anchorMin.y);
            row.anchorMax = new Vector2(1f, row.anchorMax.y);
            row.offsetMin = new Vector2(0f, row.offsetMin.y);
            row.offsetMax = new Vector2(0f, row.offsetMax.y);

            // Sort nodes inside tier (techniques last, then name)
            tierNodes.Sort((a, b) =>
            {
                int tech = a.isTechnique.CompareTo(b.isTechnique);
                if (tech != 0) return tech;
                return string.Compare(a.displayName, b.displayName, StringComparison.Ordinal);
            });

            for (int i = 0; i < tierNodes.Count; i++)
            {
                var node = tierNodes[i];
                if (node == null) continue;

                var btn = Instantiate(nodeButtonPrefab, row);
                spawnedButtons.Add(btn);

                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    string status =
                        sinkManager.IsPurchased(node.id) ? "OWNED" :
                        sinkManager.CanPurchase(node.id) ? "" :
                        "LOCKED";

                    string techniqueTag = node.isTechnique ? " TECH" : "";
                    label.SetText($"T{node.tier}{techniqueTag}\n{node.displayName}\n{status}".Trim());
                }

                btn.interactable = !lockedOut;

                string capturedId = node.id;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    selectedNodeId = capturedId;
                    RefreshAll();
                });
            }
        }
    }

    private void BuildNodeButtons_FallbackVertical()
    {
        var list = sinkManager.GetNodesForBranch(activeBranch);
        bool lockedOut = IsBranchLockedOut(activeBranch);

        for (int i = 0; i < list.Count; i++)
        {
            var node = list[i];
            if (node == null) continue;

            var btn = Instantiate(nodeButtonPrefab, nodeButtonContainer);
            spawnedButtons.Add(btn);

            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                string status =
                    sinkManager.IsPurchased(node.id) ? "OWNED" :
                    sinkManager.CanPurchase(node.id) ? "" :
                    "LOCKED";

                string techniqueTag = node.isTechnique ? " TECH" : "";
                label.SetText($"T{node.tier}{techniqueTag}  {node.displayName}  {status}".Trim());
            }

            btn.interactable = !lockedOut;

            string capturedId = node.id;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                selectedNodeId = capturedId;
                RefreshAll();
            });
        }
    }


    private void ClearSpawnedButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                Destroy(spawnedButtons[i].gameObject);
        }
        spawnedButtons.Clear();
    }

    private void AutoSelectFirstNodeIfNeeded()
    {
        if (sinkManager == null) return;

        if (!string.IsNullOrWhiteSpace(selectedNodeId))
        {
            var existing = sinkManager.GetNode(selectedNodeId);
            if (existing != null && existing.branch == activeBranch) return;
        }

        var branchNodes = sinkManager.GetNodesForBranch(activeBranch);
        selectedNodeId = (branchNodes.Count > 0) ? branchNodes[0].id : null;
    }

    // --------------------------
    // Details + buttons
    // --------------------------

    private void RefreshAll()
    {
        RefreshCommittedText();
        RefreshTabInteractivity();
        RefreshDetailsPanel();
        RefreshActionButtons();
        RefreshSinkHudSprite();
    }

    private void RefreshCommittedText()
    {
        if (committedText == null || sinkManager == null) return;

        if (sinkManager.CurrentSinkType == SinkManager.SinkType.Basic)
            committedText.text = "Sink: Basic (choose a branch to commit)";
        else
            committedText.text = $"Sink: {sinkManager.CurrentSinkType}";
    }

    private void RefreshTabInteractivity()
    {
        if (sinkManager == null) return;

        // Tabs should be disabled if you’re committed to a different sink
        SetTabInteractable(powerWasherTabButton, !IsBranchLockedOut(SinkManager.SinkType.PowerWasher));
        SetTabInteractable(washBasinTabButton, !IsBranchLockedOut(SinkManager.SinkType.WashBasin));
        SetTabInteractable(dishwasherTabButton, !IsBranchLockedOut(SinkManager.SinkType.Dishwasher));
    }

    private void SetTabInteractable(Button b, bool interactable)
    {
        if (b == null) return;
        b.interactable = interactable;
    }

    private void RefreshDetailsPanel()
    {
        if (sinkManager == null) return;

        var node = sinkManager.GetNode(selectedNodeId);

        if (node == null)
        {
            if (nodeNameText) nodeNameText.text = "";
            if (nodeDescText) nodeDescText.text = "";
            if (nodeLoreText) nodeLoreText.text = "";
            if (nodeCostText) nodeCostText.text = "";
            if (nodeIconImage) nodeIconImage.gameObject.SetActive(false);
            return;
        }

        if (nodeNameText) nodeNameText.text = node.displayName;
        if (nodeDescText) nodeDescText.text = node.description ?? "";
        if (nodeLoreText) nodeLoreText.text = node.loreDescription ?? "";

        if (nodeCostText)
            nodeCostText.text = node.cost > 0f
                ? $"Cost: {BigNumberFormatter.FormatMoney((double)node.cost)}"
                : "Cost: $0";

        if (nodeIconImage != null)
        {
            if (nodeIconImage.sprite != null)
                nodeIconImage.gameObject.SetActive(true);
            else
                nodeIconImage.gameObject.SetActive(false);
        }
    }

    private void RefreshActionButtons()
    {
        if (purchaseButton == null || resetButton == null || sinkManager == null) return;

        var node = sinkManager.GetNode(selectedNodeId);

        // Purchase button
        var purchaseLabel = purchaseButton.GetComponentInChildren<TMP_Text>();
        bool branchLockedOut = IsBranchLockedOut(activeBranch);

        if (node == null || branchLockedOut)
        {
            purchaseButton.interactable = false;
            if (purchaseLabel) purchaseLabel.SetText("Locked");
        }
        else if (sinkManager.IsPurchased(node.id))
        {
            purchaseButton.interactable = false;
            if (purchaseLabel) purchaseLabel.SetText("Purchased");
        }
        else
        {
            bool canBuy = sinkManager.CanPurchase(node.id);

            float wallet = (scoreManager != null) ? scoreManager.GetTotalProfit() : 0f;
            bool canPay = wallet >= node.cost;

            purchaseButton.interactable = canBuy && canPay;

            if (purchaseLabel != null)
            {
                if (!canBuy) purchaseLabel.SetText("Locked");
                else purchaseLabel.SetText($"Buy {BigNumberFormatter.FormatMoney((double)node.cost)}");
            }
        }

        // Reset button
        float refund = sinkManager.GetRefundAmount();
        var resetLabel = resetButton.GetComponentInChildren<TMP_Text>();

        bool canReset = sinkManager.CurrentSinkType != SinkManager.SinkType.Basic || sinkManager.GetPurchasedNodeIds().Count > 0;
        resetButton.interactable = canReset;

        if (resetLabel != null)
        {
            if (!canReset) resetLabel.SetText("Reset (No sink chosen)");
            else resetLabel.SetText($"Sell Sink ({BigNumberFormatter.FormatMoney((double)refund)} refund)");
        }
    }

    private void RefreshSinkHudSprite()
    {
        if (sinkHudImage == null || sinkManager == null) return;

        var sprite = sinkManager.GetCurrentSinkSprite();
        if (sprite != null) sinkHudImage.sprite = sprite;
    }

    // --------------------------
    // Button handlers
    // --------------------------

    private void OnPurchaseClicked()
    {
        if (sinkManager == null) return;
        if (string.IsNullOrWhiteSpace(selectedNodeId)) return;

        sinkManager.TryPurchase(selectedNodeId);

        // UI refresh comes from events too, but do it here to feel snappy.
        RefreshAll();
        BuildNodeButtons();
    }

    private void OnResetClicked()
    {
        if (sinkManager == null) return;

        sinkManager.ResetToBasicAndRefund();

        // After reset, keep the player on PowerWasher tab by default
        activeBranch = SinkManager.SinkType.PowerWasher;
        selectedNodeId = null;

        BuildNodeButtons();
        AutoSelectFirstNodeIfNeeded();
        RefreshAll();
    }
}
