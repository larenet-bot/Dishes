
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small UI controller for MP3 player panel. Populates a scroll list of songs (from AudioManager.startingSongs),
/// allows selecting a song to immediately play, next/prev controls and simple toggles for loop/shuffle.
/// Designed to be lightweight and to call into AudioManager for playback.
/// 
public class Mp3PlayerUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public Transform songListParent;               // parent where song buttons will be instantiated
    public Button songButtonPrefab;                // prefab with a Button and a TMP_Text child for label
    public TMP_Text currentSongText;
    public Button closeButton;
    public Button prevButton;
    public Button nextButton;
    public Toggle loopToggle;
    public Toggle shuffleToggle;

    private AudioManager audioManager;
    private List<AudioClip> songs = new List<AudioClip>();
    private int currentIndex = -1;
    private readonly List<GameObject> spawnedButtons = new List<GameObject>();

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
            loopToggle.onValueChanged.AddListener(val =>
            {
                if (audioManager?.musicSource != null)
                    audioManager.musicSource.loop = val;
            });

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

        // create a button for each clip
        for (int i = 0; i < songs.Count; i++)
        {
            var clip = songs[i];
            if (songButtonPrefab == null || songListParent == null)
                continue;

            var go = Instantiate(songButtonPrefab.gameObject, songListParent);
            spawnedButtons.Add(go);
            var btn = go.GetComponent<Button>();
            var label = go.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.SetText(string.IsNullOrEmpty(clip?.name) ? $"Song {i + 1}" : clip.name);

            // Avoid closure capture issue
            int localIndex = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => PlayIndex(localIndex));
        }

        // Update current song indicator
        UpdateCurrentSongText();
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
    }

    public void PlayClip(AudioClip clip)
    {
        if (audioManager == null) audioManager = AudioManager.instance ?? FindFirstObjectByType<AudioManager>();
        if (audioManager == null || clip == null) return;

        audioManager.PlayMusic(clip);
        // update index if clip belongs to list
        currentIndex = songs.IndexOf(clip);
        UpdateCurrentSongText();
    }

    public void PlayNext()
    {
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

    private void UpdateCurrentSongText()
    {
        if (currentSongText == null) return;
        if (audioManager?.musicSource != null && audioManager.musicSource.clip != null)
            currentSongText.SetText(audioManager.musicSource.clip.name);
        else
            currentSongText.SetText("<none>");
    }

    private void OnDestroy()
    {
        ClearSongButtons();
    }
}