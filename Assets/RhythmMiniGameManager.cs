using UnityEngine;

public class RhythmMiniGameToggle : MonoBehaviour
{
    [Header("References")]
    public GameObject rhythmMiniGame;   // parent object of rhythm elements
    public GameObject mainGameUI;       //  game  UI

    private bool isActive = false;

    public void ToggleMiniGame()
    {
        isActive = !isActive;
        rhythmMiniGame.SetActive(isActive);
        mainGameUI.SetActive(!isActive);

    }
}
