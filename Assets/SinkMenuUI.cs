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
    private string selectedNodeId = null;

    private readonly List<Button> spawnedButtons = new List<Button>();

    private readonly List<RectTransform> spawnedTierRows = new List<RectTransform>();

    private Action<SinkManager.SinkType> onSinkTypeChangedHandler;
    private Action<string> onNodePurchasedHandler;
    private Action onSinkResetHandler;

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
            onSinkTypeChangedHandler = _ => RefreshAll();
            onNodePurchasedHandler = _ => RefreshAll();
            onSinkResetHandler = RefreshAll;

            sinkManager.OnSinkTypeChanged += onSinkTypeChangedHandler;
            sinkManager.OnNodePurchased += onNodePurchasedHandler;
            sinkManager.OnSinkReset += onSinkResetHandler;
        }

        CloseMenu();
    }

    private void OnDestroy()
    {
        if (sinkManager != null)
        {
            if (onSinkTypeChangedHandler != null) sinkManager.OnSinkTypeChanged -= onSinkTypeChangedHandler;
            if (onNodePurchasedHandler != null) sinkManager.OnNodePurchased -= onNodePurchasedHandler;
            if (onSinkResetHandler != null) sinkManager.OnSinkReset -= onSinkResetHandler;
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


    private enum NodeVisualState
    {
        Owned,
        Buyable,
        NoMoney,
        Locked
    }

    private SinkManager.SinkNode GetBranchUnlockNode(SinkManager.SinkType branch)
    {
        if (sinkManager == null) return null;
        var list = sinkManager.GetNodesForBranch(branch);
        for (int i = 0; i < list.Count; i++)
        {
            var n = list[i];
            if (n == null) continue;
            if (n.unlocksSinkType) return n;
        }
        return null;
    }

    private string GetNodeDisplayName(string nodeId)
    {
        if (sinkManager == null) return nodeId ?? "";
        var n = sinkManager.GetNode(nodeId);
        if (n != null && !string.IsNullOrWhiteSpace(n.displayName)) return n.displayName;
        return nodeId ?? "";
    }

    private List<string> GetMissingPrereqNames(SinkManager.SinkNode node)
    {
        var missing = new List<string>();
        if (node == null || node.requires == null) return missing;

        for (int i = 0; i < node.requires.Count; i++)
        {
            string reqId = node.requires[i];
            if (string.IsNullOrWhiteSpace(reqId)) continue;

            if (sinkManager != null && !sinkManager.IsPurchased(reqId))
                missing.Add(GetNodeDisplayName(reqId));
        }

        return missing;
    }

    private void GetLockInfo(SinkManager.SinkNode node, out string lockReason, out string requirements, out NodeVisualState state)
    {
        lockReason = "";
        requirements = "";
        state = NodeVisualState.Locked;

        if (sinkManager == null || node == null) return;

        if (sinkManager.IsPurchased(node.id))
        {
            state = NodeVisualState.Owned;
            return;
        }

        // Branch lockout (committed to another sink)
        if (sinkManager.CurrentSinkType != SinkManager.SinkType.Basic && sinkManager.CurrentSinkType != node.branch)
        {
            lockReason = $"Locked. Committed to {sinkManager.CurrentSinkType}.";
            state = NodeVisualState.Locked;
            return;
        }

        // Still basic sink: must unlock the branch first
        if (sinkManager.CurrentSinkType == SinkManager.SinkType.Basic && !node.unlocksSinkType)
        {
            var unlock = GetBranchUnlockNode(node.branch);
            lockReason = "Locked. Choose a sink first.";
            if (unlock != null) requirements = $"Requires: {unlock.displayName}";
            state = NodeVisualState.Locked;
            return;
        }

        // Missing prerequisite nodes
        var missing = GetMissingPrereqNames(node);
        if (missing.Count > 0)
        {
            lockReason = "Locked. Missing requirements.";
            requirements = "Requires: " + string.Join(", ", missing);
            state = NodeVisualState.Locked;
            return;
        }

        // Rule check (should be true if we passed above, but keep it explicit)
        bool canBuyRules = sinkManager.CanPurchase(node.id);
        if (!canBuyRules)
        {
            lockReason = "Locked.";
            state = NodeVisualState.Locked;
            return;
        }

        float wallet = (scoreManager != null) ? scoreManager.GetTotalProfit() : 0f;
        if (wallet < node.cost)
        {
            float need = Mathf.Max(0f, node.cost - wallet);
            lockReason = "Not enough profit.";
            requirements = $"Need {BigNumberFormatter.FormatMoney((double)need)} more.";
            state = NodeVisualState.NoMoney;
            return;
        }

        state = NodeVisualState.Buyable;
    }

    private string GetNodeStatusShort(SinkManager.SinkNode node)
    {
        if (sinkManager == null || node == null) return "";
        if (sinkManager.IsPurchased(node.id)) return "OWNED";

        GetLockInfo(node, out _, out _, out var state);

        switch (state)
        {
            case NodeVisualState.Buyable: return "BUY";
            case NodeVisualState.NoMoney: return "COST";
            default: return "LOCKED";
        }
    }

    private void ApplyNodeButtonVisual(Button btn, SinkManager.SinkNode node)
    {
        if (btn == null || node == null) return;

        GetLockInfo(node, out _, out _, out var state);

        float alpha = lockedAlpha;
        if (state == NodeVisualState.Buyable) alpha = canBuyAlpha;
        else if (state == NodeVisualState.NoMoney) alpha = noMoneyAlpha;
        else if (state == NodeVisualState.Owned) alpha = ownedAlpha;

        SetButtonAlpha(btn, alpha);
    }

    private void SetButtonAlpha(Button btn, float alpha)
    {
        if (btn == null) return;

        if (btn.targetGraphic != null)
        {
            Color c = btn.targetGraphic.color;
            c.a = alpha;
            btn.targetGraphic.color = c;
        }
        else
        {
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = alpha;
                img.color = c;
            }
        }

        float textAlpha = Mathf.Clamp01(alpha * 0.9f + 0.1f);
        var labels = btn.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == null) continue;
            Color tc = labels[i].color;
            tc.a = textAlpha;
            labels[i].color = tc;
        }
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
            spawnedTierRows.Add(row);

            // Optional: if you want row to stretch full width of content
            row.anchorMin = new Vector2(0f, row.anchorMin.y);
            row.anchorMax = new Vector2(1f, row.anchorMax.y);
            row.offsetMin = new Vector2(0f, row.offsetMin.y);
            row.offsetMax = new Vector2(0f, row.offsetMax.y);

            //// Sort nodes inside tier (techniques last, then name)
            //tierNodes.Sort((a, b) =>
            //{
            //    int tech = a.isTechnique.CompareTo(b.isTechnique);
            //    if (tech != 0) return tech;
            //    return string.Compare(a.displayName, b.displayName, StringComparison.Ordinal);
            //});

            for (int i = 0; i < tierNodes.Count; i++)
            {
                var node = tierNodes[i];
                if (node == null) continue;

                var btn = Instantiate(nodeButtonPrefab, row);
                spawnedButtons.Add(btn);

                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    string status = GetNodeStatusShort(node);

                    string techniqueTag = node.isTechnique ? " TECH" : "";
                    label.SetText($"T{node.tier}{techniqueTag}\n{node.displayName}\n{status}".Trim());
                }

                ApplyNodeButtonVisual(btn, node);

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

        ForceLayoutRefresh();
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
                string status = GetNodeStatusShort(node);

                string techniqueTag = node.isTechnique ? " TECH" : "";
                label.SetText($"T{node.tier}{techniqueTag}  {node.displayName}  {status}".Trim());
            }

            ApplyNodeButtonVisual(btn, node);

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
        // Destroy node buttons
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] == null) continue;

            spawnedButtons[i].gameObject.SetActive(false);
            Destroy(spawnedButtons[i].gameObject);
        }
        spawnedButtons.Clear();

        // Destroy tier rows we spawned last build
        HashSet<int> destroyedRowIds = new HashSet<int>();
        for (int i = 0; i < spawnedTierRows.Count; i++)
        {
            if (spawnedTierRows[i] == null) continue;

            destroyedRowIds.Add(spawnedTierRows[i].GetInstanceID());
            spawnedTierRows[i].gameObject.SetActive(false);
            Destroy(spawnedTierRows[i].gameObject);
        }
        spawnedTierRows.Clear();

        // Safety: remove any orphan TierRow_* left behind (from older versions / edge cases)
        if (nodeButtonContainer != null)
        {
            for (int i = nodeButtonContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = nodeButtonContainer.GetChild(i);
                if (child == null) continue;

                if (!child.name.StartsWith("TierRow_")) continue;
                if (destroyedRowIds.Contains(child.GetInstanceID())) continue;

                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
    }

    private void ForceLayoutRefresh()
    {
        if (nodeButtonContainer == null) return;

        // Ensures ContentSizeFitter/LayoutGroups update immediately so scrollbars behave.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(nodeButtonContainer as RectTransform);
        Canvas.ForceUpdateCanvases();
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
            if (nodeLockReasonText) nodeLockReasonText.text = "";
            if (nodeRequirementsText) nodeRequirementsText.text = "";
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

        // Lock reason / requirements
        GetLockInfo(node, out string lockReason, out string reqText, out _);

        if (nodeLockReasonText != null) nodeLockReasonText.text = lockReason;
        if (nodeRequirementsText != null) nodeRequirementsText.text = reqText;

        // Fallback: if you didn't assign the optional lock text fields, append to lore.
        if (nodeLockReasonText == null && nodeRequirementsText == null && nodeLoreText != null)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(node.loreDescription))
                sb.Append(node.loreDescription.Trim());

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

            nodeLoreText.text = sb.ToString();
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
