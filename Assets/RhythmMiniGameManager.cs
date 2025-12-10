using System.Collections;
using UnityEngine;

public class RhythmMiniGameToggle : MonoBehaviour
{
    [Header("References")]
    public GameObject rhythmMiniGame;
    public GameObject mainGameUI;

    [Header("Minigame Audio")]
    public AudioSource minigameAudioSource;

    private bool isActive = false;
    private bool waitingForStart = false;

    void Update()
    {
        // If minigame is active and waiting for user to start it
        if (isActive && waitingForStart)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartMinigameMusic();
            }
        }
    }

    // Legacy toggle kept for compatibility with existing wiring
    public void ToggleMiniGame()
    {
        if (!isActive) OpenMiniGame(waitForSpace: true);
        else CloseMiniGame();
    }

    // New API: Open the minigame UI. If waitForSpace==false you expect to start audio/spawning immediately from caller.
    public void OpenMiniGame(bool waitForSpace = true)
    {
        if (isActive) return;

        isActive = true;
        if (rhythmMiniGame != null) rhythmMiniGame.SetActive(true);
        if (mainGameUI != null) mainGameUI.SetActive(false);

        if (AudioManager.instance != null)
            AudioManager.instance.MuteMainMusic(true);

        waitingForStart = waitForSpace;
    }

    // Close the mini-game UI and stop minigame audio
    public void CloseMiniGame()
    {
        if (!isActive) return;

        isActive = false;
        if (rhythmMiniGame != null) rhythmMiniGame.SetActive(false);
        if (mainGameUI != null) mainGameUI.SetActive(true);

        if (minigameAudioSource != null)
            minigameAudioSource.Stop();

        if (AudioManager.instance != null)
        {
            AudioManager.instance.MuteMainMusic(false);
            AudioManager.instance.RestoreMainMusic();
        }

        waitingForStart = false;
    }

    private void StartMinigameMusic()
    {
        if (minigameAudioSource != null)
        {
            minigameAudioSource.time = 0f;
            minigameAudioSource.Play();
        }

        waitingForStart = false;
    }
}
