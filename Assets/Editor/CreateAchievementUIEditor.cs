using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CreateAchievementUIEditor
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath = PrefabFolder + "/AchievementEntry.prefab";

    [MenuItem("Tools/Create Achievement UI & Hook Duck")]
    public static void CreateAchievementUIAndHook()
    {
        // Ensure prefab folder exists
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // Find or create Canvas
        Canvas sceneCanvas = Object.FindObjectOfType<Canvas>();

        if (sceneCanvas == null)
        {
            var canvasGO = new GameObject(
                "Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

            sceneCanvas = canvasGO.GetComponent<Canvas>();
            sceneCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            Debug.Log("[CreateAchievementUI] Created new Canvas.");
        }

        // =========================
        // WINDOW
        // =========================

        GameObject window = new GameObject(
            "AchievementWindow",
            typeof(RectTransform),
            typeof(Image)
        );

        window.transform.SetParent(sceneCanvas.transform, false);

        var windowRt = window.GetComponent<RectTransform>();

        windowRt.anchorMin = new Vector2(0.1f, 0.1f);
        windowRt.anchorMax = new Vector2(0.9f, 0.9f);
        windowRt.offsetMin = Vector2.zero;
        windowRt.offsetMax = Vector2.zero;

        var bg = window.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        // =========================
        // TITLE
        // =========================

        GameObject titleGO = new GameObject(
            "Title",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        titleGO.transform.SetParent(window.transform, false);

        var titleRt = titleGO.GetComponent<RectTransform>();

        titleRt.anchorMin = new Vector2(0f, 0.92f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        var titleText = titleGO.GetComponent<TextMeshProUGUI>();

        titleText.text = "Achievements";
        titleText.fontSize = 30;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        // =========================
        // CLOSE BUTTON
        // =========================

        GameObject closeGO = new GameObject(
            "CloseButton",
            typeof(RectTransform),
            typeof(Button),
            typeof(Image)
        );

        closeGO.transform.SetParent(window.transform, false);

        var closeRt = closeGO.GetComponent<RectTransform>();

        closeRt.anchorMin = new Vector2(0.93f, 0.93f);
        closeRt.anchorMax = new Vector2(0.99f, 0.99f);
        closeRt.offsetMin = Vector2.zero;
        closeRt.offsetMax = Vector2.zero;

        var closeBtn = closeGO.GetComponent<Button>();

        var closeImg = closeGO.GetComponent<Image>();
        closeImg.color = new Color(0.8f, 0.2f, 0.2f);

        GameObject closeLabel = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        closeLabel.transform.SetParent(closeGO.transform, false);

        var closeLabelRt = closeLabel.GetComponent<RectTransform>();

        closeLabelRt.anchorMin = Vector2.zero;
        closeLabelRt.anchorMax = Vector2.one;
        closeLabelRt.offsetMin = Vector2.zero;
        closeLabelRt.offsetMax = Vector2.zero;

        var closeLabelText = closeLabel.GetComponent<TextMeshProUGUI>();

        closeLabelText.text = "X";
        closeLabelText.fontSize = 24;
        closeLabelText.alignment = TextAlignmentOptions.Center;
        closeLabelText.color = Color.white;

        closeBtn.onClick.AddListener(() => window.SetActive(false));

        // =========================
        // VIEWPORT
        // =========================

        GameObject viewportGO = new GameObject(
            "Viewport",
            typeof(RectTransform),
            typeof(Image),
            typeof(Mask)
        );

        viewportGO.transform.SetParent(window.transform, false);

        var viewportRt = viewportGO.GetComponent<RectTransform>();

        viewportRt.anchorMin = new Vector2(0.05f, 0.05f);
        viewportRt.anchorMax = new Vector2(0.95f, 0.9f);
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;

        var maskImg = viewportGO.GetComponent<Image>();

        // IMPORTANT
        maskImg.color = new Color(0f, 0f, 0f, 0.01f);

        var mask = viewportGO.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        // =========================
        // CONTENT
        // =========================

        GameObject contentGO = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter)
        );

        contentGO.transform.SetParent(viewportGO.transform, false);

        var contentRt = contentGO.GetComponent<RectTransform>();

        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);

        contentRt.offsetMin = new Vector2(0, 0);
        contentRt.offsetMax = new Vector2(0, 0);

        contentRt.anchoredPosition = Vector2.zero;

        var layout = contentGO.GetComponent<VerticalLayoutGroup>();

        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 8f;

        var fitter = contentGO.GetComponent<ContentSizeFitter>();

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // =========================
        // SCROLL RECT
        // =========================

        var scrollRect = window.AddComponent<ScrollRect>();

        scrollRect.content = contentRt;
        scrollRect.viewport = viewportRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 25f;

        // Start hidden
        window.SetActive(false);

        // ==================================================
        // ACHIEVEMENT ENTRY PREFAB
        // ==================================================

        GameObject entryGO = new GameObject(
            "AchievementEntry",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement),
            typeof(HorizontalLayoutGroup)
        );

        var entryRt = entryGO.GetComponent<RectTransform>();

        entryRt.sizeDelta = new Vector2(0, 80);

        // Background
        var entryBg = entryGO.GetComponent<Image>();
        entryBg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        // LayoutElement
        var layoutElement = entryGO.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 80f;

        // Horizontal Layout
        var hLayout = entryGO.GetComponent<HorizontalLayoutGroup>();

        hLayout.padding = new RectOffset(8, 8, 8, 8);
        hLayout.spacing = 10f;
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        // =========================
        // ICON
        // =========================

        GameObject iconGO = new GameObject(
            "Icon",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement)
        );

        iconGO.transform.SetParent(entryGO.transform, false);

        var iconLayout = iconGO.GetComponent<LayoutElement>();
        iconLayout.preferredWidth = 64;
        iconLayout.preferredHeight = 64;

        var iconImg = iconGO.GetComponent<Image>();

        // =========================
        // TEXT CONTAINER
        // =========================

        GameObject texts = new GameObject(
            "Texts",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(LayoutElement)
        );

        texts.transform.SetParent(entryGO.transform, false);

        var textsLayout = texts.GetComponent<LayoutElement>();
        textsLayout.flexibleWidth = 1;

        var textGroup = texts.GetComponent<VerticalLayoutGroup>();

        textGroup.spacing = 4;
        textGroup.childAlignment = TextAnchor.MiddleLeft;
        textGroup.childControlHeight = true;
        textGroup.childControlWidth = true;
        textGroup.childForceExpandHeight = false;
        textGroup.childForceExpandWidth = true;

        // =========================
        // TITLE
        // =========================

        GameObject tGO = new GameObject(
            "Title",
            typeof(RectTransform),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement)
        );

        tGO.transform.SetParent(texts.transform, false);

        var tText = tGO.GetComponent<TextMeshProUGUI>();

        tText.text = "Achievement Title";
        tText.fontSize = 22;
        tText.color = Color.white;
        tText.alignment = TextAlignmentOptions.Left;

        // =========================
        // DESCRIPTION
        // =========================

        GameObject dGO = new GameObject(
            "Description",
            typeof(RectTransform),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement)
        );

        dGO.transform.SetParent(texts.transform, false);

        var dText = dGO.GetComponent<TextMeshProUGUI>();

        dText.text = "Achievement description goes here.";
        dText.fontSize = 16;
        dText.color = new Color(0.85f, 0.85f, 0.85f);
        dText.alignment = TextAlignmentOptions.Left;

        // =========================
        // LOCKED OVERLAY
        // =========================

        GameObject lockedGO = new GameObject(
            "LockedOverlay",
            typeof(RectTransform),
            typeof(Image)
        );

        lockedGO.transform.SetParent(entryGO.transform, false);

        var lockedRt = lockedGO.GetComponent<RectTransform>();

        lockedRt.anchorMin = Vector2.zero;
        lockedRt.anchorMax = Vector2.one;
        lockedRt.offsetMin = Vector2.zero;
        lockedRt.offsetMax = Vector2.zero;

        var lockedImg = lockedGO.GetComponent<Image>();
        lockedImg.color = new Color(0f, 0f, 0f, 0.5f);

        // =========================
        // ENTRY SCRIPT
        // =========================

        var entryComp = entryGO.AddComponent<AchievementUIEntry>();

        entryComp.iconImage = iconImg;
        entryComp.titleText = tText;
        entryComp.descriptionText = dText;
        entryComp.lockedOverlay = lockedGO;
        entryComp.lockedIcon = null;

        // =========================
        // SAVE PREFAB
        // =========================

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(
            entryGO,
            PrefabPath
        );

        if (prefabAsset == null)
        {
            Debug.LogError(
                "[CreateAchievementUI] Failed to create prefab at " +
                PrefabPath
            );

            Object.DestroyImmediate(entryGO);
            return;
        }

        Object.DestroyImmediate(entryGO);

        AssetDatabase.SaveAssets();

        // ==================================================
        // HOOK DUCK
        // ==================================================

        var duckClick = Object.FindObjectOfType<DuckClick>();

        if (duckClick == null)
        {
            Debug.LogWarning(
                "[CreateAchievementUI] No DuckClick found in scene."
            );
        }
        else
        {
            var viewer = duckClick.GetComponent<DuckAchievementViewer>();

            if (viewer == null)
            {
                viewer = duckClick.gameObject.AddComponent<DuckAchievementViewer>();
            }

            viewer.achievementWindow = window;
            viewer.contentParent = contentGO.transform;

            var prefabObj =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            if (prefabObj != null)
            {
                viewer.entryPrefab =
                    prefabObj.GetComponent<AchievementUIEntry>();
            }

            Debug.Log(
                "[CreateAchievementUI] Hooked DuckAchievementViewer on duck: " +
                duckClick.gameObject.name
            );
        }

        // Save Scene
        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene()
        );

        Debug.Log(
            "[CreateAchievementUI] AchievementWindow created successfully."
        );
    }
}