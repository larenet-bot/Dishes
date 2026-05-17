using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates a temporary non-interactable copy of the completed dish image,
/// moves it left, shrinks it, then removes it after it reaches the clean pile area.
/// Put this on an empty GameObject named DishPileManager.
/// </summary>
public class DishPileManager : MonoBehaviour
{
    public static DishPileManager Instance { get; private set; }

    [Header("Completed Dish Motion")]
    [Tooltip("How fast the completed dish moves left in UI anchored-position units per second.")]
    [SerializeField] private float moveSpeed = 800f;

    [Tooltip("How far left the completed dish moves in UI anchored-position units.")]
    [SerializeField] private float moveDistance = 350f;

    [Tooltip("Final scale multiplier applied to the completed dish copy. 0.35 means it ends at 35% of its starting scale.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float shrinkMultiplier = 0.35f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AnimateCompletedDish(DishVisual completedDishVisual)
    {
        if (completedDishVisual == null || completedDishVisual.dishImage == null)
        {
            return;
        }

        Image sourceImage = completedDishVisual.dishImage;

        if (sourceImage.sprite == null)
        {
            return;
        }

        RectTransform sourceRect = sourceImage.rectTransform;
        Transform parent = sourceRect.parent;

        if (parent == null)
        {
            return;
        }

        GameObject cleanDishCopy = new GameObject(
            "CompletedDish_ToCleanPile",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(LayoutElement)
        );

        cleanDishCopy.transform.SetParent(parent, false);

        RectTransform copyRect = cleanDishCopy.GetComponent<RectTransform>();
        CopyRectTransform(sourceRect, copyRect);

        Image copyImage = cleanDishCopy.GetComponent<Image>();
        CopyImage(sourceImage, copyImage);

        CanvasGroup canvasGroup = cleanDishCopy.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        LayoutElement layoutElement = cleanDishCopy.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        copyImage.raycastTarget = false;

        // Put the completed-copy directly behind the real dish image.
        // The real dish image can immediately become the next dirty dish and stay clickable.
        copyRect.SetSiblingIndex(sourceRect.GetSiblingIndex());

        StartCoroutine(AnimateToCleanPile(copyRect));
    }

    private void CopyRectTransform(RectTransform source, RectTransform copy)
    {
        copy.anchorMin = source.anchorMin;
        copy.anchorMax = source.anchorMax;
        copy.pivot = source.pivot;
        copy.anchoredPosition = source.anchoredPosition;
        copy.sizeDelta = source.sizeDelta;
        copy.localRotation = source.localRotation;
        copy.localScale = source.localScale;
    }

    private void CopyImage(Image source, Image copy)
    {
        copy.sprite = source.sprite;
        copy.overrideSprite = source.overrideSprite;
        copy.color = source.color;
        copy.material = source.material;
        copy.type = source.type;
        copy.preserveAspect = source.preserveAspect;
        copy.fillCenter = source.fillCenter;
        copy.fillMethod = source.fillMethod;
        copy.fillOrigin = source.fillOrigin;
        copy.fillAmount = source.fillAmount;
        copy.fillClockwise = source.fillClockwise;
    }

    private IEnumerator AnimateToCleanPile(RectTransform dishRect)
    {
        if (dishRect == null)
        {
            yield break;
        }

        Vector2 startPosition = dishRect.anchoredPosition;
        Vector2 targetPosition = startPosition + Vector2.left * moveDistance;

        Vector3 startScale = dishRect.localScale;
        Vector3 targetScale = startScale * shrinkMultiplier;

        float safeSpeed = Mathf.Max(1f, moveSpeed);
        float duration = Mathf.Max(0.01f, Mathf.Abs(moveDistance) / safeSpeed);
        float elapsed = 0f;

        while (elapsed < duration && dishRect != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            dishRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            dishRect.localScale = Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        if (dishRect != null)
        {
            Destroy(dishRect.gameObject);
        }
    }
}
