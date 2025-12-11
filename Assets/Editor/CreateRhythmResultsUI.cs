using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class RhythmResultsUICreator
{
    [MenuItem("GameObject/UI/Create Rhythm Results UI", false, 2000)]
    public static void CreateUI()
    {
        // Find or create Canvas
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
        GameObject panel = new GameObject("ResultsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create ResultsPanel");
        panel.transform.SetParent(canvas.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(800, 520);
        panelRt.anchoredPosition = Vector2.zero;
        var panelImg = panel.GetComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.8f);

        // Texts
        var title = CreateTMP("Title", "Results", 36, new Vector2(0, 200), new Vector2(760, 60), panel.transform);
        var rank = CreateTMP("RankText", "Rank: -", 32, new Vector2(0, 140), new Vector2(760, 48), panel.transform);
        var score = CreateTMP("ScoreText", "Score: 0", 22, new Vector2(0, 100), new Vector2(760, 36), panel.transform);

        // Stats group (vertical)
        GameObject statsGroup = new GameObject("StatsGroup", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(statsGroup, "Create StatsGroup");
        statsGroup.transform.SetParent(panel.transform, false);
        var statsRt = statsGroup.GetComponent<RectTransform>();
        statsRt.anchoredPosition = new Vector2(0, 0);
        statsRt.sizeDelta = new Vector2(680, 140);
        var vlg = statsGroup.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 6;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;
        var csf = statsGroup.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var perfect = CreateTMP("PerfectText", "Perfect: 0", 18, Vector2.zero, new Vector2(660, 26), statsGroup.transform, TextAlignmentOptions.Left);
        var good = CreateTMP("GoodText", "Good: 0", 18, Vector2.zero, new Vector2(660, 26), statsGroup.transform, TextAlignmentOptions.Left);
        var bad = CreateTMP("BadText", "Bad: 0", 18, Vector2.zero, new Vector2(660, 26), statsGroup.transform, TextAlignmentOptions.Left);
        var miss = CreateTMP("MissText", "Miss: 0", 18, Vector2.zero, new Vector2(660, 26), statsGroup.transform, TextAlignmentOptions.Left);

        var reward = CreateTMP("RewardText", "Reward: $0.00", 24, new Vector2(0, -120), new Vector2(760, 36), panel.transform);

        // Buttons row
        GameObject btnRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        Undo.RegisterCreatedObjectUndo(btnRow, "Create Buttons");
        btnRow.transform.SetParent(panel.transform, false);
        var btnRowRt = btnRow.GetComponent<RectTransform>();
        btnRowRt.anchoredPosition = new Vector2(0, -200);
        btnRowRt.sizeDelta = new Vector2(680, 48);
        var hlg = btnRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        // Create Continue button only
        GameObject CreateButton(string name, string label)
        {
            GameObject btn = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(btn, "Create " + name);
            btn.transform.SetParent(btnRow.transform, false);
            var rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 45);
            var img = btn.GetComponent<Image>();
            img.color = new Color(0.16f, 0.16f, 0.16f, 1f);
            var txt = CreateTMP("Text", label, 20, Vector2.zero, new Vector2(200, 45), btn.transform);
            txt.alignment = TextAlignmentOptions.Center;
            return btn;
        }

        var continueBtn = CreateButton("ContinueButton", "Continue");

        // Find or create manager
        RhythmGameEndManager manager = Object.FindObjectOfType<RhythmGameEndManager>();
        if (manager == null)
        {
            GameObject mgrGO = new GameObject("RhythmGameEndManager");
            Undo.RegisterCreatedObjectUndo(mgrGO, "Create RhythmGameEndManager");
            manager = mgrGO.AddComponent<RhythmGameEndManager>();
        }

        // Assign manager references
        Undo.RecordObject(manager, "Assign RhythmGameEndManager refs");
        manager.resultsPanel = panel;
        manager.rankText = rank;
        manager.scoreText = score;
        manager.perfectText = perfect;
        manager.goodText = good;
        manager.badText = bad;
        manager.missText = miss;
        manager.rewardText = reward;
        EditorUtility.SetDirty(manager);

        // Hide results initially
        panel.SetActive(false);

        // Wire the continue button to manager.ContinueAndExit()
        var continueBtnComp = continueBtn.GetComponent<Button>();
        Undo.RecordObject(continueBtnComp, "Add Continue Listener");
        UnityEventTools.AddPersistentListener(continueBtnComp.onClick, manager.ContinueAndExit);
        EditorUtility.SetDirty(continueBtnComp);

        // Select panel in hierarchy
        Selection.activeGameObject = panel;
    }

    private static Canvas GetTargetCanvas()
    {
        if (Selection.activeGameObject != null)
        {
            var selCanvas = Selection.activeGameObject.GetComponent<Canvas>();
            if (selCanvas != null) return selCanvas;
        }

        var byName = GameObject.Find("Canvas") ?? GameObject.Find("UI Canvas") ?? GameObject.Find("Main Canvas");
        if (byName != null)
        {
            var c = byName.GetComponent<Canvas>();
            if (c != null) return c;
        }

        var found = Object.FindObjectOfType<Canvas>();
        if (found != null) return found;

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
