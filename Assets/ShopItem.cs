using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItem : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemName;
    public float cost;
    public float costIncrease = 10f;       // increase after each purchase
    public GameObject unlockableObject;    // optional, only first purchase

    [Header("UI References")]
    [Header("Effect Settings")]
    public ShopItemEffectType effectType = ShopItemEffectType.None;
    public float effectValue = 0f; // amount of increase if applicable

    public TMP_Text nameText;
    public TMP_Text costText;
    public Button buyButton;

    private bool purchased = false;

    private void Start()
    {
        // Set initial text
        if (nameText != null) nameText.text = itemName;
        if (costText != null) costText.text = $"${cost}";

        if (buyButton != null)
            buyButton.onClick.AddListener(() => ShopManager.Instance.TryPurchase(this));

        // Force initial check in case profit already exists
        RefreshButton();
    }

    private void OnEnable()
    {
        ScoreManager.OnProfitChanged += RefreshButton;
        RefreshButton(); // refresh whenever item becomes active
    }

    private void OnDisable()
    {
        ScoreManager.OnProfitChanged -= RefreshButton;
    }

    // Call to apply purchase
    public void ApplyPurchase()
    {
        // Unlockable shows only on first purchase
        if (!purchased && unlockableObject != null)
        {
            unlockableObject.SetActive(true);
            purchased = true;
        }

        // Apply the item effect
        switch (effectType)
        {
            case ShopItemEffectType.IncreaseProfitPerDish:
                ScoreManager.Instance.profitPerDish += effectValue;
                break;
            case ShopItemEffectType.IncreaseDishCount:
                ScoreManager.Instance.dishCountIncrement += Mathf.RoundToInt(effectValue);
                break;
            case ShopItemEffectType.UnlockObject:
                // Already handled with unlockableObject above
                break;
        }

        // Increase cost for next purchase
        cost += costIncrease;
        if (costText != null)
            costText.text = $"${cost}";

        RefreshButton();
    }


    // Updates button interactability
    public void RefreshButton()
    {
        if (buyButton != null)
            buyButton.interactable = ScoreManager.Instance.GetTotalProfit() >= cost;
    }
    public enum ShopItemEffectType
    {
        None,
        IncreaseProfitPerDish,
        IncreaseDishCount,
        UnlockObject
    }

}
