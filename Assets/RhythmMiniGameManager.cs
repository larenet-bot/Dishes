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

    public void ToggleMiniGame()
    {
        isActive = !isActive;

        rhythmMiniGame.SetActive(isActive);
        mainGameUI.SetActive(!isActive);

        if (isActive)
        {
            // Mute main game music
            AudioManager.instance.MuteMainMusic(true);

            // Set flag so Update() waits for space press
            waitingForStart = true;
        }
        else
        {
            // Stop minigame audio
            if (minigameAudioSource != null)
                minigameAudioSource.Stop();

            // Restore main game music
            AudioManager.instance.MuteMainMusic(false);
            AudioManager.instance.RestoreMainMusic();

            waitingForStart = false;
        }
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
