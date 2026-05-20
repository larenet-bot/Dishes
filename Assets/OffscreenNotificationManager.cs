using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OffscreenNotificationManager : MonoBehaviour
{
    public static OffscreenNotificationManager Instance { get; private set; }

    private enum HorizontalState
    {
        Visible,
        Left,
        Right
    }

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("TMP object that displays < when something is offscreen left.")]
    [SerializeField] private TMP_Text leftIndicatorText;

    [Tooltip("TMP object that displays > when something is offscreen right.")]
    [SerializeField] private TMP_Text rightIndicatorText;

    [Header("Visibility")]
    [Tooltip("Small padding so the notification clears when the target is safely on screen.")]
    [Range(0f, 0.25f)]
    [SerializeField] private float viewportPadding = 0.02f;

    [Header("Flash")]
    [SerializeField] private float flashCycleSeconds = 1.5f;

    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.2f;

    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 1f;

    private readonly List<Transform> trackedTargets = new List<Transform>();

    private Color leftBaseColor = Color.white;
    private Color rightBaseColor = Color.white;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (leftIndicatorText != null)
        {
            leftBaseColor = leftIndicatorText.color;
            leftIndicatorText.raycastTarget = false;
            leftIndicatorText.gameObject.SetActive(false);
        }

        if (rightIndicatorText != null)
        {
            rightBaseColor = rightIndicatorText.color;
            rightIndicatorText.raycastTarget = false;
            rightIndicatorText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        bool showLeft = false;
        bool showRight = false;

        for (int i = trackedTargets.Count - 1; i >= 0; i--)
        {
            Transform target = trackedTargets[i];

            if (target == null)
            {
                trackedTargets.RemoveAt(i);
                continue;
            }

            HorizontalState state = GetHorizontalState(target);

            if (state == HorizontalState.Visible)
            {
                trackedTargets.RemoveAt(i);
                continue;
            }

            if (state == HorizontalState.Left)
                showLeft = true;

            if (state == HorizontalState.Right)
                showRight = true;
        }

        float flashAlpha = GetFlashAlpha();

        ApplyIndicator(leftIndicatorText, leftBaseColor, showLeft, flashAlpha);
        ApplyIndicator(rightIndicatorText, rightBaseColor, showRight, flashAlpha);
    }

    public void NotifyUntilVisible(Transform target)
    {
        if (target == null)
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        if (GetHorizontalState(target) == HorizontalState.Visible)
            return;

        if (!trackedTargets.Contains(target))
            trackedTargets.Add(target);
    }

    public void NotifyUntilVisible(GameObject target)
    {
        if (target == null)
            return;

        NotifyUntilVisible(target.transform);
    }

    private float GetFlashAlpha()
    {
        float cycle = Mathf.Max(0.1f, flashCycleSeconds);
        float wave = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / cycle);
        float t = (wave + 1f) * 0.5f;

        return Mathf.Lerp(minAlpha, maxAlpha, t);
    }

    private void ApplyIndicator(TMP_Text text, Color baseColor, bool active, float alpha)
    {
        if (text == null)
            return;

        if (text.gameObject.activeSelf != active)
            text.gameObject.SetActive(active);

        if (!active)
            return;

        Color c = baseColor;
        c.a = alpha;
        text.color = c;
    }

    private HorizontalState GetHorizontalState(Transform target)
    {
        if (!TryGetViewportXRange(target, out float minX, out float maxX))
            return HorizontalState.Visible;

        float leftEdge = viewportPadding;
        float rightEdge = 1f - viewportPadding;

        if (maxX < leftEdge)
            return HorizontalState.Left;

        if (minX > rightEdge)
            return HorizontalState.Right;

        return HorizontalState.Visible;
    }

    private bool TryGetViewportXRange(Transform target, out float minX, out float maxX)
    {
        minX = 0f;
        maxX = 0f;

        if (target == null || targetCamera == null)
            return false;

        RectTransform rectTransform = target as RectTransform;

        if (rectTransform != null)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return GetViewportRangeFromPoints(corners, out minX, out maxX);
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);

        if (renderers != null && renderers.Length > 0)
        {
            bool hasBounds = false;
            Bounds combinedBounds = new Bounds(target.position, Vector3.zero);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];

                if (r == null)
                    continue;

                if (!hasBounds)
                {
                    combinedBounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(r.bounds);
                }
            }

            if (hasBounds)
            {
                return GetViewportRangeFromBounds(combinedBounds, out minX, out maxX);
            }
        }

        Collider2D[] colliders2D = target.GetComponentsInChildren<Collider2D>(true);

        if (colliders2D != null && colliders2D.Length > 0)
        {
            bool hasBounds = false;
            Bounds combinedBounds = new Bounds(target.position, Vector3.zero);

            for (int i = 0; i < colliders2D.Length; i++)
            {
                Collider2D c = colliders2D[i];

                if (c == null)
                    continue;

                if (!hasBounds)
                {
                    combinedBounds = c.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(c.bounds);
                }
            }

            if (hasBounds)
            {
                return GetViewportRangeFromBounds(combinedBounds, out minX, out maxX);
            }
        }

        Vector3 viewportPoint = targetCamera.WorldToViewportPoint(target.position);
        minX = viewportPoint.x;
        maxX = viewportPoint.x;
        return viewportPoint.z > 0f;
    }

    private bool GetViewportRangeFromBounds(Bounds bounds, out float minX, out float maxX)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3[] points =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z)
        };

        return GetViewportRangeFromPoints(points, out minX, out maxX);
    }

    private bool GetViewportRangeFromPoints(Vector3[] worldPoints, out float minX, out float maxX)
    {
        minX = float.PositiveInfinity;
        maxX = float.NegativeInfinity;

        bool anyPointInFront = false;

        for (int i = 0; i < worldPoints.Length; i++)
        {
            Vector3 viewportPoint = targetCamera.WorldToViewportPoint(worldPoints[i]);

            if (viewportPoint.z > 0f)
                anyPointInFront = true;

            minX = Mathf.Min(minX, viewportPoint.x);
            maxX = Mathf.Max(maxX, viewportPoint.x);
        }

        if (!anyPointInFront)
        {
            minX = 0f;
            maxX = 0f;
            return false;
        }

        return true;
    }
}