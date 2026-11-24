using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared modal interaction suppression for every menu registered with MenuManager,
/// including the Pause menu by default. Attach to the ALWAYS-ACTIVE MenuManager
/// GameObject, not to a menu that is hidden when closed.
///
/// Unlike disabling DuckClick/BubbleClickDestroy MonoBehaviours, disabling their
/// Collider2D components prevents Unity's physics-based OnMouseDown from firing.
/// World spawners, movement, animation, and income continue to run.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MenuManager))]
public class MenuManagerInteractionGuard : MonoBehaviour
{
    [Header("Menu State")]
    [SerializeField] private MenuManager menuManager;
    [SerializeField, Tooltip("Also apply while the pause menu is open. PauseMenu already has its own collider blocking, but this keeps tooltips consistent.")]
    private bool includePauseMenu = true;

    [Header("Block World Clicks")]
    [SerializeField, Tooltip("2D physics layers whose colliders cannot be clicked through any managed menu. Default is layer 6 (bubbles). Add your duck layer as well if you use one.")]
    private LayerMask colliderLayersToBlock = 1 << 6;

    [SerializeField, Tooltip("Optional individual scene objects, such as Duck, whose Collider2Ds should be disabled while menus are open. Their children are included.")]
    private GameObject[] sceneObjectsToBlock = new GameObject[0];

    [SerializeField, Min(0.02f), Tooltip("How often to catch newly spawned bubbles and other objects. Uses unscaled time, so it also works in menus that pause gameplay.")]
    private float colliderRefreshSeconds = 0.05f;

    [Header("Tooltips")]
    [SerializeField, Tooltip("Hide all TooltipManager tooltips whenever any registered menu is open, including manually triggered employee tooltips. Restores the user's tooltips preference when closed.")]
    private bool hideTooltipsWhileMenuOpen = true;

    private readonly HashSet<Collider2D> collidersWeDisabled = new HashSet<Collider2D>();
    private readonly Dictionary<TooltipManager, bool> tooltipStates = new Dictionary<TooltipManager, bool>();
    private bool wasBlocking;
    private float refreshElapsed;
    private int tooltipPreferenceAtOpen;

    private void Reset()
    {
        menuManager = GetComponent<MenuManager>();
    }

    private void Awake()
    {
        if (menuManager == null)
            menuManager = GetComponent<MenuManager>();
    }

    private void Update()
    {
        if (menuManager == null)
            menuManager = GetComponent<MenuManager>();

        bool shouldBlock = menuManager != null &&
            (includePauseMenu ? menuManager.HasAnyMenuOpen() : menuManager.HasOpenManagedMenu());

        if (!shouldBlock)
        {
            if (wasBlocking)
                RestoreAll();
            return;
        }

        if (!wasBlocking)
        {
            wasBlocking = true;
            refreshElapsed = 0f;
            tooltipPreferenceAtOpen = PlayerPrefs.GetInt("TooltipsEnabled", 1);
            ScanAndDisableWorldColliders();
            SuppressTooltips();
            return;
        }

        // SettingsMenu can toggle tooltips while open. Enforce menu suppression
        // without losing the user's newly chosen preference.
        if (hideTooltipsWhileMenuOpen)
        {
            foreach (KeyValuePair<TooltipManager, bool> entry in tooltipStates)
            {
                if (entry.Key != null && entry.Key.enabled)
                    entry.Key.enabled = false;
            }
        }

        refreshElapsed += Time.unscaledDeltaTime;
        if (refreshElapsed >= Mathf.Max(0.02f, colliderRefreshSeconds))
        {
            refreshElapsed = 0f;
            ScanAndDisableWorldColliders();
            SuppressTooltips(); // Also finds any TooltipManagers created after menu opening.
        }
    }

    private void LateUpdate()
    {
        if (!wasBlocking || !hideTooltipsWhileMenuOpen)
            return;

        // Some UI events invoke ShowManualTooltip directly even on a disabled
        // MonoBehaviour. Clear those requests after UI input processing too.
        foreach (KeyValuePair<TooltipManager, bool> entry in tooltipStates)
        {
            if (entry.Key != null)
                entry.Key.HideManualTooltip();
        }
    }

    private void ScanAndDisableWorldColliders()
    {
        // Match the existing PauseMenu/SettingsMenu logic, including checking
        // parent layers, so spawned bubbles inherit their configured layer.
        if (colliderLayersToBlock.value != 0)
        {
            Collider2D[] all = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                Collider2D col = all[i];
                if (col != null && IsColliderOnBlockedLayer(col))
                    DisableCollider(col);
            }
        }

        if (sceneObjectsToBlock == null)
            return;

        for (int i = 0; i < sceneObjectsToBlock.Length; i++)
        {
            GameObject obj = sceneObjectsToBlock[i];
            if (obj == null)
                continue;

            Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>(true);
            for (int j = 0; j < colliders.Length; j++)
                DisableCollider(colliders[j]);
        }
    }

    private bool IsColliderOnBlockedLayer(Collider2D collider)
    {
        Transform t = collider.transform;
        while (t != null)
        {
            if ((colliderLayersToBlock.value & (1 << t.gameObject.layer)) != 0)
                return true;
            t = t.parent;
        }
        return false;
    }

    private void DisableCollider(Collider2D collider)
    {
        if (collider == null || !collider.enabled)
            return;

        // Only remember colliders this component actually changed, so pre-disabled
        // colliders stay disabled when the menu is closed.
        collidersWeDisabled.Add(collider);
        collider.enabled = false;
    }

    private void SuppressTooltips()
    {
        if (!hideTooltipsWhileMenuOpen)
            return;

        // Include disabled managers so SettingsMenu's tooltip toggle and multiple
        // gameplay canvases work without assigning every TooltipManager manually.
        TooltipManager[] all = Resources.FindObjectsOfTypeAll<TooltipManager>();
        for (int i = 0; i < all.Length; i++)
        {
            TooltipManager tooltip = all[i];
            if (tooltip == null || !tooltip.gameObject.scene.IsValid())
                continue;

            if (!tooltipStates.ContainsKey(tooltip))
                tooltipStates.Add(tooltip, tooltip.enabled);

            if (tooltip.enabled)
                tooltip.enabled = false;
        }
    }

    private void RestoreAll()
    {
        foreach (Collider2D collider in collidersWeDisabled)
        {
            if (collider != null)
                collider.enabled = true;
        }
        collidersWeDisabled.Clear();

        int currentPreference = PlayerPrefs.GetInt("TooltipsEnabled", 1);
        bool preferenceChangedWhileOpen = currentPreference != tooltipPreferenceAtOpen;

        foreach (KeyValuePair<TooltipManager, bool> entry in tooltipStates)
        {
            if (entry.Key == null)
                continue;

            // Preserve the original component state unless the settings menu
            // changed the user's tooltip preference while this menu was open.
            entry.Key.enabled = preferenceChangedWhileOpen
                ? currentPreference != 0
                : entry.Value;
        }
        tooltipStates.Clear();

        wasBlocking = false;
        refreshElapsed = 0f;
    }

    private void OnDisable()
    {
        if (wasBlocking)
            RestoreAll();
    }

    private void OnDestroy()
    {
        if (wasBlocking)
            RestoreAll();
    }
}
