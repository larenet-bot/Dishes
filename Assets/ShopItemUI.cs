using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text costText;
    public Button buyButton;

    private ShopItemData itemData;

    public void Initialize(ShopItemData data)
    {
        itemData = data;

        if (nameText != null) nameText.text = itemData.itemName;
        if (costText != null) costText.text = $"${itemData.cost}";

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => ShopManager.Instance.TryPurchase(itemData));
        }

        Refresh();
    }

    public void Refresh()
    {
        if (itemData != null)
        {
            if (costText != null) costText.text = $"${itemData.cost}";
            if (buyButton != null)
                buyButton.interactable = ScoreManager.Instance.GetTotalProfit() >= itemData.cost;
        }
    }
}
