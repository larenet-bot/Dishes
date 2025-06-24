using UnityEngine;

public class DishClicker : MonoBehaviour
{
    [Header("Cleaning Settings")]
    public int clicksRequired = 3;

    [Header("References")]
    public DishVisual dishVisual;

    [Header("Auto Clicker")]
    public bool autoClickEnabled = false;
    public float autoClickInterval = 1f;

    private int currentClicks = 0;
    private float autoClickTimer = 0f;

    private void Start()
    {
        dishVisual?.SetStage(0);
    }

    private void Update()
    {
        if (autoClickEnabled)
        {
            autoClickTimer += Time.deltaTime;
            if (autoClickTimer >= autoClickInterval)
            {
                autoClickTimer = 0f;
                ProcessClick();
            }
        }
    }

    public void OnDishClicked()
    {
        ProcessClick();
    }

    private void ProcessClick()
    {
        currentClicks++;
        dishVisual?.SetStage(currentClicks);

        if (currentClicks >= clicksRequired)
        {
            currentClicks = 0;
            dishVisual?.SetStage(0);

            ScoreManager.Instance.AddScore(); // Use upgraded profitPerDish from ScoreManager
        }
    }

    public void EnableAutoClick() => autoClickEnabled = true;
}
