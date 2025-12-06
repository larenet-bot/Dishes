using UnityEngine;
using TMPro;

public class UI_RhythmHUD : MonoBehaviour
{
    public static UI_RhythmHUD Instance;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI feedbackText;

    private float feedbackTimer = 0f;
    private float feedbackDuration = 0.5f;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        MiniScoreManager.OnScoreUpdated += UpdateScore;
    }

    void OnDisable()
    {
        MiniScoreManager.OnScoreUpdated -= UpdateScore;
    }

    // Called whenever score changes
    void UpdateScore()
    {
        scoreText.text = "Score: " + MiniScoreManager.Score.ToString();
    }

    // Called by HitWindow / LaneKey
    public void ShowFeedback(string msg)
    {
        feedbackText.text = msg;
        feedbackText.color = new Color(feedbackText.color.r, feedbackText.color.g, feedbackText.color.b, 1f);
        feedbackTimer = feedbackDuration;
    }

    void Update()
    {
        if (feedbackTimer > 0)
        {
            feedbackTimer -= Time.deltaTime;

            float alpha = feedbackTimer / feedbackDuration;

            feedbackText.color = new Color(
                feedbackText.color.r,
                feedbackText.color.g,
                feedbackText.color.b,
                alpha
            );
        }
    }
}
