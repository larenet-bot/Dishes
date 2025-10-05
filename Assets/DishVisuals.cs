using UnityEngine;
using UnityEngine.UI;

public class DishVisual : MonoBehaviour
{
    [Tooltip("Image component to update visuals")]
    public Image dishImage;

    private DishData currentDishData;

    public void SetDish(DishData data)
    {
        currentDishData = data;
        SetStage(0);
    }

    public void SetStage(int stage)
    {
        if (currentDishData != null && dishImage != null && currentDishData.stageSprites.Length > 0)
        {
            int clamped = Mathf.Clamp(stage, 0, currentDishData.stageSprites.Length - 1);
            dishImage.sprite = currentDishData.stageSprites[clamped];
        }
    }

    public DishData GetDishData() => currentDishData;
}
