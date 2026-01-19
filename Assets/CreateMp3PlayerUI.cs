using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CreateMp3PlayerUI
{
    private const string SongButtonPrefabPath = "Assets/MP3SongButton.prefab";

    [MenuItem("Tools/Create MP3 Player UI (Full Fixed)")]
    public static void Create()
    {
        // ================= Canvas =================
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (!canvas)
        {
            var c = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = c.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = c.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        if (!Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>())
        {
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // ================= Panel =================
        var panel = new GameObject("MP3PlayerPanel",
            typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(820, 520);

        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

        var ui = panel.AddComponent<Mp3PlayerUI>();

        // ================= Header =================
        var header = CreateArea(panel.transform, "Header", 60);
        // Lock header transforms so it won't be affected by layout
        var hr = header.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0f, 1f);
        hr.anchorMax = new Vector2(1f, 1f);
        hr.pivot = new Vector2(0.5f, 1f);
        hr.sizeDelta = new Vector2(0, 56);
        hr.anchoredPosition = new Vector2(0, -8);

        CreateLabel(header, "CurrentSongText", "No song playing", 20, TextAlignmentOptions.MidlineLeft);

        CreateButton(header, "CloseButton", "X", 32, out var closeBtn);

        // Fix close button layout (pinned to right)
        var closeRt = closeBtn.GetComponent<RectTransform>();
        closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 0.5f);
        closeRt.pivot = new Vector2(1f, 0.5f);
        closeRt.sizeDelta = new Vector2(36, 32);
        closeRt.anchoredPosition = new Vector2(-10, 0);

        // Stretch current song text with padding
        var currentSongGO = header.transform.Find("CurrentSongText");
        if (currentSongGO)
        {
            var ctrt = currentSongGO.GetComponent<RectTransform>();
            ctrt.anchorMin = new Vector2(0f, 0f);
            ctrt.anchorMax = new Vector2(1f, 1f);
            ctrt.offsetMin = new Vector2(12, 4);
            ctrt.offsetMax = new Vector2(-52, -4);

            var ct = currentSongGO.GetComponent<TextMeshProUGUI>();
            ct.enableAutoSizing = false;
            ct.alignment = TextAlignmentOptions.MidlineLeft;
            ct.color = Color.white;
        }

        // ================= Queue Label =================
        var queueLabelArea = CreateArea(panel.transform, "QueueLabel", 36);
        CreateLabel(queueLabelArea, "QueueText", "Queue", 16, TextAlignmentOptions.MidlineLeft);

        // ================= ScrollRect =================
        var scrollGO = new GameObject("SongScroll",
            typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollGO.transform.SetParent(panel.transform, false);

        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.05f, 0.25f);
        scrollRT.anchorMax = new Vector2(0.95f, 0.75f);
        scrollRT.offsetMin = scrollRT.offsetMax = Vector2.zero;

        scrollGO.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.06f);
        scrollGO.GetComponent<Mask>().showMaskGraphic = false;

        var scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.horizontal = false;

        // ---------- Content ----------
        var content = new GameObject("SongListContent",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        content.transform.SetParent(scrollGO.transform, false);

        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.padding = new RectOffset(6, 6, 6, 6);

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRT;

        // ================= Footer Controls =================
        var footer = CreateArea(panel.transform, "Footer", 70);
        // Lock footer like header so layout doesn't drift
        var fr = footer.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0f, 0f);
        fr.anchorMax = new Vector2(1f, 0f);
        fr.pivot = new Vector2(0.5f, 0f);
        fr.sizeDelta = new Vector2(0, 72);
        fr.anchoredPosition = new Vector2(0, 8);

        CreateButton(footer, "PrevButton", "<<", 28, out var prevBtn);
        CreateButton(footer, "PlayNextButton", ">>", 28, out var nextBtn);

        // Create a centered toggle row and move Loop/Shuffle under it
        var controls = new GameObject("Controls", typeof(RectTransform));
        controls.transform.SetParent(footer.transform, false);
        var controlsRt = controls.GetComponent<RectTransform>();
        controlsRt.anchorMin = controlsRt.anchorMax = new Vector2(0.5f, 0.5f);
        controlsRt.sizeDelta = new Vector2(420, 40);
        controlsRt.anchoredPosition = Vector2.zero;

        // Place prev/next on left/right inside footer for simple layout
        var prevRt = prevBtn.GetComponent<RectTransform>();
        prevRt.anchorMin = prevRt.anchorMax = new Vector2(0f, 0.5f);
        prevRt.pivot = new Vector2(0f, 0.5f);
        prevRt.sizeDelta = new Vector2(64, 40);
        prevRt.anchoredPosition = new Vector2(12, 0);

        var nextRt = nextBtn.GetComponent<RectTransform>();
        nextRt.anchorMin = nextRt.anchorMax = new Vector2(1f, 0.5f);
        nextRt.pivot = new Vector2(1f, 0.5f);
        nextRt.sizeDelta = new Vector2(64, 40);
        nextRt.anchoredPosition = new Vector2(-12, 0);

        // Toggle row
        var toggleRow = new GameObject("ToggleRow",
            typeof(RectTransform), typeof(HorizontalLayoutGroup));
        toggleRow.transform.SetParent(controls.transform, false);

        var trRt = toggleRow.GetComponent<RectTransform>();
        trRt.anchorMin = trRt.anchorMax = new Vector2(0.5f, 0.5f);
        trRt.sizeDelta = new Vector2(240, 32);

        var hlg = toggleRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = false;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.padding = new RectOffset(4, 4, 4, 4);

        // Create toggles and parent them under toggleRow
        CreateToggle(toggleRow, "LoopToggle", "Loop", out var loopToggle);
        CreateToggle(toggleRow, "ShuffleToggle", "Shuffle", out var shuffleToggle);

        // ================= Song Button Prefab =================
        // Build an explicit prefab with Label, QueueButton, LoopButton and EnableToggle
        var prefab = new GameObject("MP3SongButton", typeof(RectTransform), typeof(Image), typeof(Button));
        prefab.transform.SetParent(null, false);

        // Background image
        var bg = prefab.GetComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        // LayoutElement so VerticalLayoutGroup controls size (stable list - no squish)
        var prefabLayout = prefab.AddComponent<LayoutElement>();
        prefabLayout.preferredHeight = 40f;
        prefabLayout.minHeight = 40f;
        prefabLayout.flexibleHeight = 0;
        prefabLayout.flexibleWidth = 1;

        // Label (center-left, leaves room for toggle + buttons)
        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(prefab.transform, false);

        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.12f, 0f);
        labelRT.anchorMax = new Vector2(0.72f, 1f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Song Name";
        tmp.fontSize = 18;
        tmp.enableAutoSizing = false;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.color = Color.white;

        // Queue button (small, right side)
        var queueBtnGO = new GameObject("QueueButton", typeof(RectTransform), typeof(Image), typeof(Button));
        queueBtnGO.transform.SetParent(prefab.transform, false);
        var qbRt = queueBtnGO.GetComponent<RectTransform>();
        qbRt.anchorMin = new Vector2(0.74f, 0.1f);
        qbRt.anchorMax = new Vector2(0.84f, 0.9f);
        qbRt.offsetMin = qbRt.offsetMax = Vector2.zero;
        var qbImg = queueBtnGO.GetComponent<Image>();
        qbImg.color = new Color(0.12f, 0.6f, 0.9f, 1f);
        var qbTextGO = new GameObject("Text", typeof(RectTransform));
        qbTextGO.transform.SetParent(queueBtnGO.transform, false);
        var qbText = qbTextGO.AddComponent<TextMeshProUGUI>();
        qbText.text = "Q";
        qbText.fontSize = 14;
        qbText.alignment = TextAlignmentOptions.Center;
        qbText.color = Color.white;

        // Loop button (small, rightmost)
        var loopBtnGO = new GameObject("LoopButton", typeof(RectTransform), typeof(Image), typeof(Button));
        loopBtnGO.transform.SetParent(prefab.transform, false);
        var lbRt = loopBtnGO.GetComponent<RectTransform>();
        lbRt.anchorMin = new Vector2(0.86f, 0.1f);
        lbRt.anchorMax = new Vector2(0.96f, 0.9f);
        lbRt.offsetMin = lbRt.offsetMax = Vector2.zero;
        var lbImg = loopBtnGO.GetComponent<Image>();
        lbImg.color = new Color(0.9f, 0.8f, 0.2f, 1f);
        var lbTextGO = new GameObject("Text", typeof(RectTransform));
        lbTextGO.transform.SetParent(loopBtnGO.transform, false);
        var lbText = lbTextGO.AddComponent<TextMeshProUGUI>();
        lbText.text = "⤾";
        lbText.fontSize = 14;
        lbText.alignment = TextAlignmentOptions.Center;
        lbText.color = Color.white;

        // Enable Toggle (left side checkbox style)
        var enableToggleGO = new GameObject("EnableToggle",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
        enableToggleGO.transform.SetParent(prefab.transform, false);

        var etRt = enableToggleGO.GetComponent<RectTransform>();
        etRt.anchorMin = new Vector2(0.02f, 0.2f);
        etRt.anchorMax = new Vector2(0.10f, 0.8f);
        etRt.offsetMin = Vector2.zero;
        etRt.offsetMax = Vector2.zero;

        var etBg = enableToggleGO.GetComponent<Image>();
        etBg.color = new Color(0.2f, 0.8f, 0.2f, 1f);

        var etToggle = enableToggleGO.GetComponent<Toggle>();
        etToggle.isOn = true;

        // Checkmark
        var checkGO = new GameObject("Checkmark",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        checkGO.transform.SetParent(enableToggleGO.transform, false);

        var checkImg = checkGO.GetComponent<Image>();
        checkImg.color = Color.white;

        var chkRt = checkGO.GetComponent<RectTransform>();
        chkRt.anchorMin = new Vector2(0.2f, 0.2f);
        chkRt.anchorMax = new Vector2(0.8f, 0.8f);
        chkRt.offsetMin = chkRt.offsetMax = Vector2.zero;

        etToggle.graphic = checkImg;
        etToggle.targetGraphic = etBg;

        // Add SongButtonUI component (use non-generic AddComponent to avoid generic constraint compile issue)
        prefab.AddComponent(typeof(SongButtonUI));
        // Note: runtime code (Mp3PlayerUI) should GetComponent<SongButtonUI>() on instantiated buttons and wire fields if necessary.
        // This keeps the editor creation script free of assembly/generic constraint issues.

        // Save prefab asset
        var prefabAsset = PrefabUtility.SaveAsPrefabAsset(prefab, SongButtonPrefabPath);
        Object.DestroyImmediate(prefab);

        // ================= Wire =================
        ui.panel = panel;
        ui.currentSongText = panel.transform.Find("Header/CurrentSongText").GetComponent<TextMeshProUGUI>();
        ui.queueText = panel.transform.Find("QueueLabel/QueueText").GetComponent<TextMeshProUGUI>();
        ui.songListParent = contentRT;
        ui.songButtonPrefab = prefabAsset.GetComponent<Button>();
        ui.prevButton = prevBtn.GetComponent<Button>();
        ui.nextButton = nextBtn.GetComponent<Button>();
        ui.closeButton = closeBtn.GetComponent<Button>();
        ui.loopToggle = loopToggle.GetComponent<Toggle>();
        ui.shuffleToggle = shuffleToggle.GetComponent<Toggle>();

        panel.SetActive(false);
        Selection.activeGameObject = panel;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("MP3 Player UI created (full feature set, fixed layout, enable/disable support).");
    }

    // ================= Helpers =================

    private static GameObject CreateArea(Transform parent, string name, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = height;
        return go;
    }

    private static void CreateLabel(GameObject parent, string name, string text, int size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.enableAutoSizing = false;
        tmp.alignment = align;
        tmp.color = Color.white;

        // Default label rect: stretch to fill parent unless caller adjusts
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void CreateButton(GameObject parent, string name, string label, int fontSize, out GameObject buttonGO)
    {
        buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent.transform, false);

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(buttonGO.transform, false);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = false;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        // Default button text stretches to fill button
        var rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void CreateToggle(GameObject parent, string name, string label, out GameObject toggleGO)
    {
        toggleGO = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        toggleGO.transform.SetParent(parent.transform, false);

        // Label as child of toggle for consistent layout
        CreateLabel(toggleGO, "Label", label, 14, TextAlignmentOptions.MidlineLeft);

        // Adjust toggle rect so it sizes correctly when placed in layout groups
        var rt = toggleGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 24);
    }
}
