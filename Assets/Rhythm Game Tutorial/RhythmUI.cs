using UnityEngine;
using UnityEngine.UI;
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

    void UpdateScore()
    {
        scoreText.text = "Score: " + MiniScoreManager.Score.ToString();
    }

    public void ShowFeedback(string msg)
    {
        feedbackText.text = msg;
        feedbackTimer = feedbackDuration;
    }

    void Update()
    {
        if (feedbackTimer > 0)
        {
            feedbackTimer -= Time.deltaTime;

            float alpha = feedbackTimer / feedbackDuration;
            feedbackText.color = new Color(feedbackText.color.r, feedbackText.color.g, feedbackText.color.b, alpha);
        }
    }
}
