using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RadioCOntroller : MonoBehaviour
{
    [Header("List of Tracks")]
    [SerializeField] private Track[] audiotracks;
    [SerializeField] private Track noAudiotracks;

    [Header("Text UI")]
    [SerializeField] private TextMeshProUGUI trackTitleText;

    [Header("Song Selection Menu")]
    [Tooltip("Parent transform under a ScrollRect/VerticalLayoutGroup where song cards will be spawned.")]
    [SerializeField] private Transform songCardContentParent;

    [Tooltip("Prefab for one song card. Put SongButtonUI on the prefab root.")]
    [SerializeField] private SongButtonUI songCardPrefab;

    [Tooltip("Optional toggle group if your song cards use toggles like radio buttons for the currently selected song.")]
    [SerializeField] private ToggleGroup songToggleGroup;

    [Tooltip("Optional text in the overlay header. If empty, trackTitleText is still updated.")]
    [SerializeField] private TextMeshProUGUI nowPlayingText;

    [Tooltip("Optional current-track progress bar for the old/global menu area.")]
    [SerializeField] private Image nowPlayingProgressFill;

    [Tooltip("Optional current-track timer for the old/global menu area.")]
    [SerializeField] private TextMeshProUGUI nowPlayingTimerText;

    [Tooltip("Optional play/pause button text for the old/global menu area.")]
    [SerializeField] private TextMeshProUGUI playPauseButtonText;

    [Tooltip("When true, song cards are rebuilt every time this object is enabled. Usually false is fine.")]
    [SerializeField] private bool rebuildSongCardsOnEnable = false;

    [Header("Playback")]
    [Tooltip("When false the radio will not start playing on Scene Start. Call StartRadio() to begin playback.")]
    public bool autoPlayOnAwake = false;

    [Tooltip("Seconds used for skip/selection fade transitions.")]
    [SerializeField] private float transitionSeconds = 0.5f;

    [Header("Purchase")]
    [Tooltip("When true the radio must be purchased via Upgrades before it can play.")]
    [SerializeField] private bool requirePurchase = true;

    private readonly List<SongButtonUI> spawnedSongCards = new List<SongButtonUI>();
    private readonly HashSet<int> disabledSongIndexes = new HashSet<int>();

    private AudioSource radioAudioSource;
    private Coroutine playbackMonitorCoroutine;
    private bool isTransitioning;
    private bool isPurchased;
    private int currentTrackIndex;

    public bool IsPurchased => isPurchased;
    public bool IsPlaying => radioAudioSource != null && radioAudioSource.isPlaying;
    public int CurrentTrackIndex => currentTrackIndex;

    public void MarkPurchased()
    {
        isPurchased = true;
        RefreshSongCards();
        RefreshNowPlayingUI();
    }

    private void Awake()
    {
        CacheAudioSource();
    }

    private void OnEnable()
    {
        CacheAudioSource();

        if (rebuildSongCardsOnEnable)
        {
            RebuildSongCards();
        }
        else
        {
            BuildSongCardsIfNeeded();
            RefreshSongCards();
        }

        RefreshNowPlayingUI();
    }

    private void Start()
    {
        Debug.Log("[Radio] Start called on " + gameObject.scene.name);

        CacheAudioSource();

        if (audiotracks == null || audiotracks.Length == 0)
        {
            Debug.LogWarning("No audio tracks assigned to RadioCOntroller.");
            RefreshNowPlayingUI();
            return;
        }

        currentTrackIndex = Mathf.Clamp(currentTrackIndex, 0, audiotracks.Length - 1);
        UpdateTrack(currentTrackIndex, keepCurrentTime: false);

        if (requirePurchase && !isPurchased)
        {
            Upgrades upgrades = FindAnyObjectByType<Upgrades>();
            if (upgrades != null && upgrades.RadioPurchased)
            {
                isPurchased = true;
            }
        }

        BuildSongCardsIfNeeded();
        RefreshSongCards();
        RefreshNowPlayingUI();

        if (autoPlayOnAwake && (!requirePurchase || isPurchased))
        {
            StartRadio();
        }
    }

    private void Update()
    {
        RefreshSongCards();
        RefreshNowPlayingUI();
    }

    public void StartRadio()
    {
        CacheAudioSource();

        if (radioAudioSource == null)
        {
            Debug.LogWarning("[RadioCOntroller] Missing AudioSource.");
            return;
        }

        if (requirePurchase && !isPurchased)
        {
            PlayUnpurchasedFallback();
            Debug.Log("[RadioCOntroller] StartRadio called but radio not purchased.");
            return;
        }

        if (audiotracks == null || audiotracks.Length == 0)
        {
            Debug.LogWarning("[RadioCOntroller] No audio tracks assigned.");
            return;
        }

        if (!IsTrackIndexValid(currentTrackIndex) || IsTrackDisabled(currentTrackIndex))
        {
            currentTrackIndex = FindNextEnabledTrackIndex(currentTrackIndex, 1);
        }

        if (!IsTrackIndexValid(currentTrackIndex))
        {
            Debug.LogWarning("[RadioCOntroller] No enabled audio tracks available.");
            return;
        }

        DisableAmbientLoopIfPossible();
        UpdateTrack(currentTrackIndex, keepCurrentTime: true);

        if (!radioAudioSource.isPlaying)
        {
            radioAudioSource.volume = 1f;
            radioAudioSource.Play();
        }

        EnsurePlaybackMonitorRunning();
        RefreshSongCards();
        RefreshNowPlayingUI();
    }

    public void PlayAudio()
    {
        if (!CanUsePurchasedRadio() || isTransitioning)
        {
            return;
        }

        DisableAmbientLoopIfPossible();
        CacheAudioSource();

        if (radioAudioSource != null && !radioAudioSource.isPlaying)
        {
            radioAudioSource.Play();
            EnsurePlaybackMonitorRunning();
        }

        RefreshSongCards();
        RefreshNowPlayingUI();
    }

    public void PauseAudio()
    {
        if (!CanUsePurchasedRadio())
        {
            return;
        }

        CacheAudioSource();
        radioAudioSource?.Pause();
        RefreshSongCards();
        RefreshNowPlayingUI();
    }

    public void TogglePlayPause()
    {
        if (!CanUsePurchasedRadio())
        {
            return;
        }

        CacheAudioSource();

        if (radioAudioSource == null)
        {
            return;
        }

        if (radioAudioSource.isPlaying)
        {
            PauseAudio();
        }
        else
        {
            PlayAudio();
        }
    }

    public void StopAudio()
    {
        if (!CanUsePurchasedRadio() || isTransitioning)
        {
            return;
        }

        CacheAudioSource();
        radioAudioSource?.Stop();
        RefreshSongCards();
        RefreshNowPlayingUI();
    }

    public void LoopAudio()
    {
        if (!CanUsePurchasedRadio())
        {
            return;
        }

        CacheAudioSource();

        if (radioAudioSource != null)
        {
            radioAudioSource.loop = !radioAudioSource.loop;
        }
    }

    public void skipforwardButton()
    {
        if (!CanUsePurchasedRadio() || audiotracks == null || audiotracks.Length == 0)
        {
            return;
        }

        int next = FindNextEnabledTrackIndex(currentTrackIndex, 1);
        if (next != currentTrackIndex)
        {
            StartTransitionTo(next);
        }
    }

    public void skipbackwardButton()
    {
        if (!CanUsePurchasedRadio() || audiotracks == null || audiotracks.Length == 0)
        {
            return;
        }

        int previous = FindNextEnabledTrackIndex(currentTrackIndex, -1);
        if (previous != currentTrackIndex)
        {
            StartTransitionTo(previous);
        }
    }

    public void PlayTrackByIndex(int index)
    {
        if (!CanUsePurchasedRadio())
        {
            return;
        }

        if (!IsTrackIndexValid(index) || IsTrackDisabled(index))
        {
            return;
        }

        if (index == currentTrackIndex)
        {
            PlayAudio();
            return;
        }

        StartTransitionTo(index);
    }

    public void RebuildSongCards()
    {
        for (int i = spawnedSongCards.Count - 1; i >= 0; i--)
        {
            if (spawnedSongCards[i] != null)
            {
                Destroy(spawnedSongCards[i].gameObject);
            }
        }

        spawnedSongCards.Clear();
        BuildSongCardsIfNeeded();
        RefreshSongCards();
    }

    private void BuildSongCardsIfNeeded()
    {
        if (spawnedSongCards.Count > 0)
        {
            return;
        }

        if (songCardContentParent == null || songCardPrefab == null)
        {
            return;
        }

        if (audiotracks == null)
        {
            return;
        }

        for (int i = 0; i < audiotracks.Length; i++)
        {
            SongButtonUI card = Instantiate(songCardPrefab, songCardContentParent);
            card.name = $"SongCard_{i}_{GetTrackTitle(i)}";
            card.Initialize(
                i,
                GetTrackTitle(i),
                GetTrackLength(i),
                songToggleGroup,
                PlayTrackByIndex,
                SetSongEnabled
            );

            spawnedSongCards.Add(card);
        }
    }

    private void RefreshSongCards()
    {
        if (spawnedSongCards.Count == 0)
        {
            return;
        }

        for (int i = 0; i < spawnedSongCards.Count; i++)
        {
            SongButtonUI card = spawnedSongCards[i];
            if (card == null)
            {
                continue;
            }

            bool selected = i == currentTrackIndex;
            bool playing = selected && IsPlaying && !isTransitioning;
            bool enabled = !IsTrackDisabled(i);
            float currentTime = selected && radioAudioSource != null ? radioAudioSource.time : 0f;
            float duration = GetTrackLength(i);

            card.Refresh(selected, playing, enabled, currentTime, duration, requirePurchase && !isPurchased);
        }
    }

    private void SetSongEnabled(int index, bool enabled)
    {
        if (!IsTrackIndexValid(index))
        {
            return;
        }

        if (enabled)
        {
            disabledSongIndexes.Remove(index);
        }
        else
        {
            disabledSongIndexes.Add(index);
        }

        if (IsTrackDisabled(currentTrackIndex))
        {
            int next = FindNextEnabledTrackIndex(currentTrackIndex, 1);
            if (IsTrackIndexValid(next) && next != currentTrackIndex)
            {
                StartTransitionTo(next);
            }
            else
            {
                StopAudio();
            }
        }

        RefreshSongCards();
    }

    private void StartTransitionTo(int newIndex)
    {
        if (isTransitioning || !IsTrackIndexValid(newIndex) || IsTrackDisabled(newIndex))
        {
            return;
        }

        StartCoroutine(TransitionToTrack(newIndex));
    }

    private IEnumerator TransitionToTrack(int newIndex)
    {
        isTransitioning = true;
        bool shouldPlayAfterTransition = radioAudioSource == null || radioAudioSource.isPlaying || newIndex != currentTrackIndex;

        CacheAudioSource();

        if (radioAudioSource != null && radioAudioSource.isPlaying)
        {
            yield return StartCoroutine(FadeOut(radioAudioSource, transitionSeconds));
        }

        currentTrackIndex = newIndex;
        UpdateTrack(currentTrackIndex, keepCurrentTime: false);

        if (shouldPlayAfterTransition && radioAudioSource != null)
        {
            DisableAmbientLoopIfPossible();
            yield return StartCoroutine(FadeIn(radioAudioSource, transitionSeconds));
            EnsurePlaybackMonitorRunning();
        }

        isTransitioning = false;
        RefreshSongCards();
        RefreshNowPlayingUI();
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
                PlayUnpurchasedFallback();
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
            bool endedNaturally = clipLength > 0f && time >= clipLength - endEpsilon;

            if (endedNaturally && !radioAudioSource.loop)
            {
                int next = FindNextEnabledTrackIndex(currentTrackIndex, 1);
                if (next != currentTrackIndex)
                {
                    StartTransitionTo(next);

                    while (isTransitioning)
                    {
                        yield return null;
                    }
                }
            }

            yield return null;
        }
    }

    private void UpdateTrack(int index, bool keepCurrentTime)
    {
        if (!IsTrackIndexValid(index))
        {
            return;
        }

        CacheAudioSource();

        AudioClip clip = GetTrackClip(index);
        if (radioAudioSource == null || clip == null)
        {
            return;
        }

        float previousTime = keepCurrentTime && radioAudioSource.clip == clip ? radioAudioSource.time : 0f;
        bool wasPlaying = radioAudioSource.isPlaying;

        radioAudioSource.clip = clip;

        if (previousTime > 0f && previousTime < clip.length)
        {
            radioAudioSource.time = previousTime;
        }

        if (wasPlaying && !radioAudioSource.isPlaying)
        {
            radioAudioSource.Play();
        }

        RefreshNowPlayingUI();
    }

    private void RefreshNowPlayingUI()
    {
        string title = IsTrackIndexValid(currentTrackIndex) ? GetTrackTitle(currentTrackIndex) : "No Track";
        float currentTime = radioAudioSource != null ? radioAudioSource.time : 0f;
        float duration = IsTrackIndexValid(currentTrackIndex) ? GetTrackLength(currentTrackIndex) : 0f;
        float fillAmount = duration > 0f ? Mathf.Clamp01(currentTime / duration) : 0f;

        if (trackTitleText != null)
        {
            trackTitleText.SetText(title);
        }

        if (nowPlayingText != null)
        {
            nowPlayingText.SetText(IsPlaying ? $"Now Playing: {title}" : $"Selected: {title}");
        }

        if (nowPlayingProgressFill != null)
        {
            nowPlayingProgressFill.fillAmount = fillAmount;
        }

        if (nowPlayingTimerText != null)
        {
            nowPlayingTimerText.SetText($"{FormatTime(currentTime)} / {FormatTime(duration)}");
        }

        if (playPauseButtonText != null)
        {
            playPauseButtonText.SetText(IsPlaying ? "Pause" : "Play");
        }
    }

    private void PlayUnpurchasedFallback()
    {
        CacheAudioSource();

        if (radioAudioSource == null || noAudiotracks == null || noAudiotracks.trackAudioClip == null)
        {
            return;
        }

        radioAudioSource.clip = noAudiotracks.trackAudioClip;

        if (!radioAudioSource.isPlaying)
        {
            radioAudioSource.volume = 1f;
            radioAudioSource.Play();
        }
    }

    private void EnsurePlaybackMonitorRunning()
    {
        if (playbackMonitorCoroutine != null)
        {
            return;
        }

        playbackMonitorCoroutine = StartCoroutine(PlaybackMonitor());
    }

    private void CacheAudioSource()
    {
        if (radioAudioSource == null)
        {
            radioAudioSource = GetComponent<AudioSource>();
        }
    }

    private bool CanUsePurchasedRadio()
    {
        return !requirePurchase || isPurchased;
    }

    private void DisableAmbientLoopIfPossible()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.DisableAmbientLooping(true);
        }
    }

    private bool IsTrackIndexValid(int index)
    {
        return audiotracks != null && index >= 0 && index < audiotracks.Length && GetTrackClip(index) != null;
    }

    private AudioClip GetTrackClip(int index)
    {
        if (audiotracks == null || index < 0 || index >= audiotracks.Length || audiotracks[index] == null)
        {
            return null;
        }

        return audiotracks[index].trackAudioClip;
    }

    private string GetTrackTitle(int index)
    {
        AudioClip clip = GetTrackClip(index);
        return clip != null ? clip.name : $"Track {index + 1}";
    }

    private float GetTrackLength(int index)
    {
        AudioClip clip = GetTrackClip(index);
        return clip != null ? clip.length : 0f;
    }

    private bool IsTrackDisabled(int index)
    {
        return disabledSongIndexes.Contains(index);
    }

    private int FindNextEnabledTrackIndex(int startIndex, int direction)
    {
        if (audiotracks == null || audiotracks.Length == 0)
        {
            return -1;
        }

        direction = direction >= 0 ? 1 : -1;
        int count = audiotracks.Length;

        for (int step = 1; step <= count; step++)
        {
            int candidate = (startIndex + (step * direction)) % count;
            if (candidate < 0)
            {
                candidate += count;
            }

            if (IsTrackIndexValid(candidate) && !IsTrackDisabled(candidate))
            {
                return candidate;
            }
        }

        return startIndex;
    }

    public IEnumerator FadeOut(AudioSource audioSource, float fadeTime)
    {
        if (audioSource == null)
        {
            yield break;
        }

        fadeTime = Mathf.Max(0.01f, fadeTime);
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(timer / fadeTime));
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    public IEnumerator FadeIn(AudioSource audioSource, float fadeTime)
    {
        if (audioSource == null)
        {
            yield break;
        }

        fadeTime = Mathf.Max(0.01f, fadeTime);
        float targetVolume = 1f;
        float timer = 0f;

        audioSource.volume = 0f;
        audioSource.Play();

        while (timer < fadeTime)
        {
            timer += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, Mathf.Clamp01(timer / fadeTime));
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    public static string FormatTime(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
        {
            seconds = 0f;
        }

        int wholeSeconds = Mathf.FloorToInt(seconds);
        int minutes = wholeSeconds / 60;
        int remainingSeconds = wholeSeconds % 60;
        return $"{minutes}:{remainingSeconds:00}";
    }
}
