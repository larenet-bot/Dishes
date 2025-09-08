using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    // Called by ShopItem button
    public void TryPurchase(ShopItem item)
    {
        if (ScoreManager.Instance.GetTotalProfit() >= item.cost)
        {
            ScoreManager.Instance.SubtractProfit(item.cost);
            item.ApplyPurchase();

            Debug.Log($"{item.itemName} purchased for {item.cost}!");
        }
        else
        {
            Debug.Log($"Not enough profit to buy {item.itemName} (need {item.cost}).");
        }
    }

    // Optional: refresh all items when shop opens
    public ShopItem[] shopItems; // assign all items in Inspector

    public void OpenShop()
    {
        foreach (ShopItem item in shopItems)
            item.RefreshButton();
    }
}
