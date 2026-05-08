using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRaycastDebugger : MonoBehaviour
{
    [SerializeField] private KeyCode debugKey = KeyCode.F9;

    private void Update()
    {
        if (!Input.GetKeyDown(debugKey))
            return;

        if (EventSystem.current == null)
        {
            Debug.LogWarning("[UIRaycastDebugger] No EventSystem found.");
            return;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count == 0)
        {
            Debug.Log("[UIRaycastDebugger] Nothing hit by UI raycast.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[UIRaycastDebugger] UI objects under mouse, top to bottom:");

        for (int i = 0; i < results.Count; i++)
        {
            GameObject hit = results[i].gameObject;
            Graphic graphic = hit.GetComponent<Graphic>();
            Selectable selectable = hit.GetComponent<Selectable>();
            Canvas canvas = hit.GetComponentInParent<Canvas>();

            sb.Append(i);
            sb.Append(". ");
            sb.Append(GetFullPath(hit.transform));

            if (canvas != null)
            {
                sb.Append(" | Canvas: ");
                sb.Append(canvas.name);
                sb.Append(" | RenderMode: ");
                sb.Append(canvas.renderMode);
                sb.Append(" | SortingOrder: ");
                sb.Append(canvas.sortingOrder);
            }

            if (graphic != null)
            {
                sb.Append(" | Graphic: ");
                sb.Append(graphic.GetType().Name);
                sb.Append(" | RaycastTarget: ");
                sb.Append(graphic.raycastTarget);
            }

            if (selectable != null)
            {
                sb.Append(" | Selectable: ");
                sb.Append(selectable.GetType().Name);
                sb.Append(" | Interactable: ");
                sb.Append(selectable.interactable);
            }

            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
    }

    private string GetFullPath(Transform t)
    {
        if (t == null)
            return "";

        string path = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }
}