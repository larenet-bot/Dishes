using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using UnityObject = UnityEngine.Object;

public static class CreateAchievementViewerMenu
{
    [MenuItem("GameObject/Dishes/Create Achievement Viewer", priority = 0)]
    public static void CreateViewer()
    {
        try
        {
            // Create parent GameObject
            GameObject go = new GameObject("AchievementViewer");
            Undo.RegisterCreatedObjectUndo(go, "Create AchievementViewer");
            var viewer = go.AddComponent<AchievementViewer>();

            // Try to find an existing Canvas in the scene to parent under.
            Canvas found = UnityObject.FindObjectOfType<Canvas>();
            GameObject canvasGO = null;
            Canvas canvas = null;
            if (found == null)
            {
                // create a Canvas
                canvasGO = new GameObject("Canvas");
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas for AchievementViewer");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                // parent the viewer to that canvas so preview layout is visible immediately
                go.transform.SetParent(canvasGO.transform, false);
                viewer.targetCanvas = canvas;
            }
            else
            {
                go.transform.SetParent(found.transform, false);
                viewer.targetCanvas = found;
            }

            // Create an editable panel child that will be enabled/disabled at runtime.
            var panel = new GameObject("AchievementViewer_Panel");
            Undo.RegisterCreatedObjectUndo(panel, "Create AchievementViewer Panel");
            panel.transform.SetParent(go.transform, false);

            var panelRt = EnsureRectTransform(panel);
            panelRt.sizeDelta = new Vector2(800, 600);

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.8f);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panel.transform, false);
            var titleText = titleGO.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.text = "Achievements";
            titleText.fontSize = 24;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            var titleRt = EnsureRectTransform(titleGO);
            titleRt.anchorMin = new Vector2(0f, 0.92f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            // Close button
            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(panel.transform, false);
            var closeBtn = closeGO.AddComponent<Button>();
            var closeImg = closeGO.AddComponent<Image>();
            closeImg.color = new Color(0.9f, 0.2f, 0.2f);
            var closeRt = EnsureRectTransform(closeGO);
            closeRt.anchorMin = new Vector2(0.95f, 0.94f);
            closeRt.anchorMax = new Vector2(0.995f, 0.995f);
            closeRt.sizeDelta = new Vector2(40, 30);
            var closeLabel = new GameObject("Label");
            closeLabel.transform.SetParent(closeGO.transform, false);
            var closeLabelText = closeLabel.AddComponent<Text>();
            closeLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeLabelText.text = "X";
            closeLabelText.alignment = TextAnchor.MiddleCenter;
            closeLabelText.color = Color.white;
            closeLabelText.fontSize = 18;
            closeBtn.onClick.AddListener(() => panel.SetActive(false));

            // ScrollRect (viewport + content)
            var scrollGO = new GameObject("ScrollRect");
            scrollGO.transform.SetParent(panel.transform, false);
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            var scrollRt = EnsureRectTransform(scrollGO);
            scrollRt.anchorMin = new Vector2(0.05f, 0.05f);
            scrollRt.anchorMax = new Vector2(0.95f, 0.9f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;

            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRt = EnsureRectTransform(viewportGO);
            viewportRt.anchorMin = new Vector2(0f, 0f);
            viewportRt.anchorMax = new Vector2(1f, 1f);
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            var maskImg = viewportGO.AddComponent<Image>();
            maskImg.color = new Color(0f, 0f, 0f, 0f);
            var mask = viewportGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRt = EnsureRectTransform(contentGO);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0, 0);

            var layout = contentGO.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.spacing = 6f;
            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;
            scrollRect.viewport = viewportRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Start disabled so duck click enables it
            panel.SetActive(false);

            // Expose created panel on the viewer so ToggleMenu works immediately
            viewer.menuRoot = panel;

            // Select the created viewer in the editor
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
        catch (Exception ex)
        {
            Debug.LogError($"CreateAchievementViewerMenu.CreateViewer failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // Ensure the GameObject has a RectTransform and return it (defensive helper).
    private static RectTransform EnsureRectTransform(GameObject go)
    {
        if (go == null) return null;
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = Undo.AddComponent<RectTransform>(go);
        return rt;
    }
}