using UnityEngine;

public class RhythmMiniGameToggle : MonoBehaviour
{
    [Header("References")]
    public GameObject rhythmMiniGame;      // parent object for minigame UI
    public GameObject mainGameUI;          // normal game UI

    //[Header("Audio Sources")]
    //public AudioSource mainAudioSource;     // background music or ambient audio
    //public AudioSource minigameAudioSource; // rhythm game music

    private bool isActive = false;


    [Header("Main Game Audio Sources")]
    public AudioSource[] mainGameAudioSources;

    [Header("Minigame Audio")]
    public AudioSource minigameAudioSource;

    public void ToggleMiniGame()
    {
        isActive = !isActive;

        rhythmMiniGame.SetActive(isActive);
        mainGameUI.SetActive(!isActive);

        if (isActive)
        {
            foreach (var src in mainGameAudioSources)
                if (src != null) src.Stop();

            if (minigameAudioSource != null)
            {
                minigameAudioSource.time = 0;
                minigameAudioSource.Play();
            }
        }
        else
        {
            foreach (var src in mainGameAudioSources)
                if (src != null) src.Play();

            if (minigameAudioSource != null)
                minigameAudioSource.Stop();
        }
    }

}
