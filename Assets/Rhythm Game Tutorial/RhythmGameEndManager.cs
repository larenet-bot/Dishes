using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Listens for NoteSpawner.OnChartCompleted, computes a simple rank from MiniScoreManager results,
/// awards a profit reward scaled by rank, and shows a results panel.
/// Attach this to a GameObject in the scene and hook the UI references in inspector.
/// </summary>
public class RhythmGameEndManager : MonoBehaviour
{
    public GameObject rhythmMiniGame;
    public GameObject mainGameUI;
    public bool isOnCooldown = false;

    [Header("UI References")]
    public GameObject resultsPanel; // root panel to toggle
    public TMP_Text rankText;
    public TMP_Text scoreText;
    public TMP_Text perfectText;
    public TMP_Text goodText;
    public TMP_Text badText;
    public TMP_Text missText;
    public TMP_Text rewardText;

    [Header("Rank thresholds (fraction of perfect score)")]
    [Range(0f, 1f)] public float sThreshold = 0.95f;
    [Range(0f, 1f)] public float aThreshold = 0.85f;
    [Range(0f, 1f)] public float bThreshold = 0.70f;
    [Range(0f, 1f)] public float cThreshold = 0.50f;

    [Header("Multipliers per rank")]
    public float sMultiplier = 2.0f;
    public float aMultiplier = 1.75f;
    public float bMultiplier = 1.5f;
    public float cMultiplier = 1.25f;
    public float dMultiplier = 1.0f;

    [Header("Reward settings")]
    [Tooltip("Minimum accuracy used when dividing — prevents divide-by-zero / enormous rewards.")]
    [Range(0.0001f, 0.1f)] public float minAccuracy = 0.01f;

    [Tooltip("Optional reference to a RhythmMiniGame toggle component; if set, will call ToggleMiniGame() after showing results (optional).")]
    public RhythmMiniGameToggle toggleRef;

    void OnEnable()
    {
        NoteSpawner.OnChartCompleted += HandleChartCompleted;
    }

    void OnDisable()
    {
        NoteSpawner.OnChartCompleted -= HandleChartCompleted;
    }

    private void HandleChartCompleted()
    {
        ShowResults();
    }

    private void ShowResults()
    {
        if (resultsPanel == null)
        {
            Debug.LogWarning("[RhythmGameEndManager] resultsPanel not assigned.");
            return;
        }

        // Compute totals
        int perfects = MiniScoreManager.Perfects;
        int goods = MiniScoreManager.Goods;
        int bads = MiniScoreManager.Bads;
        int misses = MiniScoreManager.Misses;
        int totalNotes = NoteSpawner.LastChartNoteCount;
        if (totalNotes <= 0)
            totalNotes = perfects + goods + bads + misses;

        int score = MiniScoreManager.Score;
        int maxScore = totalNotes * MiniScoreManager.perfectValue;
        float fraction = (maxScore > 0) ? (float)score / (float)maxScore : 0f;

        // Determine rank
        string rank;
        float multiplier;
        if (fraction >= sThreshold) { rank = "S"; multiplier = sMultiplier; }
        else if (fraction >= aThreshold) { rank = "A"; multiplier = aMultiplier; }
        else if (fraction >= bThreshold) { rank = "B"; multiplier = bMultiplier; }
        else if (fraction >= cThreshold) { rank = "C"; multiplier = cMultiplier; }
        else { rank = "D"; multiplier = dMultiplier; }

        // Compute base value. Prefer ProfitRate.AverageProfit when available; otherwise fallback to profit-per-dish * count.
        float baseValue = 0f;
        if (ProfitRate.Instance != null)
            baseValue = ProfitRate.Instance.AverageProfit;
        else if (ScoreManager.Instance != null)
            baseValue = ScoreManager.Instance.GetProfitPerDish() * ScoreManager.Instance.GetDishCountIncrement();

        // Current player profit (to be added to the computed reward)
        float currentProfit = (ScoreManager.Instance != null) ? ScoreManager.Instance.GetTotalProfit() : 0f;

        // NEW FORMULA (user requested "divide by accuracy"):
        // reward = (totalNotes / accuracy) * baseValue * rankMultiplier + currentProfit
        // Protect against divide-by-zero / runaway rewards by clamping accuracy to a small minimum (configurable).
        float accuracy = fraction;
        float safeAccuracy = Mathf.Max(accuracy, minAccuracy);
        float reward = ((float)totalNotes / safeAccuracy) * baseValue * multiplier + currentProfit;
        reward = Mathf.Max(0f, reward);

        // Award reward via ScoreManager so UI/profit-rate trackers receive pending adjustments correctly
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddBubbleReward(reward);
        }
        else
        {
            Debug.Log($"[RhythmGameEndManager] Reward ({reward}) computed but ScoreManager.Instance == null.");
        }

        // Populate UI
        rankText.text = $"Rank: {rank}";
        scoreText.text = $"Score: {score}";
        perfectText.text = $"Perfect: {perfects}";
        goodText.text = $"Good: {goods}";
        badText.text = $"Bad: {bads}";
        missText.text = $"Miss: {misses}";
        rewardText.text = $"Reward: {BigNumberFormatter.FormatMoney(reward)}";

        // Show results panel
        resultsPanel.SetActive(true);

        // Optional: close minigame toggles (if user provided a reference)
        if (toggleRef != null)
        {
            // Delay call one frame so UI is visible before toggling UI state if desired.
            // toggleRef.ToggleMiniGame(); // uncomment if you want automatic toggle
        }

        // Reset mini-score so next run starts cleanly (UI will update via events)
        MiniScoreManager.ResetScore();
    }
    public void ContinueAndExit()
    {
        // Hide results
        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        // Disable minigame
        if (rhythmMiniGame != null)
            rhythmMiniGame.SetActive(false);

        // Re-enable main game
        if (mainGameUI != null)
            mainGameUI.SetActive(true);

        // Trigger cooldown
        StartCoroutine(MinigameCooldownRoutine());
    }

    private IEnumerator MinigameCooldownRoutine()
    {
        isOnCooldown = true;

        // however long you want the cooldown to be
        float cooldownTime = 10f;
        yield return new WaitForSeconds(cooldownTime);

        isOnCooldown = false;
    }

    // Hook this to a "Close" button on the results panel
    public void CloseResults()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);
    }
}