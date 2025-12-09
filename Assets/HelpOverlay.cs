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

        if (helpOverlayUI != null)
            helpOverlayUI.SetActive(true);

        // Keep mainGameUI visible underneath if you want it dimmed by overlay,
        // or hide it if you want help to be its own screen.
        if (mainGameUI != null)
            mainGameUI.SetActive(true);

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

        if (helpOverlayUI != null)
            helpOverlayUI.SetActive(false);

        if (pauseGame)
            Time.timeScale = previousTimeScale;
    }
}
