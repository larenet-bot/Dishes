using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    public AudioSource musicSource;
    public AudioMixerGroup musicGroup;

    private AudioClip mainMusic;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        musicSource.outputAudioMixerGroup = musicGroup;

        mainMusic = musicSource.clip; // store main menu or level music
    }

    // Play any music clip
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource.isPlaying) musicSource.Stop();
        musicSource.clip = clip;
        musicSource.Play();
    }

    // Restore original music
    public void RestoreMainMusic()
    {
        PlayMusic(mainMusic);
    }
}
