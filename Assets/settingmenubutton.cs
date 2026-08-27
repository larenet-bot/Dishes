using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameSettingsButton : MonoBehaviour
{
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private string menuSceneName = "MainMenu";

    private bool isOpen = false;

    public void ToggleSettings()
    {
        Debug.Log("GEAR CLICKED - Before: isOpen = " + isOpen);

        isOpen = !isOpen;
        settingsMenu.SetActive(isOpen);

        Debug.Log("GEAR CLICKED - After: isOpen = " + isOpen);
        Debug.Log("Settings Menu Active = " + settingsMenu.activeSelf);
    }

    public void CloseSettings()
    {
        Debug.Log("BACK CLICKED - Closing settings");

        isOpen = false;
        settingsMenu.SetActive(false);

        Debug.Log("BACK CLICKED - isOpen = " + isOpen);
        Debug.Log("Settings Menu Active = " + settingsMenu.activeSelf);
    }

    public void GoToMenu()
    {
        CloseSettings();

        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogWarning("Menu scene name is empty. Set `menuSceneName` in the Inspector.");
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }
}