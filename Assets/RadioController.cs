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

    [Header("Playback")]
    [Tooltip("When false the radio will not start playing on Scene Start. Call StartRadio() to begin playback (useful when radio must be purchased first).")]
    public bool autoPlayOnAwake = false;

    [Header("Purchase")]
    [Tooltip("When true the radio must be purchased via Upgrades before it can play.")]
    [SerializeField] private bool requirePurchase = true;
    private bool isPurchased = false;

    // Allow external systems (Upgrades / save code) to mark this radio as purchased.
    public bool IsPurchased => isPurchased;
    public void MarkPurchased() => isPurchased = true;

    // Allow external systems to query whether the radio is currently playing.
    public bool IsPlaying => radioAudioSource != null && radioAudioSource.isPlaying;

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

        // If purchase is required and the radio hasn't been marked purchased yet,
        // check the Upgrades instance (if present) to see if the player already owns it.
        if (requirePurchase && !isPurchased)
        {
            var upgrades = FindFirstObjectByType<Upgrades>();
            if (upgrades != null && upgrades.RadioPurchased)
            {
                isPurchased = true;
            }
        }

        // Only play automatically if explicitly allowed and (if required) purchased.
        if (autoPlayOnAwake && (!requirePurchase || isPurchased))
        {
            // Use StartRadio so we get consistent behavior (including persistence)
            StartRadio();
        }
    }

    /// <summary>
    /// Start radio playback (stops ambient via AudioManager if present).
    /// Safe to call multiple times.
    /// Will refuse to start if purchase is required and not yet owned.
    /// </summary>
    public void StartRadio()
    {
        if (requirePurchase && !isPurchased)
        {
            Debug.Log("[RadioCOntroller] StartRadio called but radio not purchased.");
            return;
        }

        if (radioAudioSource == null)
            radioAudioSource = GetComponent<AudioSource>();

        // Ask AudioManager to stop ambient loop if available and persist that choice
        if (AudioManager.instance != null)
        {
            AudioManager.instance.DisableAmbientLooping(true);
        }

        if (audiotracks == null || audiotracks.Length == 0)
        {
            Debug.LogWarning("[RadioCOntroller] No audio tracks assigned.");
            return;
        }

        // ensure track is correct and playing
        updateTrack(currentTrackIndex);
        if (!radioAudioSource.isPlaying)
        {
            radioAudioSource.volume = 1f; // use audio source volume (AudioManager may control mixer)
            radioAudioSource.Play();
        }

        // ensure the playback monitor coroutine is running
        StartCoroutine(PlaybackMonitor());
    }

    public void skipforwardButton()
    {
        if (requirePurchase && !isPurchased) return;
        if (audiotracks == null || audiotracks.Length == 0) return;
        int next = (currentTrackIndex + 1) % audiotracks.Length;
        StartTransitionTo(next);
    }

    public void skipbackwardButton()
    {
        if (requirePurchase && !isPurchased) return;
        if (audiotracks == null || audiotracks.Length == 0) return;
        int prev = (currentTrackIndex - 1 + audiotracks.Length) % audiotracks.Length;
        StartTransitionTo(prev);
    }

    public void PlayAudio()
    {
        if (requirePurchase && !isPurchased) return;
        if (isTransitioning) return;
        if (!radioAudioSource.isPlaying)
        {
            // Ensure ambient loop is disabled when radio starts playing and persist (radio can only play if purchased)
            if (AudioManager.instance != null)
                AudioManager.instance.DisableAmbientLooping(true);

            radioAudioSource.Play();
        }
    }

    void updateTrack(int index)
    {
        radioAudioSource.clip = audiotracks[index].trackAudioClip;
        if (trackTitleText != null)
            trackTitleText.text = audiotracks[index].trackAudioClip.name;
    }

    public void PauseAudio()
    {
        if (requirePurchase && !isPurchased) return;
        radioAudioSource.Pause();
    }

    public void StopAudio()
    {
        if (requirePurchase && !isPurchased) return;
        if (isTransitioning) return;
        radioAudioSource.Stop();

        // Optionally re-enable ambient loop when radio stops:
        //if (AudioManager.instance != null)
        //    AudioManager.instance.EnableAmbientLooping();
    }

    public void LoopAudio()
    {
        if (requirePurchase && !isPurchased) return;
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