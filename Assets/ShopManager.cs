using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Settings")]
    public Transform shopContainer;        // UI parent for all shop items
    public GameObject shopItemPrefab;      // prefab for each item row

    [Header("Shop Inventory")]
    public List<ShopItemData> availableItems = new List<ShopItemData>(); // assigned in Inspector

    private List<ShopItemUI> shopUIItems = new List<ShopItemUI>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Start()
    {
        BuildShopUI();
    }

    private void BuildShopUI()
    {
        foreach (Transform child in shopContainer)
            Destroy(child.gameObject);

        foreach (ShopItemData data in availableItems)
        {
            GameObject newItem = Instantiate(shopItemPrefab, shopContainer);
            ShopItemUI ui = newItem.GetComponent<ShopItemUI>();
            ui.Initialize(data);
            shopUIItems.Add(ui);
        }
    }

    public bool TryPurchase(ShopItemData data)
    {
        if (ScoreManager.Instance.GetTotalProfit() >= data.cost)
        {
            ScoreManager.Instance.SubtractProfit(data.cost);
            data.ApplyEffect();

            data.cost += data.costIncrease; // scale price
            data.timesPurchased++;

            // refresh UI
            foreach (var item in shopUIItems)
                item.Refresh();

            Debug.Log($"{data.itemName} purchased for ${data.cost - data.costIncrease}");
            return true;
        }

        Debug.Log($"Not enough profit to buy {data.itemName}.");
        return false;
    }

    internal void TryPurchase(ShopItem shopItem)
    {
        throw new NotImplementedException();
    }
}
