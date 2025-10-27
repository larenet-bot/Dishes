using UnityEngine;

public class InGameSettingsButton : MonoBehaviour
{
    [SerializeField] private GameObject settingsMenu;

    private bool isOpen = false;

    public void ToggleSettings()
    {
        isOpen = !isOpen;
        settingsMenu.SetActive(isOpen);

        // Pause game when menu is open
        Time.timeScale = isOpen ? 0f : 1f;
        Debug.Log("Time.timeScale = " + Time.timeScale);

    }
    public void CloseSettings()
    {
        settingsMenu.SetActive(false);
        isOpen = false;
        Time.timeScale = 1f;
    }


}

