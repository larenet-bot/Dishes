using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages a dish-cleaning button with stages. 
/// Tracks dish count and profit separately, and unlocks two independent modifiers:
/// one for counting more dishes per clean, and one for earning more profit per dish.
/// </summary>
public class DishCounterButton : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text counterText;
    public TMP_Text profitText;
    public TMP_Text profitRateText; //test
    public Button dishButton;
    public Image dishImage;
    public Sprite[] dishStages;

    [Header("Click Logic")]
    public int clicksRequired = 3;
    private int currentClicks = 0;
    private int dishStageIndex = 0;

    [Header("Counters")]
    private int count = 0;
    private float profit = 0f;
    public float profitRate = 0f; //test

    [Header("Initialized variables for profitRate")] //test
    private float previousProfit = 0f; //test
    private float currentProfit = 0f; //test

    [Tooltip("How many dishes are added to count per clean.")]
    public int dishCountIncrement = 1;

    [Tooltip("How much profit is made per dish cleaned.")]
    public float profitPerDish = 1f;

    [Header("Modifier Buttons")]
    public Button modifierButton2; // Affects dish count
    public Button modifierButton5; // Affects profit

    [Header("Unlock Thresholds (by profit)")]
    public float unlockCountModifierAt = 10f;
    public float unlockProfitModifierAt = 25f;

    [Header("Auto Clicker Settings")]
    [Tooltip("Enable automatic clicking.")]
    public bool autoClickEnabled = false;

    [Tooltip("Time in seconds between auto clicks.")]
    public float autoClickInterval = 1f;

    private float autoClickTimer = 0f;
    void Start()
    {
        UpdateUI();
        UpdateModifierButtonStates();
        UpdateDishVisual();
    }
    void Update()
    {
        previousProfit = currentProfit; //test
        currentProfit = profit; //test
        profitRate = (profit - currentProfit)/Time.deltaTime; //test
        if (autoClickEnabled)
        {
            autoClickTimer += Time.deltaTime;
            if (autoClickTimer >= autoClickInterval)
            {
                autoClickTimer = 0f;
                OnDishClicked();
            }
        }
    }
    public void OnDishClicked()
    {
        currentClicks++;
        dishStageIndex = Mathf.Clamp(currentClicks, 0, dishStages.Length - 1);
        UpdateDishVisual();

        if (currentClicks >= clicksRequired)
        {
            count += dishCountIncrement;
            profit += dishCountIncrement * profitPerDish;

            currentClicks = 0;
            dishStageIndex = 0;

            UpdateUI();
            UpdateModifierButtonStates();
            UpdateDishVisual();
        }
    }

    // Modifier Button 1: Boost dish count increment
    public void SetDishCountIncrementIfUnlocked(int newCountIncrement)
    {
        if (profit >= unlockCountModifierAt)
        {
            dishCountIncrement = newCountIncrement;
            Debug.Log("Dish count modifier unlocked: +" + newCountIncrement);
        }
        else
        {
            Debug.Log($"Dish count modifier locked — need ${unlockCountModifierAt}, have ${profit}");
        }
    }

    // Modifier Button 2: Boost profit per dish
    public void SetProfitPerDishIfUnlocked(float newProfitPerDish)
    {
        if (profit >= unlockProfitModifierAt)
        {
            profitPerDish = newProfitPerDish;
            Debug.Log("Profit modifier unlocked: $" + newProfitPerDish + " per dish");
        }
        else
        {
            Debug.Log($"Profit modifier locked — need ${unlockProfitModifierAt}, have ${profit}");
        }
    }

    public void OnModifierCountClicked() => SetDishCountIncrementIfUnlocked(2);
    public void OnModifierProfitClicked() => SetProfitPerDishIfUnlocked(3f);

    private void UpdateModifierButtonStates()
    {
        if (modifierButton2 != null)
            modifierButton2.interactable = profit >= unlockCountModifierAt;

        if (modifierButton5 != null)
            modifierButton5.interactable = profit >= unlockProfitModifierAt;
    }

    private void UpdateDishVisual()
    {
        if (dishStages != null && dishStages.Length > 0 && dishImage != null)
        {
            dishImage.sprite = dishStages[dishStageIndex];
        }
    }
    public void EnableAutoClicker()
    {
        autoClickEnabled = true;
        Debug.Log("Auto clicker enabled!");
    }

    private void UpdateUI()
    {
        counterText.text = $"Dishes: {count}";
        profitText.text = $"Profit: ${profit:0.00}";
        profitRateText.text = $"${profitRate}/second";
    }
}
