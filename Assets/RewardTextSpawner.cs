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
            return;

        if (!TryGetMouseWorldPosition(out Vector3 worldPosition))
            return;

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
            return;

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
            return;

        if (!TryGetDishWorldPosition(dishVisual, rewardTextPlaneZ, out Vector3 dishWorld))
            return;

        dishWorld += instantWashAwardTextOffset;

        SpawnFloatingText(dishWorld, awardTitle);
    }

    private void SpawnFloatingText(Vector3 worldPosition, string message)
    {
        if (rewardTextPrefab == null)
            return;

        // If the prefab is a UI element (has RectTransform), parent it to the first Canvas
        RectTransform prefabRect = rewardTextPrefab.GetComponent<RectTransform>();
        if (prefabRect != null)
        {
            Canvas targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas != null)
            {
                // Convert world position to screen point, then to canvas local point
                Vector3 screenPoint = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPosition) : new Vector3(worldPosition.x, worldPosition.y, 0f);

                GameObject instance = Instantiate(rewardTextPrefab, targetCanvas.transform, false);
                RectTransform rt = instance.GetComponent<RectTransform>();
                if (rt != null)
                {
                    RectTransform canvasRect = targetCanvas.transform as RectTransform;
                    Camera cam = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out Vector2 localPoint))
                    {
                        rt.anchoredPosition = localPoint;
                    }
                    else
                    {
                        // fallback: center-plus-world y
                        rt.anchoredPosition = new Vector2(screenPoint.x - (canvasRect.sizeDelta.x * 0.5f), screenPoint.y - (canvasRect.sizeDelta.y * 0.5f));
                    }
                }

                BubbleRewardText floatingText1 = instance.GetComponent<BubbleRewardText>();
                if (floatingText1 != null) floatingText1.Initialize(message);
                return;
            }
        }

        // World-space fallback (non-UI prefab)
        GameObject go = Instantiate(rewardTextPrefab, worldPosition, Quaternion.identity);
        BubbleRewardText floatingText2 = go.GetComponent<BubbleRewardText>();
        if (floatingText2 != null) floatingText2.Initialize(message);
    }

    private bool TryGetMouseWorldPosition(out Vector3 worldPos)
    {
        worldPos = default;
        if (Camera.main == null)
            return false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, rewardTextPlaneZ));
        if (plane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }

        worldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));
        worldPos.z = rewardTextPlaneZ;
        return true;
    }

    /// <summary>
    /// Attempts to get the dish's world position at the specified Z plane.
    /// </summary>
    public bool TryGetDishWorldPosition(DishVisual dishVisual, float planeZ, out Vector3 worldPos)
    {
        worldPos = default;
        if (dishVisual == null || dishVisual.dishImage == null || Camera.main == null)
            return false;

        RectTransform rectTransform = dishVisual.dishImage.rectTransform;
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

        Canvas canvas = dishVisual.dishImage.canvas;
        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);

        Ray ray = Camera.main.ScreenPointToRay(screenPoint);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
        if (plane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }

        worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Camera.main.nearClipPlane));
        worldPos.z = planeZ;
        return true;
    }
}