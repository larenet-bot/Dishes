using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// An independent UI copy of an existing DishData dirty sprite. The sprite's
/// Custom Physics Shape drives both selection and the packing validator.
/// </summary>
[RequireComponent(typeof(Image))]
public class DishwasherDraggableDish : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, ICanvasRaycastFilter
{
    private readonly List<List<Vector2>> localOutlines = new List<List<Vector2>>();
    private DishwasherLoadMenu owner;
    private RectTransform rectTransform;
    private Image image;
    private DishwasherSelectionOutline selectionOutline;
    private Vector2 dragOffset;
    private Vector2 positionBeforeDrag;
    private Coroutine shakeRoutine;
    private Vector2 shakeBasePosition;
    private Vector2 lockedBasePosition;
    private bool locked;
    private bool animateWhenLocked;
    private float animationPhase;

    public DishData Dish { get; private set; }
    public RectTransform RectTransform => rectTransform;
    public bool Locked
    {
        get => locked;
        set
        {
            if (locked == value) return;
            if (value)
            {
                StopShake();
                lockedBasePosition = rectTransform.anchoredPosition;
                SetSelected(false);
            }
            else
            {
                rectTransform.anchoredPosition = lockedBasePosition;
                animateWhenLocked = false;
            }
            locked = value;
        }
    }
    public Vector2 StablePosition => locked ? lockedBasePosition : rectTransform.anchoredPosition;
    public float SelectionPadding { get; set; } = 7f;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        image = GetComponent<Image>();
        animationPhase = Mathf.Abs(GetInstanceID() % 13) * 0.47f;
    }

    public void Initialize(DishwasherLoadMenu menu, DishData dish, float sizeMultiplier)
    {
        owner = menu;
        Dish = dish;
        if (rectTransform == null) rectTransform = (RectTransform)transform;
        if (image == null) image = GetComponent<Image>();

        Sprite sprite = dish != null && dish.stageSprites != null && dish.stageSprites.Length > 0
            ? dish.stageSprites[0] : null;
        if (sprite == null) return;

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = true;
        image.color = Color.white;

        float normalScale = dish.uiScale > 0f ? dish.uiScale : 1f;
        float scale = normalScale * Mathf.Max(0.01f, sizeMultiplier);
        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
        rectTransform.sizeDelta = sprite.rect.size * scale;
        BuildLocalOutlines(sprite);
        CreateSelectionOutline();
    }

    private void CreateSelectionOutline()
    {
        GameObject indicator = new GameObject("Selected_DottedOutline",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(DishwasherSelectionOutline));
        indicator.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)indicator.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        selectionOutline = indicator.GetComponent<DishwasherSelectionOutline>();
        selectionOutline.color = new Color(1f, 1f, 1f, 0.95f);
        selectionOutline.raycastTarget = false;
        indicator.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectionOutline != null)
            selectionOutline.gameObject.SetActive(selected && !locked);
    }

    public void SetWashingAnimation(bool enabled)
    {
        animateWhenLocked = enabled;
        if (!enabled && locked && rectTransform != null)
            rectTransform.anchoredPosition = lockedBasePosition;
    }

    private void Update()
    {
        if (locked && animateWhenLocked)
            rectTransform.anchoredPosition = lockedBasePosition + Vector2.up *
                (Mathf.Sin(Time.unscaledTime * 27f + animationPhase) * 1.5f);
    }

    private void BuildLocalOutlines(Sprite sprite)
    {
        localOutlines.Clear();
        Rect displayRect = rectTransform.rect;
        var source = new List<Vector2>();
        int count = sprite.GetPhysicsShapeCount();
        for (int i = 0; i < count; i++)
        {
            source.Clear();
            sprite.GetPhysicsShape(i, source);
            if (source.Count < 3) continue;
            var contour = new List<Vector2>(source.Count);
            foreach (Vector2 point in source)
            {
                Vector2 pixel = point * sprite.pixelsPerUnit + sprite.pivot;
                contour.Add(new Vector2(
                    displayRect.xMin + pixel.x / sprite.rect.width * displayRect.width,
                    displayRect.yMin + pixel.y / sprite.rect.height * displayRect.height));
            }
            localOutlines.Add(contour);
        }

        if (localOutlines.Count == 0)
        {
            localOutlines.Add(new List<Vector2>
            {
                new Vector2(displayRect.xMin, displayRect.yMin),
                new Vector2(displayRect.xMin, displayRect.yMax),
                new Vector2(displayRect.xMax, displayRect.yMax),
                new Vector2(displayRect.xMax, displayRect.yMin)
            });
            Debug.LogWarning($"[Dishwasher] No physics outline on {sprite.name}; using its rectangle.", this);
        }
    }

    public List<List<Vector2>> GetOutlinesInSpace(RectTransform targetSpace)
    {
        var result = new List<List<Vector2>>(localOutlines.Count);
        foreach (List<Vector2> contour in localOutlines)
        {
            var converted = new List<Vector2>(contour.Count);
            foreach (Vector2 point in contour)
                converted.Add(targetSpace.InverseTransformPoint(rectTransform.TransformPoint(point)));
            result.Add(converted);
        }
        return result;
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (locked) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, screenPoint, eventCamera, out Vector2 localPoint)) return false;

        foreach (List<Vector2> contour in localOutlines)
        {
            if (DishwasherPlacementValidator.PointInsidePolygon(localPoint, contour)) return true;
            if (DishwasherPlacementValidator.NearPolygonEdge(localPoint, contour, SelectionPadding)) return true;
        }
        return false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (locked || owner == null) return;
        StopShake();
        owner.Select(this);
        transform.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (locked || owner == null) return;
        StopShake();
        owner.Select(this);
        transform.SetAsLastSibling();
        positionBeforeDrag = rectTransform.anchoredPosition;
        dragOffset = owner.TryGetLayerPoint(eventData.position, eventData.pressEventCamera, out Vector2 pointer)
            ? rectTransform.anchoredPosition - pointer : Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (locked || owner == null) return;
        if (owner.TryGetLayerPoint(eventData.position, eventData.pressEventCamera, out Vector2 pointer))
        {
            rectTransform.anchoredPosition = pointer + dragOffset;
            owner.NotifyDishMoved();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (locked || owner == null) return;
        owner.FinishDrag(this, positionBeforeDrag);
    }

    public void RotateClockwise()
    {
        if (locked) return;
        StopShake();
        rectTransform.localRotation *= Quaternion.Euler(0f, 0f, -90f);
        owner?.NotifyDishMoved();
    }

    public int GetQuarterTurns()
    {
        return (Mathf.RoundToInt(-rectTransform.localEulerAngles.z / 90f) % 4 + 4) % 4;
    }

    public void ApplyQuarterTurns(int quarterTurns)
    {
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f * ((quarterTurns % 4 + 4) % 4));
    }

    public void Shake()
    {
        if (!gameObject.activeInHierarchy || locked) return;
        StopShake();
        shakeBasePosition = rectTransform.anchoredPosition;
        shakeRoutine = StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator ShakeCoroutine()
    {
        for (int i = 0; i < 8; i++)
        {
            rectTransform.anchoredPosition = shakeBasePosition + Vector2.right * (i % 2 == 0 ? -7f : 7f);
            yield return new WaitForSecondsRealtime(0.045f);
        }
        rectTransform.anchoredPosition = shakeBasePosition;
        shakeRoutine = null;
    }

    private void StopShake()
    {
        if (shakeRoutine == null) return;
        StopCoroutine(shakeRoutine);
        rectTransform.anchoredPosition = shakeBasePosition;
        shakeRoutine = null;
    }

    private void OnDisable()
    {
        StopShake();
        if (locked && rectTransform != null)
            rectTransform.anchoredPosition = lockedBasePosition;
    }
}
