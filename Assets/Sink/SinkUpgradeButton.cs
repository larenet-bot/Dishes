using UnityEngine;

/// <summary>
/// Controls the sink upgrade button's one-time discovery pulse.
/// Keep the button active; CanvasGroup handles visibility and input.
/// </summary>
public class SinkUpgradeButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SinkManager sinkManager;
    [SerializeField] private CanvasGroup upgradeButtonGroup;

    [Header("Attention Pulse")]
    [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.25f;
    [SerializeField, Min(0.1f)] private float pulseSeconds = 1.5f;

    private int displayedState = -1;
    private float pulseElapsed;

    private void Awake()
    {
        if (upgradeButtonGroup == null) upgradeButtonGroup = GetComponent<CanvasGroup>();
        if (upgradeButtonGroup == null)
        {
            Debug.LogWarning("[SinkUpgradeButton] Assign the sink upgrade button CanvasGroup.", this);
            enabled = false;
            return;
        }
        ApplyVisualState(0, 0f);
    }

    private void Update()
    {
        if (sinkManager == null) sinkManager = SinkManager.Instance;
        if (sinkManager == null) return;

        int state = sinkManager.SinkButtonDiscoveryState;
        if (sinkManager.CurrentSinkType != SinkManager.SinkType.Basic) state = 2;

        if (state == 0 && CanAffordAnySink())
        {
            sinkManager.MarkSinkButtonRevealed();
            state = sinkManager.SinkButtonDiscoveryState;
        }

        if (state != displayedState)
        {
            displayedState = state;
            pulseElapsed = 0f;
        }

        float alpha = state == 0 ? 0f : 1f;
        if (state == 1)
        {
            float duration = Mathf.Max(0.1f, pulseSeconds);
            float wave = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * pulseElapsed / duration);
            alpha = Mathf.Lerp(Mathf.Clamp01(minimumAlpha), 1f, wave);
            pulseElapsed = Mathf.Repeat(pulseElapsed + Time.unscaledDeltaTime, duration);
        }
        ApplyVisualState(state, alpha);
    }

    private bool CanAffordAnySink()
    {
        ScoreManager score = ScoreManager.Instance;
        if (score == null || sinkManager.nodes == null) return false;
        float wallet = score.GetTotalProfit();
        foreach (SinkManager.SinkNode node in sinkManager.nodes)
        {
            if (node == null || !node.unlocksSinkType || node.hiddenFromMenu) continue;
            if (node.branch != SinkManager.SinkType.PowerWasher &&
                node.branch != SinkManager.SinkType.WashBasin &&
                node.branch != SinkManager.SinkType.Dishwasher) continue;
            if (wallet >= node.cost && sinkManager.CanPurchase(node.id)) return true;
        }
        return false;
    }

    private void ApplyVisualState(int state, float alpha)
    {
        upgradeButtonGroup.alpha = alpha;
        upgradeButtonGroup.interactable = state != 0;
        upgradeButtonGroup.blocksRaycasts = state != 0;
    }
}
