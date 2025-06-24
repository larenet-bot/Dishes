using UnityEngine;

public class ShopToggle : MonoBehaviour
{
    [Tooltip("Reference to the ScrollView GameObject")]
    public GameObject shopScrollView;

    void Start()
    {
        if (shopScrollView != null)
            shopScrollView.SetActive(false); // Ensures panel is hidden at startup
    }
    public void ToggleShop()
    {
        if (shopScrollView != null)
        {
            bool isActive = shopScrollView.activeSelf;
            shopScrollView.SetActive(!isActive);
        }
    }
}
