using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

#if UNITY_EDITOR
public static class CreateAchievementPopupPrefab
{
    [MenuItem("Tools/Create Achievement Popup Prefab (Top-Center, Small)")]
    public static void CreatePrefab()
    {
        // Ensure output folder exists
        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // Root Canvas (so prefab can be previewed/instantiated without requiring a parent canvas)
        var canvasGO = new GameObject("AchievementPopup_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // keep above gameplay
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Popup root (small rectangle anchored top-center)
        var popupRoot = new GameObject("PopupRoot");
        popupRoot.transform.SetParent(canvasGO.transform, false);
        var popupRT = popupRoot.AddComponent<RectTransform>();
        // Anchor top-center
        popupRT.anchorMin = new Vector2(0.5f, 1f);
        popupRT.anchorMax = new Vector2(0.5f, 1f);
        popupRT.pivot = new Vector2(0.5f, 1f);
        popupRT.sizeDelta = new Vector2(320f, 88f); // smaller
        popupRT.anchoredPosition = new Vector2(0f, -72f); // 72 px below top edge

        // CanvasGroup (fade control)
        var popupCg = popupRoot.AddComponent<CanvasGroup>();

        // Background panel (rounded look can be applied by assigning a sprite in inspector)
        var panelGO = new GameObject("Background");
        panelGO.transform.SetParent(popupRoot.transform, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 0f);
        panelRT.anchorMax = new Vector2(1f, 1f);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        var bg = panelGO.AddComponent<Image>();
        bg.color = new Color32(18, 18, 18, 220);

        // Optional decorative border (thin)
        var borderGO = new GameObject("Border");
        borderGO.transform.SetParent(panelGO.transform, false);
        var borderRT = borderGO.AddComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0f, 0f);
        borderRT.anchorMax = new Vector2(1f, 1f);
        borderRT.offsetMin = new Vector2(1f, 1f);
        borderRT.offsetMax = new Vector2(-1f, -1f);
        var borderImg = borderGO.AddComponent<Image>();
        borderImg.color = new Color32(255, 215, 120, 25); // subtle glow; assign sprite if desired
        borderImg.raycastTarget = false;

        // Icon (small)
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(panelGO.transform, false);
        var iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0f, 0.5f);
        iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.sizeDelta = new Vector2(48f, 48f);
        iconRT.anchoredPosition = new Vector2(28f, 0f);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color = new Color32(255, 255, 255, 255); // placeholder

        // Title (smaller)
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(10f, -12f);
        titleRT.sizeDelta = new Vector2(-96f, 28f);
        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.raycastTarget = false;
        title.fontSize = 18;
        title.alignment = TextAlignmentOptions.Left;
        title.enableAutoSizing = true;
        title.color = Color.white;
        title.text = "Achievement Unlocked";

        // Description (single-line or short)
        var descGO = new GameObject("Description");
        descGO.transform.SetParent(panelGO.transform, false);
        var descRT = descGO.AddComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0f, 0f);
        descRT.anchorMax = new Vector2(1f, 0f);
        descRT.pivot = new Vector2(0.5f, 0f);
        descRT.anchoredPosition = new Vector2(10f, 12f);
        descRT.sizeDelta = new Vector2(-96f, 28f);
        var desc = descGO.AddComponent<TextMeshProUGUI>();
        desc.raycastTarget = false;
        desc.fontSize = 14;
        desc.alignment = TextAlignmentOptions.Left;
        desc.enableAutoSizing = true;
        desc.color = new Color32(230, 230, 230, 255);
        desc.text = "Short description of the achievement.";

        // Try to assign a TMP font if available
        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        if (fonts != null && fonts.Length > 0)
        {
            title.font = fonts[0];
            desc.font = fonts[0];
        }

        // Add the AchievementPopup component and wire references
        var popup = popupRoot.AddComponent<AchievementPopup>();
        popup.canvasGroup = popupCg;
        popup.titleText = title;
        popup.descriptionText = desc;
        popup.showDuration = 2.5f;
        popup.fadeTime = 0.25f;

        // Save as prefab
        string prefabPath = Path.Combine(folder, "AchievementPopup.prefab");
        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
        PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath, out bool success);

        if (success)
        {
            Debug.Log($"Created small top-center AchievementPopup prefab at: {prefabPath}");
        }
        else
        {
            Debug.LogError("Failed to create AchievementPopup prefab.");
        }

        // Clean up temp scene objects
        Object.DestroyImmediate(canvasGO);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif