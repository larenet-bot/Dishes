using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class AchievementViewer : MonoBehaviour
{
    [Tooltip("If assigned, this Canvas will be used as the parent for the panel. The Editor menu will parent the viewer under a Canvas automatically.")]
    public Canvas targetCanvas;

    // Optional: manually assign the menu root in the inspector. If left null the Editor menu creates one for you.
    [Tooltip("Reference to the Menu root GameObject (child). Created by the Editor menu and editable in the Hierarchy.")]
    public GameObject menuRoot;

    // Content container inside the menuRoot where achievement rows are spawned.
    private RectTransform contentRoot;

    public static AchievementViewer Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        // In editor, don't auto-create menu here; the Editor menu creates the editable panel.
        // But keep a reference if already present as child.
        if (!Application.isPlaying)
        {
            TryResolveMenuReferences();
        }
        else
        {
            TryResolveMenuReferences();
        }
#else
        TryResolveMenuReferences();
#endif
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    // Try to locate the menuRoot and its content container if they exist under this GameObject.
    private void TryResolveMenuReferences()
    {
        if (menuRoot == null)
        {
            // try find child named "AchievementViewer_Panel" or first child with Image component
            var child = transform.Find("AchievementViewer_Panel");
            if (child != null)
                menuRoot = child.gameObject;
            else
            {
                // fallback: try first child that looks like a panel
                foreach (Transform t in transform)
                {
                    if (t.GetComponent<Image>() != null)
                    {
                        menuRoot = t.gameObject;
                        break;
                    }
                }
            }
        }

        contentRoot = null;
        if (menuRoot != null)
        {
            // common path: ScrollRect -> Viewport -> Content
            var scroll = menuRoot.transform.Find("ScrollRect");
            if (scroll != null)
            {
                var viewport = scroll.Find("Viewport");
                if (viewport != null)
                {
                    var content = viewport.Find("Content");
                    if (content != null)
                        contentRoot = content.GetComponent<RectTransform>();
                }
            }

            // fallback: try find a child named "Content" anywhere under menuRoot
            if (contentRoot == null)
            {
                var c = menuRoot.transform.Find("Content");
                if (c != null)
                    contentRoot = c.GetComponent<RectTransform>();
            }
        }
    }

    // Called by DuckClick via static toggle
    public void ToggleMenu()
    {
        if (menuRoot == null)
        {
            Debug.LogWarning("[AchievementViewer] menuRoot not assigned or found. Create one via GameObject -> Dishes -> Create Achievement Viewer.");
            return;
        }

        bool newState = !menuRoot.activeSelf;

        // If we're showing the panel, ensure it renders above other UI (fixes layering issues).
        if (newState)
        {
            // Add or configure a Canvas on the panel so it can override sorting and appear on top.
            var panelCanvas = menuRoot.GetComponent<Canvas>();
            if (panelCanvas == null)
            {
                panelCanvas = menuRoot.AddComponent<Canvas>();
                // Ensure the panel can receive UI events if needed.
                var ray = menuRoot.GetComponent<GraphicRaycaster>();
                if (ray == null) menuRoot.AddComponent<GraphicRaycaster>();
            }

            panelCanvas.overrideSorting = true;
            // Use a high sorting order so the panel appears above other canvases.
            panelCanvas.sortingOrder = 1000;

            // Also move the panel to the end of its parent's children so it's on top in the same canvas.
            menuRoot.transform.SetAsLastSibling();

            menuRoot.SetActive(true);
            RefreshContents();
        }
        else
        {
            menuRoot.SetActive(false);
        }
    }

    // Static entry used by DuckClick
    public static void ToggleMenuStatic()
    {
        if (Instance != null)
        {
            Instance.ToggleMenu();
            return;
        }

        Debug.LogWarning("[AchievementViewer] No AchievementViewer instance found in scene. Create one from GameObject -> Dishes -> Create Achievement Viewer.");
    }

    // Populate the contentRoot with rows for every AchievementData known to AchievementManager.
    public void RefreshContents()
    {
        if (menuRoot == null)
        {
            Debug.LogWarning("[AchievementViewer] RefreshContents: menuRoot is null.");
            return;
        }

        if (contentRoot == null)
        {
            TryResolveMenuReferences();
            if (contentRoot == null)
            {
                Debug.LogWarning("[AchievementViewer] RefreshContents: content container not found under menuRoot.");
                return;
            }
        }

        // Clear existing
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            DestroyImmediate(contentRoot.GetChild(i).gameObject);

        var mgr = AchievementManager.Instance;
        if (mgr == null)
        {
            AddInfoRow("Error", "AchievementManager not present in scene.");
            return;
        }

        var unlockedIds = mgr.GetUnlockedAchievementIds() ?? new List<string>();
        var definitions = mgr.allAchievements ?? new List<AchievementData>();

        var definedList = new List<AchievementData>(definitions);
        definedList.RemoveAll(d => d == null);

        // sort unlocked first
        definedList.Sort((a, b) =>
        {
            bool aUnlocked = unlockedIds.Contains(a.id);
            bool bUnlocked = unlockedIds.Contains(b.id);
            if (aUnlocked != bUnlocked) return aUnlocked ? -1 : 1;
            return string.Compare(a.id, b.id, StringComparison.Ordinal);
        });

        foreach (var a in definedList)
        {
            if (a == null) continue;

            bool unlocked = unlockedIds.Contains(a.id) || mgr.IsUnlocked(a.id);

            if (a.hidden && !unlocked)
            {
                AddAchievementRow("???", "Hidden achievement", unlocked: false, dimmed: true);
            }
            else
            {
                string title = string.IsNullOrWhiteSpace(a.title) ? a.id : a.title;
                string desc = string.IsNullOrWhiteSpace(a.description) ? "(no description)" : a.description;
                AddAchievementRow(title, desc, unlocked, dimmed: !unlocked);
            }
        }

        // orphan unlocked
        var definedIds = new HashSet<string>();
        foreach (var d in definedList) if (d != null && !string.IsNullOrWhiteSpace(d.id)) definedIds.Add(d.id);
        var orphanUnlocked = new List<string>();
        foreach (var id in unlockedIds) if (!definedIds.Contains(id)) orphanUnlocked.Add(id);

        if (orphanUnlocked.Count > 0)
        {
            AddInfoRow("Other unlocked (no definition)", $"Found {orphanUnlocked.Count} unlocked id(s) with no AchievementData in the manager.");
            foreach (var id in orphanUnlocked)
                AddAchievementRow(id, "(missing definition)", true, false);
        }
    }

    // Helpers to create rows using legacy UI.Text so the panel is editable without TMP.
    private void AddInfoRow(string title, string description)
    {
        var row = new GameObject("InfoRow", typeof(RectTransform));
        row.transform.SetParent(contentRoot, false);
        var rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 50);

        var tGO = new GameObject("Title");
        tGO.transform.SetParent(row.transform, false);
        var tText = tGO.AddComponent<Text>();
        tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tText.text = title;
        tText.fontSize = 16;
        tText.color = new Color(0.9f, 0.9f, 0.9f);
        tText.alignment = TextAnchor.UpperLeft;
        var tRt = tGO.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 0.5f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.offsetMin = new Vector2(6f, -24f);
        tRt.offsetMax = new Vector2(-6f, 0f);

        var dGO = new GameObject("Description");
        dGO.transform.SetParent(row.transform, false);
        var dText = dGO.AddComponent<Text>();
        dText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dText.text = description;
        dText.fontSize = 13;
        dText.color = new Color(0.8f, 0.8f, 0.8f);
        dText.alignment = TextAnchor.LowerLeft;
        var dRt = dGO.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(1f, 0.5f);
        dRt.offsetMin = new Vector2(6f, 0f);
        dRt.offsetMax = new Vector2(-6f, 24f);
    }

    private void AddAchievementRow(string title, string description, bool unlocked, bool dimmed)
    {
        var row = new GameObject("AchievementRow", typeof(RectTransform));
        row.transform.SetParent(contentRoot, false);
        var rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 70);

        var statusGO = new GameObject("Status");
        statusGO.transform.SetParent(row.transform, false);
        var statusRt = statusGO.AddComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0f, 0f);
        statusRt.anchorMax = new Vector2(0.08f, 1f);
        statusRt.offsetMin = new Vector2(6f, 6f);
        statusRt.offsetMax = new Vector2(0f, -6f);
        var img = statusGO.AddComponent<Image>();
        img.color = unlocked ? new Color(0.2f, 0.7f, 0.2f) : new Color(0.3f, 0.3f, 0.3f);

        var tGO = new GameObject("Title");
        tGO.transform.SetParent(row.transform, false);
        var tText = tGO.AddComponent<Text>();
        tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tText.text = title;
        tText.fontSize = 18;
        tText.color = dimmed ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
        tText.alignment = TextAnchor.UpperLeft;
        var tRt = tGO.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.09f, 0.5f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.offsetMin = new Vector2(6f, -28f);
        tRt.offsetMax = new Vector2(-6f, 0f);

        var dGO = new GameObject("Description");
        dGO.transform.SetParent(row.transform, false);
        var dText = dGO.AddComponent<Text>();
        dText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dText.text = description;
        dText.fontSize = 14;
        dText.color = dimmed ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.9f, 0.9f, 0.9f);
        dText.alignment = TextAnchor.LowerLeft;
        var dRt = dGO.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0.09f, 0f);
        dRt.anchorMax = new Vector2(1f, 0.5f);
        dRt.offsetMin = new Vector2(6f, 0f);
        dRt.offsetMax = new Vector2(-6f, 24f);
    }
}
