using UnityEngine;
using UnityEngine.EventSystems;


/// Manages both shop toggling and hover panel visibility.
/// Attach to the UI element that handles hover, and assign ScrollView if also managing shop.

public class ShopAndHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Panel Settings")]
    [Tooltip("Panel shown when mouse hovers over this object.")]
    public GameObject HoverPanel;

    [Header("Shop Panel Settings")]
    [Tooltip("Reference to the ScrollView shop panel.")]
    public GameObject shopScrollView;

    private void Start()
    {
        // Ensure hover panel is hidden at launch
        if (HoverPanel != null)
        {
            HoverPanel.SetActive(false);
        }

        // Ensure shop is hidden at launch
        if (shopScrollView != null)
        {
            shopScrollView.SetActive(false);
        }
    }

    //  Hover Logic 

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (HoverPanel != null)
        {
            HoverPanel.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (HoverPanel != null)
        {
            HoverPanel.SetActive(false);
        }
    }

    //  Shop Logic 

    public void ToggleShop()
    {
        if (shopScrollView != null)
        {
            bool isActive = shopScrollView.activeSelf;
            shopScrollView.SetActive(!isActive);

            // Optionally reset all hover panels inside ScrollView on open
            if (!isActive)
            {
                HoverOver[] hoverScripts = shopScrollView.GetComponentsInChildren<HoverOver>(true);
                foreach (var hover in hoverScripts)
                {
                    if (hover.HoverPanel != null)
                        hover.HoverPanel.SetActive(false);
                }
            }
        }
    }
}
