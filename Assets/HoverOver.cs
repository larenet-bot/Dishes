using UnityEngine;
using UnityEngine.EventSystems;

public class HoverOver : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject HoverPanel;

    public void OnPointerEnter(PointerEventData eventData)
    {
        HoverPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HoverPanel.SetActive(false);
    }

    void Start()
    {
        // Make sure panel is hidden at launch (even if Unity resets it)
        if (HoverPanel != null)
        {
            HoverPanel.SetActive(false);
        }
    }
}
