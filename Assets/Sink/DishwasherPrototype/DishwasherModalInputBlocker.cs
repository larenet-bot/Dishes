using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional modal interaction shield. Put this on an ALWAYS-ACTIVE object (the
/// DishwasherPrototypeController), not on the menu that gets disabled.
/// A fullscreen UI Image must still have Raycast Target ON to block other UI.
/// This helper suppresses the shared tooltip manager and world-space OnMouseDown
/// interactions from ducks and bubbles while the dishwasher menu is visible.
/// It never disables the bubble's movement/spawning components.
/// </summary>
public class DishwasherModalInputBlocker : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private TooltipManager tooltipManager;
    [SerializeField] private bool blockTooltips = true;
    [SerializeField] private bool blockDuckClicks = true;
    [SerializeField] private bool blockBubbleClicks = true;
    [SerializeField, Min(0.05f)] private float scanInterval = 0.2f;

    private readonly Dictionary<Behaviour, bool> suppressed = new Dictionary<Behaviour, bool>();
    private bool wasOpen;
    private bool previousTooltipEnabled;
    private bool tooltipWasCaptured;
    private float nextScan;

    private void Update()
    {
        bool isOpen = menuRoot != null && menuRoot.activeInHierarchy;
        if (!isOpen)
        {
            if (wasOpen) Restore();
            wasOpen = false;
            return;
        }

        if (!wasOpen)
        {
            wasOpen = true;
            if (blockTooltips)
            {
                if (tooltipManager == null) tooltipManager = FindAnyObjectByType<TooltipManager>();
                if (tooltipManager != null)
                {
                    previousTooltipEnabled = tooltipManager.enabled;
                    tooltipWasCaptured = true;
                    tooltipManager.enabled = false;
                }
            }
            SuppressWorldClickHandlers();
            nextScan = Time.unscaledTime + scanInterval;
        }
        else if (Time.unscaledTime >= nextScan)
        {
            // New bubbles may spawn at any point while the menu is open.
            SuppressWorldClickHandlers();
            nextScan = Time.unscaledTime + scanInterval;
        }
    }

    private void SuppressWorldClickHandlers()
    {
        if (blockDuckClicks)
        {
            foreach (DuckClick duck in FindObjectsByType<DuckClick>(FindObjectsSortMode.None))
                Suppress(duck);
        }
        if (blockBubbleClicks)
        {
            foreach (BubbleClickDestroy bubble in FindObjectsByType<BubbleClickDestroy>(FindObjectsSortMode.None))
                Suppress(bubble);
        }
    }

    private void Suppress(Behaviour component)
    {
        if (component == null || suppressed.ContainsKey(component)) return;
        // Leave components that another system intentionally disabled alone.
        if (!component.enabled) return;
        suppressed[component] = true;
        component.enabled = false;
    }

    private void Restore()
    {
        foreach (KeyValuePair<Behaviour, bool> entry in suppressed)
        {
            if (entry.Key != null) entry.Key.enabled = entry.Value;
        }
        suppressed.Clear();
        if (tooltipWasCaptured && tooltipManager != null)
            tooltipManager.enabled = previousTooltipEnabled;
        tooltipWasCaptured = false;
    }

    private void OnDisable()
    {
        Restore();
        wasOpen = false;
    }

    private void OnDestroy() => Restore();
}
