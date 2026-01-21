using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;
    public InGameSettingsButton settingsButton;
    [Header("UI References")]
    public GameObject pauseMenuUI;

    [Header("Scene References")]
    [SerializeField, Tooltip("Name of the menu scene to load (must be added to Build Settings)")]
    private string menuSceneName = "Menu"; // editable in Inspector

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        Debug.Log("Game Resumed");
    }
    public void Settings()
    {
        settingsButton.ToggleSettings();
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        Debug.Log("Game Resumed");
    }

    public void Pause()
    {
        settingsButton.CloseSettings();
        pauseMenuUI.SetActive(true);
        Time.timeScale = 1f;
        GameIsPaused = true;
        Debug.Log("Game Paused");
    }

    // Call this from the pause menu button OnClick to go back to the menu scene.
    public void LoadMenu()
    {
        Debug.Log("Loading Menu...");
        // restore normal time and paused state before switching scenes
        Time.timeScale = 1f;
        GameIsPaused = false;

        if (string.IsNullOrWhiteSpace(menuSceneName))
        {
            Debug.LogError("PauseMenu.LoadMenu: menuSceneName is empty. Set the scene name in the inspector and add it to Build Settings.");
            return;
        }

        // Use async load to avoid hitching; this will activate the scene once loaded.
        StartCoroutine(LoadMenuAsync());
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");

        // Make sure time scale is reset (important if quitting from pause)
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

        // optional: show a loading UI here while asyncOp.progress < 0.9f
        while (!asyncOp.isDone)
            yield return null;
    }
}
