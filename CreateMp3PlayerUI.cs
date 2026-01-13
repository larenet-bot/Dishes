csharp Assets/Editor/CreateMp3PlayerUI.cs
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utility: Creates a ready-to-use MP3 player panel UI in the current scene and saves a song-button prefab asset.
/// Usage: Menu -> Tools -> Create MP3 Player UI
/// The created panel includes a ScrollRect song list, a saved Button prefab (Assets/MP3SongButton.prefab),
/// current song label, prev/next/close buttons and loop/shuffle toggles. It also attaches the existing Mp3PlayerUI script
/// and wires its public fields.
/// </summary>
public static class CreateMp3PlayerUI
{
    private const string SongButtonPrefabPath = "Assets/MP3SongButton.prefab";

    [MenuItem("Tools/Create MP3 Player UI")]
    public static void Create()
    {
        // Ensure there's a Canvas
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }

        // Ensure EventSystem exists
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        // Create MP3PlayerPanel
        var panelGO = new GameObject("MP3PlayerPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panelGO, "Create MP3PlayerPanel");
        panelGO.transform.SetParent(canvas.transform, false);
        var rt = panelGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(720, 420);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        var panelImage = panelGO.GetComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

        // Add Mp3PlayerUI component
        var mp3UI = panelGO.AddComponent<Mp3PlayerUI>();

        // Create header area (current song + close)
        var header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(panelGO.transform, false);
        var hr = header.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0f, 1f);
        hr.anchorMax = new Vector2(1f, 1f);
        hr.pivot = new Vector2(0.5f, 1f);
        hr.sizeDelta = new Vector2(0, 60);
        hr.anchoredPosition = new Vector2(0, -10);

        // Current song text
        var currentTextGO = new GameObject("CurrentSongText", typeof(RectTransform));
        currentTextGO.transform.SetParent(header.transform, false);
        var ct = currentTextGO.AddComponent<TMP_Text>();
        ct.text = "<none>";
        ct.fontSize = 24;
        ct.alignment = TextAlignmentOptions.Center;
        var ctrt = currentTextGO.GetComponent<RectTransform>();
        ctrt.anchorMin = new Vector2(0.1f, 0f);
        ctrt.anchorMax = new Vector2(0.9f, 1f);
        ctrt.offsetMin = Vector2.zero;
        ctrt.offsetMax = Vector2.zero;

        // Close button
        var closeBtn = CreateButton("CloseButton", header.transform, new Vector2(0.92f, 0.5f), new Vector2(0.12f, 0.8f), "Close");
        // Prev/Next controls (bottom)
        var controls = new GameObject("Controls", typeof(RectTransform));
        controls.transform.SetParent(panelGO.transform, false);
        var ctrlRt = controls.GetComponent<RectTransform>();
        ctrlRt.anchorMin = new Vector2(0f, 0f);
        ctrlRt.anchorMax = new Vector2(1f, 0f);
        ctrlRt.pivot = new Vector2(0.5f, 0f);
        ctrlRt.sizeDelta = new Vector2(0, 70);
        ctrlRt.anchoredPosition = new Vector2(0, 10);

        var prevBtn = CreateButton("PrevButton", controls.transform, new Vector2(0.2f, 0.5f), new Vector2(0.2f, 0.8f), "Prev");
        var nextBtn = CreateButton("NextButton", controls.transform, new Vector2(0.8f, 0.5f), new Vector2(0.2f, 0.8f), "Next");

        // Toggles for loop/shuffle
        var togglesParent = new GameObject("Toggles", typeof(RectTransform));
        togglesParent.transform.SetParent(controls.transform, false);
        var togglesRt = togglesParent.GetComponent<RectTransform>();
        togglesRt.anchorMin = new Vector2(0.4f, 0.1f);
        togglesRt.anchorMax = new Vector2(0.6f, 0.9f);
        togglesRt.offsetMin = Vector2.zero;
        togglesRt.offsetMax = Vector2.zero;

        var loopToggle = CreateToggle("LoopToggle", togglesParent.transform, new Vector2(0.5f, 0.7f), "Loop");
        var shuffleToggle = CreateToggle("ShuffleToggle", togglesParent.transform, new Vector2(0.5f, 0.3f), "Shuffle");

        // Create ScrollRect and content for song list
        var scrollGO = new GameObject("SongListScroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask), typeof(ScrollRect));
        scrollGO.transform.SetParent(panelGO.transform, false);
        var srt = scrollGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.05f, 0.15f);
        srt.anchorMax = new Vector2(0.95f, 0.82f);
        srt.offsetMin = srt.offsetMax = Vector2.zero;
        var scrollImage = scrollGO.GetComponent<Image>();
        scrollImage.color = new Color(0.06f, 0.06f, 0.07f, 0.9f);
        var mask = scrollGO.GetComponent<Mask>();
        mask.showMaskGraphic = false;
        var scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;

        var contentGO = new GameObject("SongListContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(scrollGO.transform, false);
        var contentRt = contentGO.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 0);
        var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;
        vlg.spacing = 6;
        var csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRt;

        // Create song button prefab and save it as asset
        var prefabGO = new GameObject("MP3SongButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var prefabRt = prefabGO.GetComponent<RectTransform>();
        prefabRt.sizeDelta = new Vector2(0, 40);
        var btnImage = prefabGO.GetComponent<Image>();
        btnImage.color = new Color(0.2f, 0.2f, 0.24f, 1f);
        var btn = prefabGO.GetComponent<Button>();

        // Add TMP label
        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(prefabGO.transform, false);
        var tmp = labelGO.AddComponent<TMP_Text>();
        tmp.text = "Song";
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        var tmpRt = labelGO.GetComponent<RectTransform>();
        tmpRt.anchorMin = new Vector2(0f, 0f);
        tmpRt.anchorMax = new Vector2(1f, 1f);
        tmpRt.offsetMin = new Vector2(8f, 4f);
        tmpRt.offsetMax = new Vector2(-8f, -4f);

        // Try to set default TMP font asset (if available)
        #if TMP_PRESENT
        if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        #else
        var defaultFont = TMPro.TMP_Settings.defaultFontAsset;
        if (defaultFont != null)
            tmp.font = defaultFont;
        #endif

        // Save prefab asset (overwrite if exists)
        GameObject prefabAsset = null;
        if (File.Exists(SongButtonPrefabPath))
        {
            prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SongButtonPrefabPath);
            // replace existing asset
            PrefabUtility.SaveAsPrefabAssetAndConnect(prefabGO, SongButtonPrefabPath, InteractionMode.AutomatedAction);
            prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SongButtonPrefabPath);
        }
        else
        {
            prefabAsset = PrefabUtility.SaveAsPrefabAsset(prefabGO, SongButtonPrefabPath);
        }

        // Destroy transient prefabGO from scene (we only wanted the asset)
        Object.DestroyImmediate(prefabGO);

        // Wire Mp3PlayerUI fields
        mp3UI.panel = panelGO;
        mp3UI.songListParent = contentRt;
        if (prefabAsset != null)
            mp3UI.songButtonPrefab = prefabAsset.GetComponent<Button>();
        mp3UI.currentSongText = tmp;
        mp3UI.closeButton = closeBtn;
        mp3UI.prevButton = prevBtn;
        mp3UI.nextButton = nextBtn;
        mp3UI.loopToggle = loopToggle;
        mp3UI.shuffleToggle = shuffleToggle;

        // Default the panel to inactive so it doesn't show immediately
        panelGO.SetActive(false);

        // Clean up selection and mark scene dirty
        Selection.activeGameObject = panelGO;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("MP3 Player UI created. Song button prefab saved to: " + SongButtonPrefabPath);
    }

    private static Button CreateButton(string name, Transform parent, Vector2 anchorPos, Vector2 sizePercent, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorPos.x - sizePercent.x / 2f, anchorPos.y - sizePercent.y / 2f);
        rt.anchorMax = new Vector2(anchorPos.x + sizePercent.x / 2f, anchorPos.y + sizePercent.y / 2f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = new Color(0.18f, 0.18f, 0.2f, 1f);
        var btn = go.GetComponent<Button>();

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TMP_Text>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 18;
        var tr = textGO.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;

        // Set TMP font if available
        var defaultFont = TMPro.TMP_Settings.defaultFontAsset;
        if (defaultFont != null)
            tmp.font = defaultFont;

        return btn;
    }

    private static Toggle CreateToggle(string name, Transform parent, Vector2 anchorPos, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorPos.x - 0.45f, anchorPos.y - 0.2f);
        rt.anchorMax = new Vector2(anchorPos.x + 0.45f, anchorPos.y + 0.2f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Toggle root
        var toggleGO = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
        toggleGO.transform.SetParent(go.transform, false);
        var togRt = toggleGO.GetComponent<RectTransform>();
        togRt.anchorMin = new Vector2(0f, 0.2f);
        togRt.anchorMax = new Vector2(0.12f, 0.8f);
        togRt.offsetMin = togRt.offsetMax = Vector2.zero;

        // Background and checkmark visuals
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(toggleGO.transform, false);
        var bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0f);
        bgRt.anchorMax = new Vector2(1f, 1f);
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        check.transform.SetParent(bg.transform, false);
        var checkImg = check.GetComponent<Image>();
        checkImg.color = new Color(0.2f, 0.6f, 1f, 1f);
        var checkRt = check.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0.15f, 0.15f);
        checkRt.anchorMax = new Vector2(0.85f, 0.85f);
        checkRt.offsetMin = checkRt.offsetMax = Vector2.zero;

        // Label
        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        var labelText = labelGO.AddComponent<TMP_Text>();
        labelText.text = label;
        labelText.fontSize = 16;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        var lblRt = labelGO.GetComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0.14f, 0f);
        lblRt.anchorMax = new Vector2(1f, 1f);
        lblRt.offsetMin = new Vector2(6f, 0f);
        lblRt.offsetMax = Vector2.zero;

        var toggle = toggleGO.GetComponent<Toggle>();
        toggle.graphic = checkImg;
        toggle.targetGraphic = bgImg;

        // Wrap into a Toggle component object to return the Toggle (we created toggle on toggleGO)
        return toggle;
    }
}