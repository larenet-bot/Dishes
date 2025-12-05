using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Mixer Group")]
    public AudioMixerGroup musicGroup;

    [Header("Main Game Song Pool")]
    public AudioClip[] startingSongs;   // random selection pool

    private AudioClip mainMusic;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        musicSource.outputAudioMixerGroup = musicGroup;

        // fallback: in case startingSongs is empty
        mainMusic = musicSource.clip;
    }

    private void Start()
    {
        // Randomize main music if songs are provided
        if (startingSongs != null && startingSongs.Length > 0)
        {
            int index = Random.Range(0, startingSongs.Length);
            mainMusic = startingSongs[index];
        }

        // Start music
        if (mainMusic != null)
            PlayMusic(mainMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void RestoreMainMusic()
    {
        PlayMusic(mainMusic);
    }

    public void MuteMainMusic(bool mute)
    {
        musicSource.mute = mute;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public IEnumerator FadeOutMusic(float duration)
    {
        float startVolume = musicSource.volume;

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVolume; // reset for next track
    }

}
