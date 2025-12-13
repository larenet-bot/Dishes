using UnityEngine;

public class HelpOverlay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainGameUI;     // root of your regular HUD
    [SerializeField] private GameObject helpOverlayUI;  // the HelpOverlayUI object

    [Header("Behaviour")]
    [SerializeField] private bool pauseGame = true;

    private bool isOpen = false;
    private float previousTimeScale = 1f;

    private void Start()
    {
        if (helpOverlayUI != null)
            helpOverlayUI.SetActive(false);
    }

    // Hook this to the "?" button OnClick
    public void ToggleHelp()
    {
        if (isOpen)
            CloseHelp();
        else
            OpenHelp();
    }

    public void OpenHelp()
    {
        if (isOpen) return;
        isOpen = true;

        // SHOW help
        if (helpOverlayUI != null)
            helpOverlayUI.SetActive(true);

        // HIDE main game UI
        if (mainGameUI != null)
            mainGameUI.SetActive(false);

        if (pauseGame)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    // Hook this to CloseArea Button OnClick (click-anywhere-to-close)
    public void CloseHelp()
    {
        if (!isOpen) return;
        isOpen = false;

        // HIDE help
        if (helpOverlayUI != null)
            helpOverlayUI.SetActive(false);

        // SHOW main game UI again
        if (mainGameUI != null)
            mainGameUI.SetActive(true);

        if (pauseGame)
            Time.timeScale = previousTimeScale;
    }
}
