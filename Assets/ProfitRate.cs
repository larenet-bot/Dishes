using System.Collections;
using TMPro;
using UnityEngine;

public class ProfitRate : MonoBehaviour
{
    public static ProfitRate Instance { get; private set; }

    [Header("UI References")]
    public TMP_Text profitRateText;

    [Header("Profit Rate Settings")]
    [SerializeField] private float updateTime = 1f;

    private float timer = 0f;
    private double averageProfit = 0d;

    //private Coroutine tempDisplayCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ResetBaseline();
    }

    private void Update()
    {
        if (ScoreManager.Instance == null)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= updateTime)
        {
            double elapsed = System.Math.Max(0.0001d, (double)timer);
            double incomeThisPeriod = ScoreManager.Instance.ConsumeProfitRateIncome();

            averageProfit = incomeThisPeriod / elapsed;

            if (double.IsNaN(averageProfit) || double.IsInfinity(averageProfit) || averageProfit < 0d)
            {
                averageProfit = 0d;
            }

            timer = 0f;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (profitRateText != null)
        {
            profitRateText.text = $"{BigNumberFormatter.FormatMoney(averageProfit)}/second";
        }
    }

    public void ResetBaseline()
    {
        timer = 0f;
        averageProfit = 0d;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ClearProfitRateIncome();
        }

        UpdateUI();
    }

    public float AverageProfit => ToSafeFloat(averageProfit);
    public double AverageProfitDouble => averageProfit;

    private static float ToSafeFloat(double value)
    {
        if (double.IsNaN(value) || value <= 0d)
        {
            return 0f;
        }

        if (double.IsPositiveInfinity(value) || value > float.MaxValue)
        {
            return float.MaxValue;
        }

        return (float)value;
    }

    //public void ShowTemporaryProfitRate(float value, float duration = 1f)
    //{
    //    if (tempDisplayCoroutine != null)
    //        StopCoroutine(tempDisplayCoroutine);
    //    tempDisplayCoroutine = StartCoroutine(ShowTempProfitRateCoroutine(value, duration));
    //}

    private IEnumerator ShowTempProfitRateCoroutine(float value, float duration)
    {
        profitRateText.text = $"${value:0.00}/second";
        yield return new WaitForSeconds(duration);
        UpdateUI();
    }
}
