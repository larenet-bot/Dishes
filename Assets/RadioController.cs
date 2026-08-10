using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RadioCOntroller : MonoBehaviour
{
    [Header("List of Tracks")]
    [SerializeField] private Track[] audiotracks;
    [SerializeField] private Track noAudiotracks;

    [Header("Text UI")]
    [SerializeField] private TextMeshProUGUI trackTitleText;

    [Header("Track List UI")]
    [Tooltip("Scroll View Content transform. If blank, the template's parent is used.")]
    [SerializeField] private Transform trackListContent;

    [Tooltip("Disabled row/template object under the Scroll View Content.")]
    [SerializeField] private SongButtonUI trackRowTemplate;

    [Tooltip("If true, the radio rebuilds the scroll list from audiotracks on Start.")]
    [SerializeField] private bool rebuildTrackRowsOnStart = true;

    [Header("Playback Options")]
    [Tooltip("If true the playlist will restart from the first track after the last track finishes.")]
    [SerializeField] private bool loopPlaylist = true;

    private readonly List<SongButtonUI> trackRows = new List<SongButtonUI>();

    private AudioSource radioAudioSource;
    private Coroutine playbackMonitorCoroutine;

    private int currentTrackIndex;
    private bool isTransitioning;

    [Header("Playback")]
    [Tooltip("When false the radio will not start playing on Scene Start. Call StartRadio() to begin playback (useful when radio must be purchased first).")]
    public bool autoPlayOnAwake = false;

    [Header("Purchase")]
    [Tooltip("When true the radio must be purchased via Upgrades before it can play.")]
    [SerializeField] private bool requirePurchase = true;
    private bool isPurchased = false;

    public bool IsPurchased => isPurchased;
    public bool IsPlaying => radioAudioSource != null && radioAudioSource.isPlaying;

    public void MarkPurchased()
    {
        isPurchased = true;
        RefreshTrackRows();
    }

    private void OnEnable()
    {
        if (radioAudioSource == null)
            radioAudioSource = GetComponent<AudioSource>();

        RefreshTrackRows();

        if (radioAudioSource != null
            && radioAudioSource.clip != null
            && !radioAudioSource.isPlaying
            && (!requirePurchase || isPurchased))
        {
            StartRadio();
        }
    }

    private void OnDisable()
    {
        Debug.Log("Radio disabled");
    }

    private void OnDestroy()
    {
        Debug.Log("Radio destroyed");
    }

    private void Start()
    {
        Debug.Log("[Radio] Start called on " + gameObject.scene.name);

        radioAudioSource = GetComponent<AudioSource>();

        if (audiotracks == null || audiotracks.Length == 0)
        {
            Debug.LogWarning("No audio tracks assigned to RadioCOntroller.");
            return;
        }

        currentTrackIndex = Mathf.Clamp(currentTrackIndex, 0, audiotracks.Length - 1);
        updateTrack(currentTrackIndex);

        if (requirePurchase && !isPurchased)
        {
            var upgrades = FindAnyObjectByType<Upgrades>();

            if (upgrades != null && upgrades.RadioPurchased)
            {
                isPurchased = true;
            }
        }

        if (rebuildTrackRowsOnStart)
        {
            BuildTrackListUI();
        }

        // Kept matching current project behavior: StartRadio handles the unpurchased "no audio" case.
        StartRadio();
    }

    private void Update()
    {
        RefreshTrackRows();
    }

    public void StartRadio()
    {
        if (radioAudioSource == null)
            radioAudioSource = GetComponent<AudioSource>();

        if (radioAudioSource == null)
        {
            Debug.LogWarning("[RadioCOntroller] Missing AudioSource.");
            return;
        }

        if (requirePurchase && !isPurchased)
        {
            if (noAudiotracks != null && noAudiotracks.trackAudioClip != null)
            {
                radioAudioSource.clip = noAudiotracks.trackAudioClip;

                if (!radioAudioSource.isPlaying)
                {
                    radioAudioSource.volume = 1f;
                    radioAudioSource.Play();
                }
            }

            Debug.Log("[RadioCOntroller] StartRadio called but radio not purchased.");
            RefreshTrackRows();
            return;
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.DisableAmbientLooping(true);
        }

        if (audiotracks == null || audiotracks.Length == 0)
        {
            Debug.LogWarning("[RadioCOntroller] No audio tracks assigned.");
            return;
        }

        currentTrackIndex = Mathf.Clamp(currentTrackIndex, 0, audiotracks.Length - 1);
        updateTrack(currentTrackIndex);

        if (!radioAudioSource.isPlaying)
        {
            radioAudioSource.volume = 1f;
            radioAudioSource.Play();
        }

        EnsurePlaybackMonitorRunning();
        RefreshTrackRows();
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

        if (radioAudioSource == null)
            radioAudioSource = GetComponent<AudioSource>();

        if (radioAudioSource == null)
            return;

        if (!radioAudioSource.isPlaying)
        {
            if (AudioManager.instance != null)
                AudioManager.instance.DisableAmbientLooping(true);

            radioAudioSource.Play();
            EnsurePlaybackMonitorRunning();
        }

        RefreshTrackRows();
    }

    public void PlayTrackFromList(int index)
    {
        if (requirePurchase && !isPurchased) return;
        if (audiotracks == null || index < 0 || index >= audiotracks.Length) return;
        if (isTransitioning) return;

        if (radioAudioSource == null)
            radioAudioSource = GetComponent<AudioSource>();

        if (radioAudioSource == null)
            return;

        if (AudioManager.instance != null)
            AudioManager.instance.DisableAmbientLooping(true);

        if (index == currentTrackIndex)
        {
            if (radioAudioSource.clip == null)
                updateTrack(currentTrackIndex);

            if (!radioAudioSource.isPlaying)
            {
                radioAudioSource.Play();
                EnsurePlaybackMonitorRunning();
            }

            RefreshTrackRows();
            return;
        }

        StartTransitionTo(index);
    }

    private void updateTrack(int index)
    {
        if (audiotracks == null || audiotracks.Length == 0) return;
        if (radioAudioSource == null) radioAudioSource = GetComponent<AudioSource>();
        if (radioAudioSource == null) return;

        index = Mathf.Clamp(index, 0, audiotracks.Length - 1);

        Track track = audiotracks[index];
        AudioClip clip = track != null ? track.trackAudioClip : null;

        radioAudioSource.clip = clip;

        if (trackTitleText != null)
            trackTitleText.text = GetTrackTitle(index);
    }

    public void PauseAudio()
    {
        if (requirePurchase && !isPurchased) return;
        if (radioAudioSource == null) return;

        radioAudioSource.Pause();
        RefreshTrackRows();
    }

    public void StopAudio()
    {
        if (requirePurchase && !isPurchased) return;
        if (isTransitioning) return;
        if (radioAudioSource == null) return;

        radioAudioSource.Stop();
        RefreshTrackRows();

        // Optionally re-enable ambient loop when radio stops:
        //if (AudioManager.instance != null)
        //    AudioManager.instance.EnableAmbientLooping();
    }

    public void LoopAudio()
    {
        if (requirePurchase && !isPurchased) return;
        if (radioAudioSource == null) return;

        radioAudioSource.loop = !radioAudioSource.loop;
    }

    public void BuildTrackListUI()
    {
        if (trackRowTemplate == null)
        {
            return;
        }

        if (trackListContent == null)
        {
            trackListContent = trackRowTemplate.transform.parent;
        }

        if (trackListContent == null)
        {
            Debug.LogWarning("[RadioCOntroller] Track list content is not assigned.");
            return;
        }

        for (int i = trackRows.Count - 1; i >= 0; i--)
        {
            if (trackRows[i] != null && trackRows[i] != trackRowTemplate)
            {
                Destroy(trackRows[i].gameObject);
            }
        }

        trackRows.Clear();

        for (int i = trackListContent.childCount - 1; i >= 0; i--)
        {
            Transform child = trackListContent.GetChild(i);

            if (child == trackRowTemplate.transform)
                continue;

            if (child.name.StartsWith("TrackRow_"))
                Destroy(child.gameObject);
        }

        trackRowTemplate.gameObject.SetActive(false);

        if (audiotracks == null)
        {
            return;
        }

        for (int i = 0; i < audiotracks.Length; i++)
        {
            SongButtonUI row = Instantiate(trackRowTemplate, trackListContent);
            row.name = $"TrackRow_{i + 1:00}_{GetTrackTitle(i)}";
            row.gameObject.SetActive(true);
            row.Initialize(this, i, GetTrackTitle(i));
            trackRows.Add(row);
        }

        RefreshTrackRows();
    }

    private void RefreshTrackRows()
    {
        if (trackRows == null || trackRows.Count == 0)
            return;

        float currentProgress = GetCurrentTrackProgress();

        for (int i = 0; i < trackRows.Count; i++)
        {
            SongButtonUI row = trackRows[i];

            if (row == null)
                continue;

            bool isCurrent = i == currentTrackIndex;
            float progress = isCurrent ? currentProgress : 0f;
            row.Refresh(isCurrent, IsPlaying, progress, isTransitioning);
        }
    }

    private float GetCurrentTrackProgress()
    {
        if (radioAudioSource == null || radioAudioSource.clip == null)
            return 0f;

        float length = radioAudioSource.clip.length;

        if (length <= 0.001f)
            return 0f;

        return Mathf.Clamp01(radioAudioSource.time / length);
    }

    private string GetTrackTitle(int index)
    {
        if (audiotracks == null || index < 0 || index >= audiotracks.Length)
            return "Track";

        Track track = audiotracks[index];

        if (track != null && track.trackAudioClip != null)
            return track.trackAudioClip.name;

        return $"Track {index + 1}";
    }

    private void StartTransitionTo(int newIndex)
    {
        if (isTransitioning) return;
        if (audiotracks == null || audiotracks.Length == 0) return;
        if (newIndex < 0 || newIndex >= audiotracks.Length) return;

        StartCoroutine(TransitionToTrack(newIndex));
    }

    private IEnumerator TransitionToTrack(int newIndex)
    {
        isTransitioning = true;
        RefreshTrackRows();

        yield return StartCoroutine(FadeOut(radioAudioSource, 0.5f));

        currentTrackIndex = newIndex;
        updateTrack(currentTrackIndex);

        yield return StartCoroutine(FadeIn(radioAudioSource, 0.5f));

        isTransitioning = false;
        EnsurePlaybackMonitorRunning();
        RefreshTrackRows();
    }

    public IEnumerator FadeOut(AudioSource audioSource, float fadeTime)
    {
        if (audioSource == null) yield break;

        float startVolume = audioSource.volume;
        fadeTime = Mathf.Max(0.01f, fadeTime);

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

        fadeTime = Mathf.Max(0.01f, fadeTime);

        audioSource.Play();
        audioSource.volume = 0f;

        while (audioSource.volume < 1.0f)
        {
            audioSource.volume += Time.deltaTime / fadeTime;
            yield return null;
        }

        audioSource.volume = 1f;
    }

    private void EnsurePlaybackMonitorRunning()
    {
        if (playbackMonitorCoroutine != null)
            return;

        playbackMonitorCoroutine = StartCoroutine(PlaybackMonitor());
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

            while (!radioAudioSource.isPlaying)
            {
                yield return null;
            }

            while (radioAudioSource.isPlaying)
            {
                yield return null;
            }

            if (requirePurchase && !isPurchased)
            {
                yield return null;
                continue;
            }

            if (isTransitioning)
            {
                yield return null;
                continue;
            }

            float clipLength = radioAudioSource.clip != null ? radioAudioSource.clip.length : 0f;
            float time = radioAudioSource.time;

            if (clipLength > 0f && time >= clipLength - endEpsilon)
            {
                if (audiotracks == null || audiotracks.Length == 0)
                {
                    yield return null;
                    continue;
                }

                int nextIndex = currentTrackIndex + 1;

                if (nextIndex >= audiotracks.Length)
                {
                    if (loopPlaylist)
                    {
                        // restart at top
                        nextIndex = 0;
                    }
                    else
                    {
                        // reached end of queue and not configured to loop:
                        radioAudioSource.Stop();
                        RefreshTrackRows();
                        yield return null;
                        continue;
                    }
                }

                StartTransitionTo(nextIndex);

                while (isTransitioning)
                {
                    yield return null;
                }
            }
            else
            {
                yield return null;
            }
        }
    }
}
