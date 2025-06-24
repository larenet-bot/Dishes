using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages sprite visuals for a dish based on cleaning stage.
/// </summary>
public class DishVisual : MonoBehaviour
{
    [Tooltip("Image component to update visuals")]
    public Image dishImage;

    [Tooltip("Sprites representing each stage (Dirty → Soapy → Clean)")]
    public Sprite[] stageSprites;

    /// <summary>
    /// Set the visual sprite based on the current stage index.
    /// </summary>
    public void SetStage(int stage)
    {
        if (stageSprites != null && stageSprites.Length > 0 && dishImage != null)
        {
            int clamped = Mathf.Clamp(stage, 0, stageSprites.Length - 1);
            dishImage.sprite = stageSprites[clamped];
        }
    }
}
