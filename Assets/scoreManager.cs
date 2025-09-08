using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    public TMP_Text dishCountText;
    public TMP_Text profitText;

    [Header("Tracking")]
    private int totalDishes = 0;
    private float totalProfit = 0f;

    [Header("Dish Modifiers")]
    public int dishCountIncrement = 1;
    public float profitPerDish = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        UpdateUI();
        NotifyProfitChanged();
    }

    // Event for profit changes
    public delegate void ProfitChanged();
    public static event ProfitChanged OnProfitChanged;

    private void NotifyProfitChanged()
    {
        OnProfitChanged?.Invoke();
    }

    // Add score from clicks or actions
    public void AddScore()
    {
        totalDishes += dishCountIncrement;
        totalProfit += dishCountIncrement * profitPerDish;

        UpdateUI();
        NotifyProfitChanged();
    }

    // Add profit from bubbles or other sources
    public void AddBubbleReward(float reward)
    {
        totalProfit += reward;
        UpdateUI();
        NotifyProfitChanged();
    }

    // Subtract profit when purchasing
    public void SubtractProfit(float amount)
    {
        totalProfit = Mathf.Max(0, totalProfit - amount);
        UpdateUI();
        NotifyProfitChanged();
    }

    // --- Getters ---
    public int GetTotalDishes() => totalDishes;
    public float GetTotalProfit() => totalProfit;
    public float GetProfitPerDish() => profitPerDish;
    public int GetDishCountIncrement() => dishCountIncrement;

    private void UpdateUI()
    {
        if (dishCountText != null)
            dishCountText.text = $"Dishes: {totalDishes}";

        if (profitText != null)
            profitText.text = $"Profit: ${totalProfit:0.00}";
    }
}
