using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small UI controller for MP3 player panel. Populates a scroll list of songs (from AudioManager.startingSongs),
/// allows selecting a song to immediately play, next/prev controls and simple toggles for loop/shuffle.
/// Also supports a simple queue and save/load of queue + looped song.
/// </summary>
public class Mp3PlayerUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public Transform songListParent;               // parent where song buttons will be instantiated
    public Button songButtonPrefab;                // prefab with a Button and a TMP_Text child for label
    public TMP_Text currentSongText;
    public TMP_Text queueText;                     // optional: show queued song names (comma separated)
    public Button closeButton;
    public Button prevButton;
    public Button nextButton;
    public Toggle loopToggle;
    public Toggle shuffleToggle;

    private AudioManager audioManager;
    private List<AudioClip> songs = new List<AudioClip>();
    private int currentIndex = -1;
    private readonly List<GameObject> spawnedButtons = new List<GameObject>();

    // queue contains references to AudioClips from 'songs' (keeps order)
    private readonly List<AudioClip> playQueue = new List<AudioClip>();

    // optional looped song (null = none); loopToggle controls whether looping is enabled
    private AudioClip loopedSong = null;

    // Coroutine watching playback to advance queue
    private Coroutine playbackWatcher;

    private void Awake()
    {
        audioManager = AudioManager.instance ?? FindFirstObjectByType<AudioManager>();

        if (closeButton != null) closeButton.onClick.RemoveAllListeners();
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (prevButton != null) prevButton.onClick.RemoveAllListeners();
        if (prevButton != null) prevButton.onClick.AddListener(PlayPrev);

        if (nextButton != null) nextButton.onClick.RemoveAllListeners();
        if (nextButton != null) nextButton.onClick.AddListener(PlayNext);

        if (loopToggle != null)
        {
            loopToggle.onValueChanged.RemoveAllListeners();
            loopToggle.onValueChanged.AddListener(val =>
            {
                // If a loopedSong exists, we interpret this as "loop that song".
                if (audioManager?.musicSource != null)
                {
                    audioManager.musicSource.loop = val && loopedSong != null;
                }
            });
        }

        if (shuffleToggle != null)
            shuffleToggle.onValueChanged.AddListener(val =>
            {
                if (audioManager != null)
                {
                    // Use avoidImmediateRepeat as a simple "shuffle-ish" option and enable autoPlayNext so it advances
                    audioManager.avoidImmediateRepeat = val;
                    audioManager.autoPlayNext = val || audioManager.autoPlayNext;
                }
            });

        // Hide panel initially if assigned
        if (panel != null && panel.activeSelf) panel.SetActive(false);
    }

    /// <summary>Open player panel and (re)populate the song list.</summary>
    public void Open()
    {
        if (panel == null) return;

        if (audioManager == null) audioManager = AudioManager.instance ?? FindFirstObjectByType<AudioManager>();

        // Try to auto-find songListParent if it's not assigned (common prefab names)
        if (songListParent == null && panel != null)
        {
            var candidate = panel.transform.Find("SongListContent") ?? panel.transform.Find("SongListScroll/SongListContent");
            if (candidate != null) songListParent = candidate;
        }

        PopulateSongList();

        // Sync toggles to audio manager's current state
        if (loopToggle != null && audioManager?.musicSource != null)
            loopToggle.isOn = audioManager.musicSource.loop;

        if (shuffleToggle != null && audioManager != null)
        {
            shuffleToggle.isOn = audioManager.avoidImmediateRepeat;
        }

        panel.SetActive(true);
    }

    public void Close()
    {
        if (panel == null) return;
        panel.SetActive(false);
    }

    private void PopulateSongList()
    {
        ClearSongButtons();

        songs.Clear();
        if (audioManager?.startingSongs != null && audioManager.startingSongs.Length > 0)
            songs.AddRange(audioManager.startingSongs);

        // If no parent or prefab, nothing to display but we still track songs
        if (songButtonPrefab == null || songListParent == null)
        {
            // Update queue/current text only
            UpdateCurrentSongText();
            UpdateQueueText();
            return;
        }

        // create a button for each clip
        for (int i = 0; i < songs.Count; i++)
        {
            var clip = songs[i];
            var go = Instantiate(songButtonPrefab.gameObject, songListParent);
            spawnedButtons.Add(go);

            

            var rootBtn = go.GetComponent<Button>();
            var label = go.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.SetText(string.IsNullOrEmpty(clip?.name) ? $"Song {i + 1}" : clip.name);

            // Fix label RectTransform so it keeps the intended padding / right-side buttons don't shrink the label unexpectedly
            if (label != null)
            {
                var labelRt = label.rectTransform;
                labelRt.anchorMin = new Vector2(0f, 0f);
                labelRt.anchorMax = new Vector2(0.78f, 1f);
                labelRt.offsetMin = new Vector2(8f, 4f);
                labelRt.offsetMax = new Vector2(-8f, -4f);
            }

            // Improve readability on TMP labels if present
            if (label != null)
            {
                try
                {
                    label.enableAutoSizing = false;
                    label.fontSize = 18;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.overflowMode = TextOverflowModes.Ellipsis;

                }
                catch (Exception) { /* ignore runtime differences */ }
            }

            // Add LayoutElement to cooperate with parent layout
            var layout = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            layout.minHeight = Mathf.Max(layout.minHeight, 28f);
            layout.flexibleWidth = 1f;

            // Avoid closure capture issue
            int localIndex = i;

            // Root button -> Play immediately
            if (rootBtn != null)
            {
                rootBtn.onClick.RemoveAllListeners();
                rootBtn.onClick.AddListener(() => PlayIndex(localIndex));
            }

            // Explicit "Queue" button (optional in prefab)
            var queueBtn = go.transform.Find("QueueButton")?.GetComponent<Button>();
            if (queueBtn != null)
            {
                queueBtn.onClick.RemoveAllListeners();
                queueBtn.onClick.AddListener(() => EnqueueSongIndex(localIndex));
            }

            // Explicit "Loop" button (optional in prefab) - sets this song as loop target
            var loopBtn = go.transform.Find("LoopButton")?.GetComponent<Button>();
            if (loopBtn != null)
            {
                loopBtn.onClick.RemoveAllListeners();
                loopBtn.onClick.AddListener(() =>
                {
                    SetLoopedSongByIndex(localIndex);
                    // if loop toggle is on, ensure source loops now
                    if (loopToggle != null && loopToggle.isOn && audioManager?.musicSource != null && audioManager.musicSource.clip == songs[localIndex])
                        audioManager.musicSource.loop = true;
                });
            }
        }

        // Force layout rebuild so sizes/auto-sizing are applied immediately
        if (songListParent is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        // Update current song indicator & queue text
        UpdateCurrentSongText();
        UpdateQueueText();
    }

    private void ClearSongButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                Destroy(spawnedButtons[i]);
        }
        spawnedButtons.Clear();
    }

    // Enqueue by index (from displayed list)
    public void EnqueueSongIndex(int index)
    {
        if (audioManager == null) audioManager = AudioManager.instance ?? FindFirstObjectByType<AudioManager>();
        if (index < 0 || index >= songs.Count) return;
        var clip = songs[index];
        if (clip == null) return;

        playQueue.Add(clip);
        UpdateQueueText();

        // auto-start playback if nothing is playing
        if (audioManager?.musicSource != null && !audioManager.musicSource.isPlaying)
        {
            PlayNextInQueue();
        }
    }

    // Play immediately and set currentIndex appropriately (does not alter queue)
    public void PlayIndex(int index)
    {
        if (audioManager == null) audioManager = AudioManager.instance ?? FindFirstObjectByType<AudioManager>();
        if (audioManager == null) return;
        if (index < 0 || index >= songs.Count) return;

        var clip = songs[index];
        if (clip == null) return;

        audioManager.PlayMusic(clip);
        currentIndex = index;
        UpdateCurrentSongText();

        // If not looping specifically, ensure playback watcher runs to advance queue
        StartPlaybackWatcher();
    }

    public void PlayClip(AudioClip clip)
    {
        if (audioManager == null) audioManager = AudioManager.instance ?? FindFirstObjectByType<AudioManager>();
        if (audioManager == null || clip == null) return;

        audioManager.PlayMusic(clip);
        currentIndex = songs.IndexOf(clip);
        UpdateCurrentSongText();

        StartPlaybackWatcher();
    }

    public void PlayNext()
    {
        // If queue has items prefer queue
        if (playQueue.Count > 0)
        {
            PlayNextInQueue();
            return;
        }

        if (songs == null || songs.Count == 0) return;
        if (currentIndex < 0) currentIndex = 0;
        else currentIndex = (currentIndex + 1) % songs.Count;
        PlayIndex(currentIndex);
    }

    public void PlayPrev()
    {
        if (songs == null || songs.Count == 0) return;
        if (currentIndex < 0) currentIndex = 0;
        else currentIndex = (currentIndex - 1 + songs.Count) % songs.Count;
        PlayIndex(currentIndex);
    }

    // Plays the next clip from queue (removes it). If queue empty, no-op.
    private void PlayNextInQueue()
    {
        if (playQueue.Count == 0) return;
        var next = playQueue[0];
        playQueue.RemoveAt(0);
        UpdateQueueText();

        // set loopedSong behavior: if loopedSong is set and equals next, maintain audioSource.loop depending on loopToggle
        if (audioManager == null) audioManager = AudioManager.instance ?? FindFirstObjectByType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.PlayMusic(next);
            // set audioSource.loop if loopToggle is on and this is the looped song
            if (audioManager.musicSource != null)
                audioManager.musicSource.loop = loopToggle != null && loopToggle.isOn && loopedSong != null && loopedSong == next;
        }

        currentIndex = songs.IndexOf(next);
        UpdateCurrentSongText();
        StartPlaybackWatcher();
    }

    private void UpdateCurrentSongText()
    {
        if (currentSongText == null) return;
        if (audioManager?.musicSource != null && audioManager.musicSource.clip != null)
            currentSongText.SetText(audioManager.musicSource.clip.name);
        else
            currentSongText.SetText("<none>");
    }

    private void UpdateQueueText()
    {
        if (queueText == null) return;
        if (playQueue.Count == 0)
        {
            queueText.SetText("<empty>");
            return;
        }

        // show up to first 8 items
        int show = Mathf.Min(playQueue.Count, 8);
        var names = new List<string>(show);
        for (int i = 0; i < show; i++)
            names.Add(playQueue[i]?.name ?? $"Song {i + 1}");
        string summary = string.Join(", ", names);
        if (playQueue.Count > show)
            summary += $" (+{playQueue.Count - show})";

        queueText.SetText(summary);
    }

    private void StartPlaybackWatcher()
    {
        if (playbackWatcher != null)
        {
            StopCoroutine(playbackWatcher);
            playbackWatcher = null;
        }

        playbackWatcher = StartCoroutine(PlaybackWatcherCoroutine());
    }

    private IEnumerator PlaybackWatcherCoroutine()
    {
        if (audioManager == null) audioManager = AudioManager.instance ?? FindFirstObjectByType<AudioManager>();
        var src = audioManager?.musicSource;
        if (src == null) yield break;

        // Wait while playing; when it stops (and not looping) advance to next queued song
        while (src != null)
        {
            // if loop is enabled and this clip is the loopedSong, then just yield until loop state changes
            if (src.isPlaying)
            {
                yield return null;
                continue;
            }

            // Not playing: if loop toggle is on and the loopedSong is set and matches the last clip, ensure source.loop remains (handled elsewhere)
            bool performedNext = false;

            // If loopToggle is on and loopedSong is set and matches the last played clip, re-play it (keep looping)
            if (loopToggle != null && loopToggle.isOn && loopedSong != null && src.clip == loopedSong)
            {
                src.Play();
                performedNext = true;
            }
            else if (playQueue.Count > 0)
            {
                PlayNextInQueue();
                performedNext = true;
                // PlayNextInQueue will restart watcher by calling StartPlaybackWatcher
                break;
            }

            if (!performedNext)
                break;

            yield return null;
        }

        playbackWatcher = null;
    }

    private void OnDestroy()
    {
        ClearSongButtons();
    }

    // --- Save / Load helpers for SaveManager ---

    // Provide a serializable representation of queue + loop
    public void GetSaveState(out List<string> outQueue, out string outLoopedSongName, out bool outLoopEnabled)
    {
        outQueue = new List<string>(playQueue.Count);
        for (int i = 0; i < playQueue.Count; i++)
            outQueue.Add(playQueue[i] != null ? playQueue[i].name : string.Empty);

        outLoopedSongName = loopedSong != null ? loopedSong.name : null;
        outLoopEnabled = (loopToggle != null) ? loopToggle.isOn : false;
    }

    // Apply saved queue and loop info. Matches songs by name (first match).
    public void ApplySaveState(List<string> savedQueue, string savedLoopedSongName, bool savedLoopEnabled)
    {
        if (audioManager == null) audioManager = AudioManager.instance ?? FindFirstObjectByType<AudioManager>();

        // Rebuild playQueue from saved names
        playQueue.Clear();
        if (savedQueue != null && savedQueue.Count > 0 && audioManager?.startingSongs != null)
        {
            for (int i = 0; i < savedQueue.Count; i++)
            {
                var name = savedQueue[i];
                if (string.IsNullOrEmpty(name)) continue;
                // find first matching clip by name
                AudioClip found = null;
                foreach (var c in audioManager.startingSongs)
                {
                    if (c != null && c.name == name)
                    {
                        found = c;
                        break;
                    }
                }
                if (found != null) playQueue.Add(found);
            }
        }

        // Apply looped song by name (first match)
        loopedSong = null;
        if (!string.IsNullOrEmpty(savedLoopedSongName) && audioManager?.startingSongs != null)
        {
            foreach (var c in audioManager.startingSongs)
            {
                if (c != null && c.name == savedLoopedSongName)
                {
                    loopedSong = c;
                    break;
                }
            }
        }

        // Apply toggle
        if (loopToggle != null)
        {
            loopToggle.isOn = savedLoopEnabled;
            if (audioManager?.musicSource != null)
            {
                audioManager.musicSource.loop = savedLoopEnabled && loopedSong != null && audioManager.musicSource.clip == loopedSong;
            }
        }

        UpdateQueueText();
        UpdateCurrentSongText();
    }

    // Allow outside callers (UI) to set which song should be looped
    public void SetLoopedSongByIndex(int index)
    {
        if (audioManager == null) audioManager = AudioManager.instance ?? FindFirstObjectByType<AudioManager>();
        if (index < 0 || index >= songs.Count) { loopedSong = null; return; }
        loopedSong = songs[index];
        if (loopToggle != null && audioManager?.musicSource != null)
            audioManager.musicSource.loop = loopToggle.isOn && audioManager.musicSource.clip == loopedSong;
    }

    // Optional: clear queue
    public void ClearQueue()
    {
        playQueue.Clear();
        UpdateQueueText();
    }
}