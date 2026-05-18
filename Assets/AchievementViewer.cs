using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementViewer : MonoBehaviour
{
    public static AchievementViewer Instance { get; private set; }

    private GameObject menuRoot;
    private RectTransform contentRoot;
    private Canvas sceneCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Safe static entry so callers don't need an existing component in the scene.
    public static void ToggleMenuStatic()
    {
        if (Instance == null)
        {
            var go = new GameObject("AchievementViewer");
            Instance = go.AddComponent<AchievementViewer>();
        }

        Instance.ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (menuRoot == null)
            CreateMenu();

        bool newState = !menuRoot.activeSelf;
        menuRoot.SetActive(newState);

        if (newState)
            RefreshContents();
    }

    private void CreateMenu()
    {
        // Find or create a Canvas
        sceneCanvas = FindObjectOfType<Canvas>();
        if (sceneCanvas == null)
        {
            var canvasGO = new GameObject("AchievementViewer_Canvas");
            sceneCanvas = canvasGO.AddComponent<Canvas>();
            sceneCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGO);
        }

        // Root panel
        menuRoot = new GameObject("AchievementViewer_Panel");
        menuRoot.transform.SetParent(sceneCanvas.transform, false);
        var rootRt = menuRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.1f, 0.1f);
        rootRt.anchorMax = new Vector2(0.9f, 0.9f);
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var bg = menuRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        // Title bar
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(menuRoot.transform, false);
        var titleRt = titleGO.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.92f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Achievements";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        // Close button
        var closeGO = new GameObject("CloseButton");
        closeGO.transform.SetParent(menuRoot.transform, false);
        var closeRt = closeGO.AddComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.95f, 0.94f);
        closeRt.anchorMax = new Vector2(0.995f, 0.995f);
        closeRt.sizeDelta = new Vector2(0, 0);
        var closeBtn = closeGO.AddComponent<Button>();
        var closeImg = closeGO.AddComponent<Image>();
        closeImg.color = new Color(0.9f, 0.2f, 0.2f);
        var closeLabel = new GameObject("Label");
        closeLabel.transform.SetParent(closeGO.transform, false);
        var closeLabelText = closeLabel.AddComponent<TextMeshProUGUI>();
        closeLabelText.text = "X";
        closeLabelText.fontSize = 18;
        closeLabelText.alignment = TextAlignmentOptions.Center;
        closeLabelText.color = Color.white;
        closeBtn.onClick.AddListener(() => menuRoot.SetActive(false));

        // Scroll viewport
        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(menuRoot.transform, false);
        var viewportRt = viewportGO.AddComponent<RectTransform>();
        viewportRt.anchorMin = new Vector2(0.05f, 0.05f);
        viewportRt.anchorMax = new Vector2(0.95f, 0.9f);
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        var maskImg = viewportGO.AddComponent<Image>();
        maskImg.color = new Color(0f, 0f, 0f, 0f);
        var mask = viewportGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content (vertical layout)
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        contentRoot = contentGO.AddComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(0, 0);

        var layout = contentGO.AddComponent<VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.spacing = 6f;
        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ScrollRect
        var scroll = menuRoot.AddComponent<ScrollRect>();
        scroll.content = contentRoot;
        scroll.viewport = viewportRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        // Move the viewport object into scroll hierarchy properly
        viewportGO.transform.SetParent(menuRoot.transform, false);

        // Hook the scroll rect to the viewport and content
        scroll.content = contentRoot;
        scroll.viewport = viewportRt;

        // Start hidden
        menuRoot.SetActive(false);

        DontDestroyOnLoad(menuRoot);
    }

    private void RefreshContents()
    {
        if (menuRoot == null || contentRoot == null) return;

        // Clear previous entries
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }

        var mgr = AchievementManager.Instance;
        if (mgr == null)
        {
            AddInfoRow("Error", "AchievementManager not found in scene.");
            return;
        }

        var unlocked = mgr.GetUnlockedAchievementIds();
        if (unlocked == null || unlocked.Count == 0)
        {
            AddInfoRow("No achievements", "You have not unlocked any achievements yet.");
            return;
        }

        // Map ids to definition data
        var definitions = mgr.allAchievements;
        var byId = new Dictionary<string, AchievementData>();
        if (definitions != null)
        {
            foreach (var d in definitions)
            {
                if (d != null && !string.IsNullOrWhiteSpace(d.id))
                    byId[d.id] = d;
            }
        }

        foreach (var id in unlocked)
        {
            AchievementData data = null;
            byId.TryGetValue(id, out data);

            string title = data != null ? data.title : id;
            string desc = data != null ? data.description : "(no description)";

            AddInfoRow(title, desc);
        }
    }

    private void AddInfoRow(string title, string description)
    {
        var row = new GameObject("Row");
        row.transform.SetParent(contentRoot, false);
        var rt = row.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 60);

        // Title
        var tGO = new GameObject("Title");
        tGO.transform.SetParent(row.transform, false);
        var tRt = tGO.AddComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 0.5f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.offsetMin = new Vector2(6f, -30f);
        tRt.offsetMax = new Vector2(-6f, 0f);
        var tText = tGO.AddComponent<TextMeshProUGUI>();
        tText.text = title;
        tText.fontSize = 18;
        tText.color = Color.white;
        tText.alignment = TextAlignmentOptions.TopLeft;

        // Description
        var dGO = new GameObject("Description");
        dGO.transform.SetParent(row.transform, false);
        var dRt = dGO.AddComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(1f, 0.5f);
        dRt.offsetMin = new Vector2(6f, 0f);
        dRt.offsetMax = new Vector2(-6f, 30f);
        var dText = dGO.AddComponent<TextMeshProUGUI>();
        dText.text = description;
        dText.fontSize = 14;
        dText.color = new Color(0.9f, 0.9f, 0.9f);
        dText.alignment = TextAlignmentOptions.BottomLeft;
    }
}
