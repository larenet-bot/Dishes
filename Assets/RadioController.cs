using System.Collections;
using UnityEngine;

public class RadioCOntroller : MonoBehaviour
{
    [Header("List of Tracks")]
    [SerializeField] private Track[] audiotracks;
    private int currentTrackIndex;

    [Header("Text UI")]
    [SerializeField] private TMPro.TextMeshProUGUI trackTitleText;

    private AudioSource radioAudioSource;
    private bool isTransitioning;

    private void Start()
    {
        radioAudioSource = GetComponent<AudioSource>();

        if (audiotracks == null || audiotracks.Length == 0)
        {
            Debug.LogWarning("No audio tracks assigned to RadioCOntroller.");
            return;
        }

        currentTrackIndex = 0;
        updateTrack(currentTrackIndex);
        radioAudioSource.Play();
        StartCoroutine(PlaybackMonitor());
    }

    public void skipforwardButton()
    {
        if (audiotracks == null || audiotracks.Length == 0) return;
        int next = (currentTrackIndex + 1) % audiotracks.Length;
        StartTransitionTo(next);
    }

    public void skipbackwardButton()
    {
        if (audiotracks == null || audiotracks.Length == 0) return;
        int prev = (currentTrackIndex - 1 + audiotracks.Length) % audiotracks.Length;
        StartTransitionTo(prev);
    }

    public void PlayAudio()
    {
        if (isTransitioning) return;
        if (!radioAudioSource.isPlaying)
        {
            StartCoroutine(FadeIn(radioAudioSource, 0.5f));
        }
    }

    void updateTrack(int index)
    {
        radioAudioSource.clip = audiotracks[index].trackAudioClip;
        trackTitleText.text = audiotracks[index].trackAudioClip.name;
    }

    public void PauseAudio()
    {
        radioAudioSource.Pause();
    }

    public void StopAudio()
    {
        if (isTransitioning) return;
        StartCoroutine(FadeOut(radioAudioSource, 0.5f));
    }

    public void LoopAudio()
    {
        radioAudioSource.loop = !radioAudioSource.loop;
    }

    private void StartTransitionTo(int newIndex)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionToTrack(newIndex));
    }

    private IEnumerator TransitionToTrack(int newIndex)
    {
        isTransitioning = true;
        yield return StartCoroutine(FadeOut(radioAudioSource, 0.5f));
        currentTrackIndex = newIndex;
        updateTrack(currentTrackIndex);
        yield return StartCoroutine(FadeIn(radioAudioSource, 0.5f));
        isTransitioning = false;
    }

    public IEnumerator FadeOut(AudioSource audioSource, float fadeTime)
    {
        if (audioSource == null) yield break;
        float startVolume = audioSource.volume;
        while (audioSource.volume > 0f)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    public IEnumerator FadeIn(AudioSource audioSource, float fadeTime)
    {
        if (audioSource == null) yield break;
        audioSource.Play();
        audioSource.volume = 0f;
        while (audioSource.volume < 1.0f)
        {
            audioSource.volume += Time.deltaTime / fadeTime;
            yield return null;
        }
        audioSource.volume = 1f;
    }

    private IEnumerator PlaybackMonitor()
    {
        const float endEpsilon = 0.05f;

        while (true)
        {
            if (radioAudioSource == null || radioAudioSource.clip == null)
            {
                yield return null;
                continue;
            }

            // Wait until the clip starts playing
            while (!radioAudioSource.isPlaying)
            {
                yield return null;
            }

            // Wait until it stops playing (either ended, paused or stopped)
            while (radioAudioSource.isPlaying)
            {
                yield return null;
            }

            // If a transition is running, skip reacting here.
            if (isTransitioning)
            {
                yield return null;
                continue;
            }

            // Determine whether the clip ended naturally (time near clip length).
            float clipLength = radioAudioSource.clip != null ? radioAudioSource.clip.length : 0f;
            float time = radioAudioSource.time;

            if (clipLength > 0f && time >= clipLength - endEpsilon)
            {
                // advance to next track and wrap around
                int next = (currentTrackIndex + 1) % audiotracks.Length;
                StartTransitionTo(next);

                // wait until transition completes before continuing monitoring
                while (isTransitioning)
                {
                    yield return null;
                }
            }
            else
            {
                // stopped or paused manually — do nothing and continue monitoring
                yield return null;
            }
        }
    }
}
