using UnityEngine;

public class RhythmMiniGameManager : MonoBehaviour
{
    public static RhythmMiniGameManager Instance { get; private set; }

    [Header("References")]
    public GameObject rhythmUI;         // rhythm minigame UI
    public GameObject mainGameUI;       // dish clicker UI

    private bool isActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenMiniGame()
    {
        if (isActive) return;
        isActive = true;
        rhythmUI.SetActive(true);

       

        //  Dim main game UI
        mainGameUI.SetActive(false);
    }

    public void CloseMiniGame()
    {
        if (!isActive) return;

        isActive = false;
        rhythmUI.SetActive(false);
        mainGameUI.SetActive(true);
    }
}
