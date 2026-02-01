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

    [Header("Transition Settings")]
    public bool crossfadeEnabled = true;
    public float crossfadeDuration = 1.0f; // seconds for crossfade

    // Optional ambient looping clip that plays until the radio is started
    [Header("Ambient Loop (will be disabled when radio starts)")]
    public AudioClip ambientLoopClip;
    private AudioSource ambientSource;
    private bool ambientPlaying;

    // Keep a reference to the originally assigned clip so RestoreMainMusic still works
    private AudioClip mainMusic;

    // Event kept for external listeners (AudioManager does not auto-invoke it anymore).
    public event Action OnMusicFinished;

    private bool isFading = false;
    private Coroutine crossfadeCoroutine;

    // secondary source used for crossfading
    private AudioSource secondaryMusicSource;
    private float musicVolume = 1f;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (musicSource != null)
            musicSource.outputAudioMixerGroup = musicGroup;

        // create a secondary internal audio source for smooth crossfades
        secondaryMusicSource = gameObject.AddComponent<AudioSource>();
        secondaryMusicSource.playOnAwake = false;
        secondaryMusicSource.outputAudioMixerGroup = musicGroup;
        secondaryMusicSource.spatialBlend = 0f;
        // copy loop setting from the inspector-assigned musicSource to keep behavior consistent
        secondaryMusicSource.loop = musicSource != null ? musicSource.loop : false;

        // create ambient source for looping background audio
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.playOnAwake = false;
        ambientSource.loop = true;
        ambientSource.spatialBlend = 0f;
        ambientSource.outputAudioMixerGroup = musicGroup;

        // store default volume
        musicVolume = musicSource != null ? musicSource.volume : 1f;

        // fallback: store initial clip if present
        mainMusic = musicSource != null ? musicSource.clip : null;
    }

    private void Start()
    {
        // Start ambient loop if assigned
        if (ambientLoopClip != null && ambientSource != null)
        {
            ambientSource.clip = ambientLoopClip;
            ambientSource.volume = musicVolume;
            ambientSource.loop = true;
            ambientSource.Play();
            ambientPlaying = true;
        }

        // Start main music if the musicSource already has a clip assigned
        if (mainMusic != null && musicSource != null)
        {
            PlayMusic(mainMusic);
        }
    }

    /// <summary>
    /// Stop and disable the ambient looping audio (called by RadioController when radio starts).
    /// </summary>
    public void DisableAmbientLooping()
    {
        if (ambientSource == null) return;
        if (ambientSource.isPlaying) ambientSource.Stop();
        ambientSource.loop = false;
        ambientPlaying = false;
    }

    /// <summary>
    /// Returns whether ambient loop is currently playing.
    /// </summary>
    public bool IsAmbientPlaying => ambientPlaying;

    public void PlayMusic(AudioClip clip)
    {
        // stop any in-progress crossfade
        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);

        // If we want a crossfade and there is currently music playing, use crossfade
        if (crossfadeEnabled && musicSource != null && musicSource.isPlaying && crossfadeDuration > 0f)
        {
            crossfadeCoroutine = StartCoroutine(CrossfadeToClip(clip, crossfadeDuration));
            return;
        }

        // Otherwise do instant switch
        if (musicSource == null) return;
        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
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

        crossfadeCoroutine = null;
    }

    public void RestoreMainMusic()
    {
        if (mainMusic != null)
            PlayMusic(mainMusic);
    }

    public void MuteMainMusic(bool mute)
    {
        // mute both sources for safety
        if (musicSource != null) musicSource.mute = mute;
        if (secondaryMusicSource != null) secondaryMusicSource.mute = mute;
        if (ambientSource != null) ambientSource.mute = mute;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
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

    internal void SetMusicVolume(float music)
    {
        throw new NotImplementedException();
    }
}
