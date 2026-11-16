using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overnight Soak technique state.
/// Every N dishes cleaned (manual completions), the next manual wash awards double dishes.
/// 
/// This is separated from SinkManager so renaming node IDs doesn't break logic.
/// </summary>
public class WashBasinSoakTechnique : MonoBehaviour
{
    [Header("Refs")]
    public SinkManager sinkManager;

    [Header("Node ID")]
    [Tooltip("The SinkManager node ID for Overnight Soak.")]
    public string techniqueNodeId = "wb_technique";

    [Header("Tuning")]
    public long dishesPerSoak = 100;

    private long progress;
    private bool ready;
    private bool readyPopupShown;
    private GameObject readyPopupCanvas;

    public bool IsReady => ready;

    /// <summary>0..1 progress toward the next soak.</summary>
    public float Progress01
    {
        get
        {
            if (dishesPerSoak <= 0) return 0f;
            return Mathf.Clamp01((float)progress / dishesPerSoak);
        }
    }

    private void Awake()
    {
        if (sinkManager == null) sinkManager = SinkManager.Instance;

        if (sinkManager != null)
        {
            sinkManager.OnSinkReset += ResetState;
            sinkManager.OnSinkTypeChanged += OnSinkTypeChanged;
        }
    }

    private void OnDestroy()
    {
        if (sinkManager != null)
        {
            sinkManager.OnSinkReset -= ResetState;
            sinkManager.OnSinkTypeChanged -= OnSinkTypeChanged;
        }

        if (readyPopupCanvas != null)
            Destroy(readyPopupCanvas);
    }

    private void OnSinkTypeChanged(SinkManager.SinkType newType)
    {
        // If the player leaves the wash basin, clear the technique state.
        if (newType != SinkManager.SinkType.WashBasin)
            ResetState();
    }

    public void ResetState()
    {
        progress = 0;
        ready = false;
        readyPopupShown = false;

        if (readyPopupCanvas != null)
            readyPopupCanvas.SetActive(false);
    }

    public bool TryOpenReadyPopup()
    {
        if (!IsReady || !HasTechnique() || !sinkManager.IsPurchased("wb_soak_popup") || readyPopupShown)
            return false;

        readyPopupShown = true;
        CreateReadyPopup();
        readyPopupCanvas.SetActive(true);
        return true;
    }

    public bool HasTechnique()
    {
        if (sinkManager == null) return false;
        if (sinkManager.CurrentSinkType != SinkManager.SinkType.WashBasin) return false;
        if (string.IsNullOrWhiteSpace(techniqueNodeId)) return false;
        return sinkManager.IsPurchased(techniqueNodeId);
    }

    /// <summary>
    /// Preview-only: returns what the award would be if a manual wash completed right now.
    /// This does NOT consume the ready state or advance progress.
    /// </summary>
    public long PreviewApplySoak(long dishesAwarded)
    {
        if (!HasTechnique()) return dishesAwarded;
        if (!ready) return dishesAwarded;
        return dishesAwarded * 2;
    }

    /// <summary>
    /// Call this once per manual completion after awarding dishes.
    /// This consumes the ready state (if present), then advances progress.
    /// </summary>
    public void OnManualWashCompleted(long dishesAwarded)
    {
        if (!HasTechnique()) return;
        if (dishesPerSoak <= 0) return;
        if (dishesAwarded <= 0) return;

        // If we were ready, this wash is considered the doubled wash.
        if (ready)
        {
            ready = false;
            readyPopupShown = false;
        }

        progress += dishesAwarded;

        while (progress >= dishesPerSoak)
        {
            progress -= dishesPerSoak;
            ready = true;
            readyPopupShown = false;
        }
    }

    private void CreateReadyPopup()
    {
        if (readyPopupCanvas != null)
            return;

        readyPopupCanvas = new GameObject(
            "WashBasinSoakReadyCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = readyPopupCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = readyPopupCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        RectTransform backdropRect = CreateRect("Backdrop", readyPopupCanvas.transform, Vector2.zero);
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image backdrop = backdropRect.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.65f);

        RectTransform panelRect = CreateRect("Panel", backdropRect, new Vector2(480f, 260f));
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        Image panel = panelRect.gameObject.AddComponent<Image>();
        panel.color = new Color(0.95f, 0.9f, 0.72f, 1f);

        RectTransform titleRect = CreateRect("Title", panelRect, new Vector2(420f, 72f));
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -32f);
        Text title = CreateText("Label", titleRect, "Overnight Soak Ready", 32);
        title.color = new Color(0.2f, 0.18f, 0.12f, 1f);
        title.rectTransform.anchorMin = Vector2.zero;
        title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;

        RectTransform buttonRect = CreateRect("CloseButton", panelRect, new Vector2(200f, 64f));
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 28f);

        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = new Color(0.25f, 0.45f, 0.3f, 1f);
        Button closeButton = buttonRect.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = buttonImage;

        Text buttonLabel = CreateText("Label", buttonRect, "Close", 30);
        buttonLabel.color = Color.white;
        buttonLabel.rectTransform.anchorMin = Vector2.zero;
        buttonLabel.rectTransform.anchorMax = Vector2.one;
        buttonLabel.rectTransform.offsetMin = Vector2.zero;
        buttonLabel.rectTransform.offsetMax = Vector2.zero;

        closeButton.onClick.AddListener(() => readyPopupCanvas.SetActive(false));
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 size)
    {
        GameObject element = new GameObject(name, typeof(RectTransform));
        element.transform.SetParent(parent, false);

        RectTransform rect = element.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        return rect;
    }

    private static Text CreateText(string name, Transform parent, string value, int fontSize)
    {
        RectTransform rect = CreateRect(name, parent, Vector2.zero);
        Text text = rect.gameObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }
}
