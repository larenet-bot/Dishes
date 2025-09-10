using System.Collections;
using TMPro;
using UnityEngine;

public class ProfitRate : MonoBehaviour
{
    public static ProfitRate Instance { get; private set; }
    [Header("UI References")]
    public TMP_Text profitRateText;

    float timer = 0f;

    [Header("Profit Rate Settings")]
    [SerializeField] float updateTime = 1f; // Adjustable in Inspector

    float previousProfit = 0f;
    float averageProfit = 0f;
    float currentProfit = 0f;

    //private Coroutine tempDisplayCoroutine;

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

        if (timer >= updateTime)
        {
            Debug.Log($"Average Profit pre calculation: {averageProfit}");
            Debug.Log($"Current Profit pre calculation: {currentProfit}"); 
            Debug.Log($"Previous Profit pre calculation: {previousProfit}");
            averageProfit = ((currentProfit - previousProfit)
                             + ScoreManager.PendingProfitAdjustment
                             - ScoreManager.PendingRewardAdjustment) / updateTime;
            Debug.Log($"Average Profit post calculation: {averageProfit}");
            previousProfit = currentProfit;
            Debug.Log($"Previous Profit post calculation: {previousProfit}");

            timer = 0f;
            ScoreManager.PendingProfitAdjustment = 0f;
            ScoreManager.PendingRewardAdjustment = 0f;
            if (averageProfit < 0)
                averageProfit = 0f;
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        profitRateText.text = $"${averageProfit:0.00/second}";
    }

    public float AverageProfit => averageProfit;

  
    //public void ShowTemporaryProfitRate(float value, float duration = 1f)
    //{
    //    if (tempDisplayCoroutine != null)
    //        StopCoroutine(tempDisplayCoroutine);
    //    tempDisplayCoroutine = StartCoroutine(ShowTempProfitRateCoroutine(value, duration));
    //}

    private IEnumerator ShowTempProfitRateCoroutine(float value, float duration)
    {
        profitRateText.text = $"${value:0.00/second}";
        yield return new WaitForSeconds(duration);
        UpdateUI();
    }
}