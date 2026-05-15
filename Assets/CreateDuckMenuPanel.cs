//csharp Assets/Editor/CreateDuckMenuPanel.cs
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CreateDuckMenuPanel
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string DishItemPrefabPath = PrefabFolder + "/DishItem.prefab";

    [MenuItem("Tools/Create Duck Menu Panel")]
    public static void CreatePanel()
    {
        // Ensure Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler cs = canvasGO.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);

            // Ensure EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }

        // Create Prefab folder
        if (!Directory.Exists(PrefabFolder))
            Directory.CreateDirectory(PrefabFolder);

        // Create DishItem prefab (if missing)
        if (!File.Exists(DishItemPrefabPath))
        {
            GameObject dishItem = new GameObject("DishItem");
            var hLayout = dishItem.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.spacing = 8f;
            hLayout.childForceExpandHeight = false;
            hLayout.childForceExpandWidth = false;

            // Icon
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(dishItem.transform, false);
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.color = Color.white;
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.sizeDelta = new Vector2(48, 48);

            // Name (TMP)
            GameObject nameGO = new GameObject("Name", typeof(RectTransform));
            nameGO.transform.SetParent(dishItem.transform, false);
            var tmp = nameGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Dish Name";
            tmp.fontSize = 24;
            tmp.color = Color.white;

            // LockedOverlay child (optional)
            GameObject locked = new GameObject("LockedOverlay", typeof(RectTransform), typeof(Image));
            locked.transform.SetParent(dishItem.transform, false);
            var lockImg = locked.GetComponent<Image>();
            lockImg.color = new Color(0f, 0f, 0f, 0.6f);
            locked.SetActive(false);

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(dishItem, DishItemPrefabPath);
            Object.DestroyImmediate(dishItem);
            Debug.Log("[CreateDuckMenuPanel] Created prefab: " + DishItemPrefabPath);
        }

        // Create Duck Menu Panel under Canvas
        GameObject panelRoot = new GameObject("DuckMenuPanel", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(canvas.transform, false);
        var panelImage = panelRoot.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.7f);

        var panelRT = panelRoot.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(900, 600);
        panelRoot.SetActive(false);
        Undo.RegisterCreatedObjectUndo(panelRoot, "Create DuckMenuPanel");

        // Title bar
        GameObject titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(panelRoot.transform, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(0, 60);
        titleRT.anchoredPosition = new Vector2(0, 0);
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Duck Menu";
        titleText.fontSize = 36;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        // Content container (horizontal two columns)
        GameObject contentRoot = new GameObject("ContentRoot", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        contentRoot.transform.SetParent(panelRoot.transform, false);
        var contentRT = contentRoot.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 0f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 0.5f);
        contentRT.anchoredPosition = new Vector2(0, -30);
        contentRT.offsetMin = new Vector2(10, 10);
        contentRT.offsetMax = new Vector2(-10, -70);

        var hgroup = contentRoot.GetComponent<HorizontalLayoutGroup>();
        hgroup.spacing = 10;
        hgroup.childForceExpandHeight = true;
        hgroup.childForceExpandWidth = true;
        Undo.RegisterCreatedObjectUndo(contentRoot, "Create ContentRoot");

        // Left column: Dishes (ScrollView)
        GameObject dishesPanel = CreatePanelColumn("DishesPanel", contentRoot.transform, "Unlocked Dishes");
        // Right column: Achievements
        GameObject achievementsPanel = CreatePanelColumn("AchievementsPanel", contentRoot.transform, "Achievements");

        // Create a content container inside dishesPanel to be used as dishesContent
        GameObject dishesScrollView = CreateSimpleScrollView("DishesScrollView", dishesPanel.transform);
        RectTransform dishesContent = dishesScrollView.transform.Find("Viewport/Content").GetComponent<RectTransform>();

        // Create achievements content (vertical layout)
        GameObject achievementsContent = new GameObject("AchievementsContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        achievementsContent.transform.SetParent(achievementsPanel.transform, false);
        var aRT = achievementsContent.GetComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0f, 0f);
        aRT.anchorMax = new Vector2(1f, 1f);
        aRT.offsetMin = new Vector2(10, 10);
        aRT.offsetMax = new Vector2(-10, -10);
        var aV = achievementsContent.GetComponent<VerticalLayoutGroup>();
        aV.spacing = 8;
        aV.childForceExpandHeight = false;
        aV.childForceExpandWidth = true;
        var asf = achievementsContent.GetComponent<ContentSizeFitter>();
        asf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Create an empty placeholder to show instructions
        GameObject placeholder = new GameObject("PlaceholderText", typeof(RectTransform));
        placeholder.transform.SetParent(achievementsContent.transform, false);
        var ph = placeholder.AddComponent<TextMeshProUGUI>();
        ph.text = "Place achievement prefabs in DuckClick. They will be instantiated here.";
        ph.fontSize = 18;
        ph.color = new Color(1f, 1f, 1f, 0.9f);

        // Save scene objects via Undo already done; now create prefab asset reference
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Try to assign to DuckClick components in scene
        var duckClicks = Object.FindObjectsOfType<DuckClick>();
        var dishItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DishItemPrefabPath);

        foreach (var dc in duckClicks)
        {
            Undo.RecordObject(dc, "Wire DuckClick references");

            SerializedObject so = new SerializedObject(dc);
            var duckMenuProp = so.FindProperty("duckMenuPanel");
            if (duckMenuProp != null)
                duckMenuProp.objectReferenceValue = panelRoot;

            var dishesContentProp = so.FindProperty("dishesContent");
            if (dishesContentProp != null)
                dishesContentProp.objectReferenceValue = dishesContent;

            var achievementsContentProp = so.FindProperty("achievementsContent");
            if (achievementsContentProp != null)
                achievementsContentProp.objectReferenceValue = achievementsContent;

            var dishPrefabProp = so.FindProperty("dishItemPrefab");
            if (dishPrefabProp != null)
                dishPrefabProp.objectReferenceValue = dishItemPrefab;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(dc);
        }

        // Select created panel
        Selection.activeGameObject = panelRoot;
        Debug.Log("[CreateDuckMenuPanel] Duck menu created and DuckClick components wired (if present).");
    }

    private static GameObject CreatePanelColumn(string name, Transform parent, string headerText)
    {
        GameObject column = new GameObject(name, typeof(RectTransform), typeof(Image));
        column.transform.SetParent(parent, false);
        var img = column.GetComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

        var rt = column.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 0);

        // Header
        GameObject header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(column.transform, false);
        var hRT = header.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0f, 1f);
        hRT.anchorMax = new Vector2(1f, 1f);
        hRT.pivot = new Vector2(0.5f, 1f);
        hRT.sizeDelta = new Vector2(0, 40);
        var headerTextComp = header.AddComponent<TextMeshProUGUI>();
        headerTextComp.text = headerText;
        headerTextComp.fontSize = 22;
        headerTextComp.alignment = TextAlignmentOptions.Center;
        headerTextComp.color = Color.white;

        // Body will be filled by caller
        return column;
    }

    private static GameObject CreateSimpleScrollView(string name, Transform parent)
    {
        // ScrollView root
        GameObject sv = new GameObject(name, typeof(RectTransform));
        sv.transform.SetParent(parent, false);
        var rt = sv.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(10, 10);
        rt.offsetMax = new Vector2(-10, -10);

        // Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(sv.transform, false);
        var vImg = viewport.GetComponent<Image>();
        vImg.color = new Color(0f, 0f, 0f, 0f);
        var vRT = viewport.GetComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0f, 0f);
        vRT.anchorMax = new Vector2(1f, 1f);
        vRT.offsetMin = new Vector2(0, 0);
        vRT.offsetMax = new Vector2(0, 0);

        // Content
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var cRT = content.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0f, 1f);
        cRT.anchorMax = new Vector2(1f, 1f);
        cRT.pivot = new Vector2(0.5f, 1f);
        cRT.sizeDelta = new Vector2(0, 0);

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 6f;

        var csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Add ScrollRect component on root and wire
        var scroll = sv.AddComponent<ScrollRect>();
        scroll.content = cRT;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.horizontal = false;
        scroll.vertical = true;

        // Add a mask image background
        var bg = sv.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.0f);

        return sv;
    }
}