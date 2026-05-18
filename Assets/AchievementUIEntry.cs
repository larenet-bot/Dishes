//Assets/AchievementUIEntry.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementUIEntry : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public Image iconImage;
    public GameObject lockedOverlay;

    [Header("Optional")]
    public Sprite lockedIcon;

    public void Setup(AchievementData data, bool unlocked)
    {
        if (titleText != null)
            titleText.text = unlocked ? data.title : "Locked Achievement";

        if (descriptionText != null)
            descriptionText.text = unlocked ? data.description : "???";

        if (iconImage != null)
        {
            if (unlocked)
            {
                iconImage.sprite = data.icon;
            }
            else if (lockedIcon != null)
            {
                iconImage.sprite = lockedIcon;
            }
            else
            {
                iconImage.sprite = data.icon; // fallback so UI isn't empty if you want
            }
        }

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!unlocked);
    }
}