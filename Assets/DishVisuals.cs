using UnityEngine;
using UnityEngine.UI;

public class DishVisual : MonoBehaviour
{
    [Tooltip("Image component to update visuals")]
    public Image dishImage;

    [Tooltip("Base reference size for the dish button (optional).")]
    public Vector2 baseSize = new Vector2(400f, 400f);

    private DishData currentDishData;

    public void SetDish(DishData data)
    {
        currentDishData = data;

        if (currentDishData == null || dishImage == null || currentDishData.stageSprites.Length == 0)
            return;

        // Set starting sprite
        dishImage.sprite = currentDishData.stageSprites[0];
        dishImage.preserveAspect = true; // Unclear if working

        // Make sure aspect is correct and we are not stretching
        // (also check "Preserve Aspect" on the Image in the Inspector)
        dishImage.SetNativeSize();

        // Apply per-dish scale so big pans can be larger than plates
        float scale = currentDishData.uiScale <= 0f ? 1f : currentDishData.uiScale;
        RectTransform rt = dishImage.rectTransform;
        rt.localScale = Vector3.one * scale;
    }

    public void SetStage(int stage)
    {
        if (currentDishData != null && dishImage != null && currentDishData.stageSprites.Length > 0)
        {
            int clamped = Mathf.Clamp(stage, 0, currentDishData.stageSprites.Length - 1);
            dishImage.sprite = currentDishData.stageSprites[clamped];
            // Keep size/scale as set in SetDish; no need to touch it here
        }
    }

    public DishData GetDishData() => currentDishData;
}