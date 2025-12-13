using System;
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

    [Header("Playback Settings")]
    public bool autoPlayNext = true;    // when a track ends, automatically pick another
    public bool avoidImmediateRepeat = true; // try to avoid playing the same song twice in a row

    [Header("Transition Settings")]
    public bool crossfadeEnabled = true;
    public float crossfadeDuration = 1.0f; // seconds for crossfade

    private AudioClip mainMusic;

    // Event fired when music finishes naturally (not when stopped by FadeOutMusic)
    public event Action OnMusicFinished;

    private bool isFading = false;
    private Coroutine watchCoroutine;
    private Coroutine crossfadeCoroutine;

    // secondary source used for crossfading
    private AudioSource secondaryMusicSource;
    private float musicVolume = 1f;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        musicSource.outputAudioMixerGroup = musicGroup;

        // create a secondary internal audio source for smooth crossfades
        secondaryMusicSource = gameObject.AddComponent<AudioSource>();
        secondaryMusicSource.playOnAwake = false;
        secondaryMusicSource.outputAudioMixerGroup = musicGroup;
        secondaryMusicSource.spatialBlend = 0f;
        // copy loop setting from the inspector-assigned musicSource to keep behavior consistent
        secondaryMusicSource.loop = musicSource != null ? musicSource.loop : false;

        // store default volume
        musicVolume = musicSource != null ? musicSource.volume : 1f;

        // fallback: in case startingSongs is empty
        mainMusic = musicSource.clip;
    }

    private void Start()
    {
        // Randomize main music if songs are provided
        if (startingSongs != null && startingSongs.Length > 0)
        {
            int index = UnityEngine.Random.Range(0, startingSongs.Length);
            mainMusic = startingSongs[index];
        }

        // Start music
        if (mainMusic != null)
            PlayMusic(mainMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        // stop any watcher for previous clip
        if (watchCoroutine != null) StopCoroutine(watchCoroutine);

        // stop any in-progress crossfade
        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);

        // If we want a crossfade and there is currently music playing, use crossfade
        if (crossfadeEnabled && musicSource != null && musicSource.isPlaying && crossfadeDuration > 0f)
        {
            crossfadeCoroutine = StartCoroutine(CrossfadeToClip(clip, crossfadeDuration));
            return;
        }

        // Otherwise do instant switch
        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();

        // start watcher to detect natural end
        watchCoroutine = StartCoroutine(WatchForMusicEnd());
    }

    private IEnumerator CrossfadeToClip(AudioClip newClip, float duration)
    {
        if (secondaryMusicSource == null || musicSource == null)
        {
            // fallback to normal PlayMusic behavior
            PlayMusic(newClip);
            yield break;
        }

        // prepare secondary source
        secondaryMusicSource.Stop();
        secondaryMusicSource.clip = newClip;
        secondaryMusicSource.volume = 0f;
        secondaryMusicSource.Play();

        float time = 0f;
        float startVol = musicVolume;

        // crossfade while both are playing
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            // fade out current, fade in new
            musicSource.volume = Mathf.Lerp(startVol, 0f, t);
            secondaryMusicSource.volume = Mathf.Lerp(0f, startVol, t);
            yield return null;
        }

        // finish: stop old source, ensure volumes are reset
        musicSource.Stop();
        musicSource.volume = startVol;
        secondaryMusicSource.volume = startVol;

        // swap references so 'musicSource' always points to the active source
        var temp = musicSource;
        musicSource = secondaryMusicSource;
        secondaryMusicSource = temp;

        // restart watcher for the new active musicSource
        if (watchCoroutine != null) StopCoroutine(watchCoroutine);
        watchCoroutine = StartCoroutine(WatchForMusicEnd());

        crossfadeCoroutine = null;
    }

    private IEnumerator WatchForMusicEnd()
    {
        if (musicSource == null || musicSource.clip == null) yield break;

        // Wait until clip finishes (not stopped)
        while (musicSource.isPlaying)
            yield return null;

        // small frame delay to ensure stop wasn't due to our FadeOutMusic
        yield return null;

        if (isFading)
            yield break;

        // If configured, automatically play a new random song from the pool
        if (autoPlayNext && startingSongs != null && startingSongs.Length > 0)
        {
            AudioClip next = GetRandomNextClip(musicSource.clip);
            // If next is null fallback to mainMusic (rare)
            if (next == null) next = mainMusic;
            if (next != null)
            {
                PlayMusic(next);
                yield break;
            }
        }

        // Otherwise notify listeners that music finished
        OnMusicFinished?.Invoke();
    }

    private AudioClip GetRandomNextClip(AudioClip exclude)
    {
        if (startingSongs == null || startingSongs.Length == 0) return null;
        if (startingSongs.Length == 1) return startingSongs[0];

        // Pick random index, optionally avoiding the excluded clip
        int attempts = 0;
        int idx = UnityEngine.Random.Range(0, startingSongs.Length);
        if (avoidImmediateRepeat)
        {
            while (startingSongs[idx] == exclude && attempts < 10)
            {
                idx = UnityEngine.Random.Range(0, startingSongs.Length);
                attempts++;
            }
        }
        return startingSongs[idx];
    }

    public void RestoreMainMusic()
    {
        PlayMusic(mainMusic);
    }

    public void MuteMainMusic(bool mute)
    {
        // mute both sources for safety
        if (musicSource != null) musicSource.mute = mute;
        if (secondaryMusicSource != null) secondaryMusicSource.mute = mute;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public IEnumerator FadeOutMusic(float duration)
    {
        isFading = true;
        float startVolume = musicSource != null ? musicSource.volume : 1f;
        float startVolume2 = secondaryMusicSource != null ? secondaryMusicSource.volume : 0f;

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            if (musicSource != null) musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            if (secondaryMusicSource != null && secondaryMusicSource.isPlaying) secondaryMusicSource.volume = Mathf.Lerp(startVolume2, 0f, t);
            yield return null;
        }

        if (musicSource != null) musicSource.Stop();
        if (secondaryMusicSource != null && secondaryMusicSource.isPlaying) secondaryMusicSource.Stop();

        if (musicSource != null) musicSource.volume = startVolume; // reset for next track
        if (secondaryMusicSource != null) secondaryMusicSource.volume = startVolume; // reset

        isFading = false;
    }

}
