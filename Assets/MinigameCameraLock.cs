using UnityEngine;

public class MinigameCameraLock : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("What to center on")]
    [Tooltip("Drag the SpriteRenderer or any Renderer you want the camera to center on")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Disable pan scripts")]
    [Tooltip("Drag your camera pan script(s) here, for example CameraPanClamp2D")]
    [SerializeField] private MonoBehaviour[] panScriptsToDisable;

    [Header("Optional")]
    [Tooltip("If true, keep the current Y position. If false, use forcedY")]
    [SerializeField] private bool keepCurrentY = true;

    [SerializeField] private float forcedY = 0f;

    private Vector3 savedCameraPos;
    private bool savedCameraPosValid;
    private bool[] savedEnabledStates;

    private void Reset()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    public void LockCameraToRenderer()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || targetRenderer == null) return;

        SaveState();

        Vector3 center = targetRenderer.bounds.center;

        float y = keepCurrentY ? targetCamera.transform.position.y : forcedY;
        float z = targetCamera.transform.position.z;

        targetCamera.transform.position = new Vector3(center.x, y, z);

        DisablePanScripts();
    }

    public void UnlockCamera()
    {
        RestorePanScripts();

        if (savedCameraPosValid && targetCamera != null)
        {
            targetCamera.transform.position = savedCameraPos;
        }
    }

    private void SaveState()
    {
        if (targetCamera != null)
        {
            savedCameraPos = targetCamera.transform.position;
            savedCameraPosValid = true;
        }

        if (panScriptsToDisable == null) return;

        savedEnabledStates = new bool[panScriptsToDisable.Length];
        for (int i = 0; i < panScriptsToDisable.Length; i++)
        {
            if (panScriptsToDisable[i] == null) continue;
            savedEnabledStates[i] = panScriptsToDisable[i].enabled;
        }
    }

    private void DisablePanScripts()
    {
        if (panScriptsToDisable == null) return;

        for (int i = 0; i < panScriptsToDisable.Length; i++)
        {
            if (panScriptsToDisable[i] == null) continue;
            panScriptsToDisable[i].enabled = false;
        }
    }

    private void RestorePanScripts()
    {
        if (panScriptsToDisable == null || savedEnabledStates == null) return;

        int count = Mathf.Min(panScriptsToDisable.Length, savedEnabledStates.Length);
        for (int i = 0; i < count; i++)
        {
            if (panScriptsToDisable[i] == null) continue;
            panScriptsToDisable[i].enabled = savedEnabledStates[i];
        }
    }
}