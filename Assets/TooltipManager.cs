using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    [Serializable]
    public class TooltipEntry
    {
        [Tooltip("Drag the object you want to hover over here.")]
        public GameObject targetObject;

        [TextArea(2, 6)]
        [Tooltip("Text shown when the mouse hovers over this object.")]
        public string tooltipText;
    }

    [Header("Tooltip List")]
    public TooltipEntry[] tooltipEntries = new TooltipEntry[0];

    [Header("Tooltip UI")]
    [Tooltip("Leave empty to auto-find a Canvas.")]
    [SerializeField] private Canvas tooltipCanvas;

    [Tooltip("Leave empty to auto-create the tooltip box.")]
    [SerializeField] private RectTransform tooltipRoot;

    [Tooltip("Leave empty to auto-create tooltip text.")]
    [SerializeField] private TMP_Text tooltipText;

    [Header("Position")]
    [SerializeField] private Vector2 tooltipOffset = new Vector2(18f, -18f);

    [Tooltip("Maximum tooltip text width before wrapping. The actual tooltip width is measured after layout rebuilds.")]
    [SerializeField] private float tooltipMaxTextWidth = 320f;

    [Tooltip("When true, the tooltip appears to the left of the cursor whenever the mouse is on the right half of the screen.")]
    [SerializeField] private bool flipOnRightHalf = true;

    [SerializeField] private bool clampToCanvas = true;

    [Header("World Object Hover")]
    [Tooltip("Allows tooltips on objects with Collider2D.")]
    [SerializeField] private bool check2DColliders = true;

    [Tooltip("Allows tooltips on objects with 3D colliders.")]
    [SerializeField] private bool check3DColliders = true;

    private GraphicRaycaster graphicRaycaster;
    private readonly List<RaycastResult> uiResults = new List<RaycastResult>();

    private TooltipEntry currentEntry;

    private bool manualTooltipActive;
    private string manualTooltipText = string.Empty;

    private void Awake()
    {
        SetupCanvas();
        SetupTooltipUI();
        HideTooltip();
    }

    private void OnEnable()
    {
        SetupCanvas();
        SetupTooltipUI();
        HideTooltip();
    }

    private void OnDisable()
    {
        manualTooltipActive = false;
        manualTooltipText = string.Empty;
        HideTooltip();
    }

    private void Update()
    {
        if (manualTooltipActive)
        {
            ShowTooltipText(manualTooltipText);
            UpdateTooltipPosition();
            return;
        }

        TooltipEntry hoveredEntry = FindHoveredTooltipEntry();

        if (hoveredEntry != null)
        {
            ShowTooltip(hoveredEntry);
            UpdateTooltipPosition();
        }
        else
        {
            HideTooltip();
        }
    }

    private void SetupCanvas()
    {
        if (tooltipCanvas == null)
        {
            tooltipCanvas = FindFirstObjectByType<Canvas>();
        }

        if (tooltipCanvas == null)
        {
            Debug.LogWarning("[TooltipManager] No Canvas found. Tooltip cannot be shown.");
            return;
        }

        graphicRaycaster = tooltipCanvas.GetComponent<GraphicRaycaster>();

        if (graphicRaycaster == null)
        {
            graphicRaycaster = tooltipCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (EventSystem.current == null)
        {
            Debug.LogWarning("[TooltipManager] No EventSystem found in scene. UI hover detection may not work.");
        }
    }

    private void SetupTooltipUI()
    {
        if (tooltipCanvas == null)
        {
            return;
        }

        if (tooltipRoot != null && tooltipText != null)
        {
            CanvasGroup existingGroup = tooltipRoot.GetComponent<CanvasGroup>();

            if (existingGroup == null)
            {
                existingGroup = tooltipRoot.gameObject.AddComponent<CanvasGroup>();
            }

            existingGroup.blocksRaycasts = false;
            ConfigureTooltipTransform();
            return;
        }

        GameObject root = new GameObject("GeneratedTooltip");
        root.transform.SetParent(tooltipCanvas.transform, false);

        tooltipRoot = root.AddComponent<RectTransform>();
        ConfigureTooltipTransform();

        Image background = root.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.82f);

        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = root.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textObject = new GameObject("TooltipText");
        textObject.transform.SetParent(root.transform, false);

        tooltipText = textObject.AddComponent<TextMeshProUGUI>();
        tooltipText.text = "";
        tooltipText.fontSize = 22f;
        tooltipText.color = Color.white;
        tooltipText.enableWordWrapping = true;

        RectTransform textRect = tooltipText.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(Mathf.Max(80f, tooltipMaxTextWidth), 0f);
    }

    private void ConfigureTooltipTransform()
    {
        if (tooltipRoot == null)
        {
            return;
        }

        // Center anchors make anchoredPosition use the same coordinate space
        // returned by ScreenPointToLocalPointInRectangle on the canvas.
        tooltipRoot.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRoot.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRoot.pivot = new Vector2(0f, 1f);
    }

    private TooltipEntry FindHoveredTooltipEntry()
    {
        TooltipEntry uiEntry = FindHoveredUIEntry();

        if (uiEntry != null)
        {
            return uiEntry;
        }

        TooltipEntry rectEntry = FindHoveredRectTransformEntry();

        if (rectEntry != null)
        {
            return rectEntry;
        }

        TooltipEntry worldEntry = FindHoveredWorldEntry();

        if (worldEntry != null)
        {
            return worldEntry;
        }

        return null;
    }

    private TooltipEntry FindHoveredUIEntry()
    {
        if (EventSystem.current == null)
        {
            return null;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        uiResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiResults);

        for (int i = 0; i < uiResults.Count; i++)
        {
            GameObject hitObject = uiResults[i].gameObject;

            TooltipEntry entry = GetEntryForObjectOrParent(hitObject);

            if (entry != null)
            {
                return entry;
            }
        }

        return null;
    }

    private TooltipEntry FindHoveredRectTransformEntry()
    {
        Camera uiCamera = GetUICamera();

        for (int i = 0; i < tooltipEntries.Length; i++)
        {
            TooltipEntry entry = tooltipEntries[i];

            if (entry == null || entry.targetObject == null)
            {
                continue;
            }

            RectTransform rect = entry.targetObject.GetComponent<RectTransform>();

            if (rect == null)
            {
                continue;
            }

            bool containsMouse = RectTransformUtility.RectangleContainsScreenPoint(
                rect,
                Input.mousePosition,
                uiCamera
            );

            if (containsMouse)
            {
                return entry;
            }
        }

        return null;
    }

    private TooltipEntry FindHoveredWorldEntry()
    {
        if (Camera.main == null)
        {
            return null;
        }

        if (check2DColliders)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mouseWorld2D = new Vector2(mouseWorld.x, mouseWorld.y);

            Collider2D hit2D = Physics2D.OverlapPoint(mouseWorld2D);

            if (hit2D != null)
            {
                TooltipEntry entry = GetEntryForObjectOrParent(hit2D.gameObject);

                if (entry != null)
                {
                    return entry;
                }
            }
        }

        if (check3DColliders)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit3D))
            {
                TooltipEntry entry = GetEntryForObjectOrParent(hit3D.collider.gameObject);

                if (entry != null)
                {
                    return entry;
                }
            }
        }

        return null;
    }

    private TooltipEntry GetEntryForObjectOrParent(GameObject hitObject)
    {
        if (hitObject == null || tooltipEntries == null)
        {
            return null;
        }

        for (int i = 0; i < tooltipEntries.Length; i++)
        {
            TooltipEntry entry = tooltipEntries[i];

            if (entry == null || entry.targetObject == null)
            {
                continue;
            }

            if (hitObject == entry.targetObject)
            {
                return entry;
            }

            if (hitObject.transform.IsChildOf(entry.targetObject.transform))
            {
                return entry;
            }
        }

        return null;
    }

    public void ShowManualTooltip(string text)
    {
        manualTooltipText = text;
        manualTooltipActive = !string.IsNullOrWhiteSpace(manualTooltipText);

        if (!manualTooltipActive)
        {
            HideTooltip();
            return;
        }

        currentEntry = null;
        ShowTooltipText(manualTooltipText);
        UpdateTooltipPosition();
    }

    public void HideManualTooltip()
    {
        manualTooltipActive = false;
        manualTooltipText = string.Empty;
        HideTooltip();
    }

    private void ShowTooltip(TooltipEntry entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.tooltipText))
        {
            HideTooltip();
            return;
        }

        if (currentEntry != entry)
        {
            currentEntry = entry;
        }

        ShowTooltipText(entry.tooltipText);
    }

    private void ShowTooltipText(string text)
    {
        if (tooltipRoot == null || tooltipText == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            HideTooltip();
            return;
        }

        if (tooltipText.text != text)
        {
            tooltipText.text = text;
        }

        if (!tooltipRoot.gameObject.activeSelf)
        {
            tooltipRoot.gameObject.SetActive(true);
        }

        RebuildTooltipLayout();
    }

    private void HideTooltip()
    {
        currentEntry = null;

        if (tooltipRoot != null && tooltipRoot.gameObject.activeSelf)
        {
            tooltipRoot.gameObject.SetActive(false);
        }
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipCanvas == null || tooltipRoot == null)
        {
            return;
        }

        RectTransform canvasRect = tooltipCanvas.transform as RectTransform;

        if (canvasRect == null)
        {
            return;
        }

        ConfigureTooltipTransform();
        RebuildTooltipLayout();

        Vector2 localMousePosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            GetUICamera(),
            out localMousePosition
        );

        bool shouldFlipLeft = flipOnRightHalf && Input.mousePosition.x > Screen.width * 0.5f;

        tooltipRoot.pivot = shouldFlipLeft
            ? new Vector2(1f, 1f)
            : new Vector2(0f, 1f);

        Vector2 appliedOffset = shouldFlipLeft
            ? new Vector2(-Mathf.Abs(tooltipOffset.x), tooltipOffset.y)
            : new Vector2(Mathf.Abs(tooltipOffset.x), tooltipOffset.y);

        Vector2 anchoredPosition = localMousePosition + appliedOffset;

        if (clampToCanvas)
        {
            anchoredPosition = ClampTooltipToCanvas(anchoredPosition, canvasRect);
        }

        tooltipRoot.anchoredPosition = anchoredPosition;
    }

    private void RebuildTooltipLayout()
    {
        if (tooltipRoot == null)
        {
            return;
        }

        if (tooltipText != null)
        {
            RectTransform textRect = tooltipText.GetComponent<RectTransform>();

            if (textRect != null)
            {
                textRect.sizeDelta = new Vector2(Mathf.Max(80f, tooltipMaxTextWidth), textRect.sizeDelta.y);
            }

            tooltipText.ForceMeshUpdate();
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);
    }

    private Vector2 ClampTooltipToCanvas(Vector2 position, RectTransform canvasRect)
    {
        Vector2 tooltipSize = GetMeasuredTooltipSize();
        Vector2 pivot = tooltipRoot.pivot;
        Rect canvasBounds = canvasRect.rect;

        float minX = canvasBounds.xMin + tooltipSize.x * pivot.x;
        float maxX = canvasBounds.xMax - tooltipSize.x * (1f - pivot.x);

        float minY = canvasBounds.yMin + tooltipSize.y * pivot.y;
        float maxY = canvasBounds.yMax - tooltipSize.y * (1f - pivot.y);

        position.x = ClampAxis(position.x, minX, maxX, canvasBounds.center.x);
        position.y = ClampAxis(position.y, minY, maxY, canvasBounds.center.y);

        return position;
    }

    private Vector2 GetMeasuredTooltipSize()
    {
        Vector2 size = tooltipRoot.rect.size;

        if (size.x > 1f && size.y > 1f)
        {
            return size;
        }

        if (tooltipText == null)
        {
            return new Vector2(Mathf.Max(80f, tooltipMaxTextWidth), 40f);
        }

        Vector2 preferred = tooltipText.GetPreferredValues(
            tooltipText.text,
            Mathf.Max(80f, tooltipMaxTextWidth),
            0f
        );

        Vector2 padding = GetLayoutPadding();

        return new Vector2(
            Mathf.Max(1f, preferred.x + padding.x),
            Mathf.Max(1f, preferred.y + padding.y)
        );
    }

    private Vector2 GetLayoutPadding()
    {
        HorizontalOrVerticalLayoutGroup layout = tooltipRoot.GetComponent<HorizontalOrVerticalLayoutGroup>();

        if (layout == null)
        {
            return Vector2.zero;
        }

        RectOffset padding = layout.padding;

        if (padding == null)
        {
            return Vector2.zero;
        }

        return new Vector2(
            padding.left + padding.right,
            padding.top + padding.bottom
        );
    }

    private float ClampAxis(float value, float min, float max, float fallback)
    {
        if (min > max)
        {
            return fallback;
        }

        return Mathf.Clamp(value, min, max);
    }

    private Camera GetUICamera()
    {
        if (tooltipCanvas == null)
        {
            return null;
        }

        if (tooltipCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return tooltipCanvas.worldCamera;
    }
}
