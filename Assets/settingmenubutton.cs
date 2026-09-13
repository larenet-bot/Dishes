using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameSettingsButton : MonoBehaviour
{
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Guided Intro")]
    [SerializeField] private GuidedIntroManager guidedIntroManager;

    private bool isOpen = false;

    public void ToggleSettings()
    {
        isOpen = !isOpen;
        settingsMenu.SetActive(isOpen);

        // Tell the guided intro when the settings menu is opened.
        if (isOpen && guidedIntroManager != null)
        {
            guidedIntroManager.ReportSettingsOpened();
        }

        // Pause game when menu is open
        //Time.timeScale = isOpen ? 0f : 1f;
        //Debug.Log("Time.timeScale = " + Time.timeScale);
    }
    public void CloseSettings()
    {
        settingsMenu.SetActive(false);
        isOpen = false;
        //Time.timeScale = 1f;
    }

    public void GoToMenu()
    {
        // Ensure time is running and settings UI closed
        CloseSettings();

        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogWarning("Menu scene name is empty. Set `menuSceneName` in the inspector.");
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }
}

