using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIStartupWarmup : MonoBehaviour
{
    [Header("The canvas root that Help toggles (the one that fixes everything)")]
    [SerializeField] private GameObject mainGameCanvasRoot;

    [Header("Camera used by ScreenSpace-Camera and WorldSpace canvases")]
    [SerializeField] private Camera uiCamera;

    [Header("Optional: canvases that must have worldCamera set")]
    [SerializeField] private Canvas[] canvasesNeedingCamera;

    [Header("Warmup")]
    [SerializeField] private bool toggleRootForOneFrame = true;

    private IEnumerator Start()
    {
        if (uiCamera == null) uiCamera = Camera.main;

        // Let all Awake/Start run first.
        yield return null;

        // Force camera assignment (fixes "clicks don’t register" when worldCamera is null/wrong).
        if (uiCamera != null && canvasesNeedingCamera != null)
        {
            for (int i = 0; i < canvasesNeedingCamera.Length; i++)
            {
                var c = canvasesNeedingCamera[i];
                if (c == null) continue;

                if (c.renderMode != RenderMode.ScreenSpaceOverlay)
                    c.worldCamera = uiCamera;

                var gr = c.GetComponent<GraphicRaycaster>();
                if (gr != null) gr.enabled = true;
            }
        }

        Canvas.ForceUpdateCanvases();

        // Mimic your Help button: toggle once to force Unity to rebuild raycasters/layout.
        if (toggleRootForOneFrame && mainGameCanvasRoot != null)
        {
            mainGameCanvasRoot.SetActive(false);
            yield return null;
            mainGameCanvasRoot.SetActive(true);
            yield return null;
        }

        Canvas.ForceUpdateCanvases();
    }
}