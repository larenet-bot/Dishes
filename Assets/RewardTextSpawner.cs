
using UnityEngine;

/// <summary>
/// Responsible for spawning floating reward/award text. Separated from DishClicker
/// so UI positioning and prefab details are consolidated in one place.
/// </summary>
public class RewardTextSpawner : MonoBehaviour
{
    [SerializeField, Tooltip("Prefab used for floating reward text.")]
    private GameObject rewardTextPrefab;

    [SerializeField, Tooltip("World-space offset used for standard reward text.")]
    private Vector3 rewardTextOffset = new Vector3(0f, 0.5f, 0f);

    [SerializeField, Tooltip("World-space Z plane used when spawning floating text.")]
    private float rewardTextPlaneZ = 0f;

    [SerializeField, Tooltip("World-space offset used for the instant wash title text.")]
    private Vector3 instantWashAwardTextOffset = new Vector3(0f, 0.75f, 0f);

    /// <summary>
    /// Spawns reward text near the pointer position.
    /// </summary>
    public void SpawnRewardText(float rewardAmount)
    {
        if (rewardTextPrefab == null || Camera.main == null || rewardAmount <= 0f)
        {
            return;
        }

        if (!TryGetMouseWorldPosition(out Vector3 worldPosition))
        {
            return;
        }

        worldPosition.z = rewardTextPlaneZ;
        worldPosition += rewardTextOffset;

        SpawnFloatingText(worldPosition, "+ " + BigNumberFormatter.FormatMoney((double)rewardAmount));
    }

    /// <summary>
    /// Spawns reward text at a supplied world position.
    /// </summary>
    public void SpawnRewardTextAtWorld(float rewardAmount, Vector3 worldPosition)
    {
        if (rewardTextPrefab == null || rewardAmount <= 0f)
        {
            return;
        }

        worldPosition.z = rewardTextPlaneZ;
        worldPosition += rewardTextOffset;

        SpawnFloatingText(worldPosition, "+ " + BigNumberFormatter.FormatMoney((double)rewardAmount));
    }

    /// <summary>
    /// Spawns the instant wash title above the dish.
    /// </summary>
    public void SpawnInstantWashAwardText(string awardTitle, DishVisual dishVisual)
    {
        if (rewardTextPrefab == null || string.IsNullOrWhiteSpace(awardTitle) || dishVisual == null)
        {
            return;
        }

        if (!TryGetDishWorldPosition(dishVisual, out Vector3 dishWorld))
        {
            return;
        }

        dishWorld.z = rewardTextPlaneZ;
        dishWorld += instantWashAwardTextOffset;

        SpawnFloatingText(dishWorld, awardTitle);
    }

    private void SpawnFloatingText(Vector3 worldPosition, string message)
    {
        GameObject instance = Instantiate(rewardTextPrefab, worldPosition, Quaternion.identity);
        BubbleRewardText floatingText = instance.GetComponent<BubbleRewardText>();

        if (floatingText != null)
        {
            floatingText.Initialize(message);
        }
    }

    private bool TryGetMouseWorldPosition(out Vector3 worldPos)
    {
        worldPos = default;
        if (Camera.main == null)
        {
            return false;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, rewardTextPlaneZ));
        if (plane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }

        worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));
        worldPos.z = rewardTextPlaneZ;
        return true;
    }

    private bool TryGetDishWorldPosition(DishVisual dishVisual, out Vector3 worldPos)
    {
        worldPos = default;
        if (dishVisual == null || dishVisual.dishImage == null || Camera.main == null)
        {
            return false;
        }

        RectTransform rectTransform = dishVisual.dishImage.rectTransform;
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

        Canvas canvas = dishVisual.dishImage.canvas;
        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);

        Ray ray = Camera.main.ScreenPointToRay(screenPoint);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, rewardTextPlaneZ));
        if (plane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }

        worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Camera.main.nearClipPlane));
        worldPos.z = rewardTextPlaneZ;
        return true;
    }
}