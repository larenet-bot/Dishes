using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    public InGameSettingsButton settingsButton;

    [Header("UI References")]
    public GameObject pauseMenuUI;

    [Header("World Objects Disabled During Pause")]
    [SerializeField, Tooltip("Scene objects placed here will have their Collider2D disabled while paused. Use this for the duck and other fixed clickable objects.")]
    private GameObject[] objectsToDisableDuringPause;

    [SerializeField, Tooltip("Also disables every active Collider2D on this layer while paused. Layer 6 is your bubbles layer.")]
    private int bubbleLayer = 6;

    [SerializeField, Tooltip("How often to check for newly spawned bubbles while paused.")]
    private float pausedColliderRefreshInterval = 0.1f;

    [Header("Scene References")]
    [SerializeField, Tooltip("Name of the menu scene to load (must be added to Build Settings)")]
    private string menuSceneName = "Menu";

    private readonly List<Collider2D> collidersDisabledByPause = new List<Collider2D>();
    private float pausedColliderRefreshTimer = 0f;

    public bool IsPauseMenuOpen
    {
        get { return pauseMenuUI != null && pauseMenuUI.activeSelf; }
    }

    public GameObject PauseMenuRoot
    {
        get { return pauseMenuUI; }
    }

    void Update()
    {
        // Escape is handled by MenuManager so every menu follows the same rules.
        if (GameIsPaused)
        {
            pausedColliderRefreshTimer += Time.unscaledDeltaTime;

            if (pausedColliderRefreshTimer >= pausedColliderRefreshInterval)
            {
                pausedColliderRefreshTimer = 0f;
                DisablePausedObjectColliders(false);
            }
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        RestorePausedObjectColliders();

        Time.timeScale = 1f;
        GameIsPaused = false;

        Debug.Log("Game Resumed");
    }

    public void Settings()
    {
        if (settingsButton != null)
            settingsButton.ToggleSettings();

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        RestorePausedObjectColliders();

        Time.timeScale = 1f;
        GameIsPaused = false;

        Debug.Log("Game Resumed");
    }

    public void Pause()
    {
        if (settingsButton != null)
            settingsButton.CloseSettings();

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);

        pausedColliderRefreshTimer = 0f;
        DisablePausedObjectColliders(true);

        // Time keeps passing while paused.
        Time.timeScale = 1f;
        GameIsPaused = true;

        Debug.Log("Game Paused");
    }

    private void DisablePausedObjectColliders(bool clearExistingList)
    {
        if (clearExistingList)
        {
            collidersDisabledByPause.Clear();
        }

        DisableColliderArrayObjects();
        DisableCollidersOnLayer(bubbleLayer);
    }

    private void DisableColliderArrayObjects()
    {
        if (objectsToDisableDuringPause == null)
            return;

        for (int i = 0; i < objectsToDisableDuringPause.Length; i++)
        {
            GameObject obj = objectsToDisableDuringPause[i];

            if (obj == null)
                continue;

            Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>(true);

            for (int c = 0; c < colliders.Length; c++)
            {
                DisableColliderIfEnabled(colliders[c]);
            }
        }
    }

    private void DisableCollidersOnLayer(int layer)
    {
        Collider2D[] allColliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);

        for (int i = 0; i < allColliders.Length; i++)
        {
            Collider2D collider = allColliders[i];

            if (collider == null)
                continue;

            if (!ColliderOrParentIsOnLayer(collider, layer))
                continue;

            DisableColliderIfEnabled(collider);
        }
    }

    private bool ColliderOrParentIsOnLayer(Collider2D collider, int layer)
    {
        if (collider == null)
            return false;

        Transform current = collider.transform;

        while (current != null)
        {
            if (current.gameObject.layer == layer)
                return true;

            current = current.parent;
        }

        return false;
    }

    private void DisableColliderIfEnabled(Collider2D collider)
    {
        if (collider == null)
            return;

        if (!collider.enabled)
            return;

        collider.enabled = false;

        if (!collidersDisabledByPause.Contains(collider))
        {
            collidersDisabledByPause.Add(collider);
        }
    }

    private void RestorePausedObjectColliders()
    {
        for (int i = 0; i < collidersDisabledByPause.Count; i++)
        {
            if (collidersDisabledByPause[i] != null)
            {
                collidersDisabledByPause[i].enabled = true;
            }
        }

        collidersDisabledByPause.Clear();
        pausedColliderRefreshTimer = 0f;
    }

    public void LoadMenu()
    {
        Debug.Log("Loading Menu...");

        RestorePausedObjectColliders();

        Time.timeScale = 1f;
        GameIsPaused = false;

        if (string.IsNullOrWhiteSpace(menuSceneName))
        {
            Debug.LogError("PauseMenu.LoadMenu: menuSceneName is empty. Set the scene name in the inspector and add it to Build Settings.");
            return;
        }

        StartCoroutine(LoadMenuAsync());
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");

        RestorePausedObjectColliders();

        Time.timeScale = 1f;
        GameIsPaused = false;

        Application.Quit();
    }

    private IEnumerator LoadMenuAsync()
    {
        var asyncOp = SceneManager.LoadSceneAsync(menuSceneName);

        if (asyncOp == null)
        {
            Debug.LogError($"PauseMenu.LoadMenu: Scene '{menuSceneName}' not found. Make sure it is added to Build Settings.");
            yield break;
        }

        while (!asyncOp.isDone)
            yield return null;
    }
}