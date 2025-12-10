using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class BeatmapSelectorUICreator
{
    [MenuItem("GameObject/UI/Create Beatmap Selector UI", false, 2010)]
    public static void CreateUI()
    {
        Canvas canvas = GetTargetCanvas();

        // Helper to create TMP text
        TextMeshProUGUI CreateTMP(string name, string content, int fontSize, Vector2 anchoredPos, Vector2 size, Transform parent, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.enableAutoSizing = false;
            return tmp;
        }

        // Panel root
        GameObject panel = new GameObject("BeatmapSelectorPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create BeatmapSelectorPanel");
        panel.transform.SetParent(canvas.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(760, 300);
        panelRt.anchoredPosition = Vector2.zero;
        var panelImg = panel.GetComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.8f);

        // Title
        var title = CreateTMP("Title", "Select Difficulty", 30, new Vector2(0, 110), new Vector2(720, 48), panel.transform);

        // Difficulty buttons container (horizontal)
        GameObject diffRow = new GameObject("DifficultyRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        Undo.RegisterCreatedObjectUndo(diffRow, "Create DifficultyRow");
        diffRow.transform.SetParent(panel.transform, false);
        var diffRt = diffRow.GetComponent<RectTransform>();
        diffRt.anchoredPosition = new Vector2(0, 20);
        diffRt.sizeDelta = new Vector2(700, 60);
        var hlg = diffRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        // Helper to create a button with TMP child and return Button
        Button CreateButton(string name, string label)
        {
            GameObject btn = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(btn, "Create " + name);
            btn.transform.SetParent(diffRow.transform, false);
            var rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 56);
            var img = btn.GetComponent<Image>();
            img.color = new Color(0.18f, 0.18f, 0.18f, 1f);
            var tmp = CreateTMP("Text", label, 20, Vector2.zero, new Vector2(200, 56), btn.transform);
            tmp.alignment = TextAlignmentOptions.Center;
            return btn.GetComponent<Button>();
        }

        var easyBtn = CreateButton("EasyButton", "Easy");
        var normalBtn = CreateButton("NormalButton", "Normal");
        var hardBtn = CreateButton("HardButton", "Hard");

        // Selected label
        var selectedLabel = CreateTMP("SelectedLabel", "Selected: -", 18, new Vector2(0, -40), new Vector2(720, 28), panel.transform, TextAlignmentOptions.Center);

        // Start button
        GameObject startRow = new GameObject("StartRow", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(startRow, "Create StartRow");
        startRow.transform.SetParent(panel.transform, false);
        var startRt = startRow.GetComponent<RectTransform>();
        startRt.anchoredPosition = new Vector2(0, -110);
        startRt.sizeDelta = new Vector2(720, 48);

        Button startBtn;
        {
            GameObject btn = new GameObject("StartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(btn, "Create StartButton");
            btn.transform.SetParent(startRow.transform, false);
            var rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220, 42);
            var img = btn.GetComponent<Image>();
            img.color = new Color(0.08f, 0.6f, 0.1f, 1f);
            var tmp = CreateTMP("Text", "Start", 20, Vector2.zero, new Vector2(220, 42), btn.transform);
            tmp.alignment = TextAlignmentOptions.Center;
            startBtn = btn.GetComponent<Button>();
        }

        // Find or create BeatmapSelector and assign references
        BeatmapSelector selector = Object.FindObjectOfType<BeatmapSelector>();
        if (selector == null)
        {
            GameObject selGO = new GameObject("BeatmapSelector");
            Undo.RegisterCreatedObjectUndo(selGO, "Create BeatmapSelector");
            selector = selGO.AddComponent<BeatmapSelector>();
        }

        // Auto-wire NoteSpawner / RhythmMiniGameToggle if present
        var spawner = Object.FindObjectOfType<NoteSpawner>();
        var mini = Object.FindObjectOfType<RhythmMiniGameToggle>();

        Undo.RecordObject(selector, "Assign BeatmapSelector refs");
        selector.noteSpawner = spawner;
        selector.miniToggle = mini;
        selector.difficultyButtons = new Button[] { easyBtn, normalBtn, hardBtn };
        selector.startButton = startBtn;
        selector.selectedLabel = selectedLabel;
        EditorUtility.SetDirty(selector);

        // Hide panel by default
        panel.SetActive(false);

        // Select created panel in Hierarchy
        Selection.activeGameObject = panel;

        // Mark scene dirty so changes persist
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    private static Canvas GetTargetCanvas()
    {
        // Prefer the selected GameObject if it is a Canvas
        if (Selection.activeGameObject != null)
        {
            var selCanvas = Selection.activeGameObject.GetComponent<Canvas>();
            if (selCanvas != null) return selCanvas;
        }

        // Try common canvas names
        var byName = GameObject.Find("Canvas") ?? GameObject.Find("UI Canvas") ?? GameObject.Find("Main Canvas");
        if (byName != null)
        {
            var c = byName.GetComponent<Canvas>();
            if (c != null) return c;
        }

        // Fallback to first found in scene
        var found = Object.FindObjectOfType<Canvas>();
        if (found != null) return found;

        // If none exist, create a new Canvas + EventSystem
        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = canvasGO.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        return canvas;
    }
}