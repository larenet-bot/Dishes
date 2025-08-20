using TMPro;
using UnityEngine;

public class ProfitRate : MonoBehaviour
{
    public static ProfitRate Instance { get; private set; }
    [Header("UI References")]
    public TMP_Text profitRateText;

    float timer = 0f;
    float lastUpdate = 3f;
    float previousProfit = 0f;
    float averageProfit = 0f;
    float currentProfit = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        if (ScoreManager.Instance != null)
        {
            currentProfit = ScoreManager.Instance.GetTotalProfit();
            previousProfit = currentProfit;
        }
    }

    private void Update()
    {
        if (ScoreManager.Instance == null) return;

        currentProfit = ScoreManager.Instance.GetTotalProfit();
        timer += Time.deltaTime;

        if (timer >= lastUpdate)
        {
            averageProfit = (currentProfit - previousProfit) / lastUpdate;
            previousProfit = currentProfit;
            timer = 0f;
            if (averageProfit < 0)
            {
                averageProfit = 0f; // Ensure average profit doesn't go negative
            }
        }
        Debug.Log("Average profit in the last 3 seconds " + averageProfit);
        UpdateUI();
    }
    private void UpdateUI()
    {
        profitRateText.text = $"${averageProfit:0.00/second}";
    }

    public float AverageProfit => averageProfit;
}