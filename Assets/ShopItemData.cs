using UnityEngine;

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/Item")]
public class ShopItemData : ScriptableObject
{
    public string itemName;
    public float cost;
    public float costIncrease = 10f;

    [Header("Effect Settings")]
    public ShopItemEffectType effectType;
    public float effectValue;

    [Header("Unlockable Object")]
    public GameObject unlockableObject; // this is assigned manually in the scene later

    [HideInInspector]
    public int timesPurchased = 0;

    public void ApplyEffect()
    {
        switch (effectType)
        {
            case ShopItemEffectType.IncreaseProfitPerDish:
                ScoreManager.Instance.profitPerDish += effectValue;
                break;
            case ShopItemEffectType.IncreaseDishCount:
                ScoreManager.Instance.dishCountIncrement += Mathf.RoundToInt(effectValue);
                break;
            case ShopItemEffectType.UnlockObject:
                if (unlockableObject != null)
                    unlockableObject.SetActive(true);
                break;
        }
    }

    public enum ShopItemEffectType
    {
        None,
        IncreaseProfitPerDish,
        IncreaseDishCount,
        UnlockObject
    }
}
