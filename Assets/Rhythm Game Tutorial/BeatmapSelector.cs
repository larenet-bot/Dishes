using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class BeatmapSelector : MonoBehaviour
{
    [Header("UI Panel Root")]
    public GameObject beatmapPanel;


    [Header("References")]
    public NoteSpawner noteSpawner;
    public RhythmMiniGameToggle miniToggle;

    [Header("Beatmaps")]
    public BeatmapOption[] options;

    [Header("Shared audio (used when per-option audio is empty)")]
    public AudioClip sharedAudioClip;

    [Header("UI (optional)")]
    public Button[] difficultyButtons; // wire 0..N buttons in inspector
    public Button startButton;
    public TextMeshProUGUI selectedLabel;
    public bool startImmediately = false; // if true, Start will immediately schedule audio + spawn

    int selectedIndex = 0;

    void Start()
    {
        // auto-wire buttons if provided
        if (difficultyButtons != null && difficultyButtons.Length > 0)
        {
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                int idx = i;
                difficultyButtons[i].onClick.RemoveAllListeners();
                difficultyButtons[i].onClick.AddListener(() => Select(idx));
            }
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() => StartSelected(true));

        }

        UpdateUI();
        ApplySelectionToSpawner();
        if (beatmapPanel != null)
            beatmapPanel.SetActive(true);

    }

    public void Select(int index)
    {
        if (options == null || options.Length == 0) return;
        selectedIndex = Mathf.Clamp(index, 0, options.Length - 1);
        ApplySelectionToSpawner();
        UpdateUI();
    }

    public void StartSelected(bool immediate = false)
    {
        if (options == null || options.Length == 0) return;

        ApplySelectionToSpawner();

        // Hide the beatmap selector UI
        if (beatmapPanel != null)
            beatmapPanel.SetActive(false);

        // Open the mini-game UI
        if (miniToggle != null)
        {
            miniToggle.OpenMiniGame(waitForSpace: !immediate);
        }

        // If immediate start requested, schedule spawning now.
        if (immediate && noteSpawner != null)
        {
            noteSpawner.StartSpawning();
        }
    }

    private void ApplySelectionToSpawner()
    {
        if (options == null || options.Length == 0) return;
        var opt = options[selectedIndex];

        // choose per-option audio if assigned, otherwise use sharedAudioClip
        AudioClip clipToUse = (opt != null && opt.audioClip != null) ? opt.audioClip : sharedAudioClip;

        if (noteSpawner != null)
        {
            noteSpawner.chartFile = opt.chartFile;
            if (noteSpawner.musicSource != null)
                noteSpawner.musicSource.clip = clipToUse;
        }

        if (miniToggle != null && miniToggle.minigameAudioSource != null)
        {
            miniToggle.minigameAudioSource.clip = clipToUse;
        }
    }

    private void UpdateUI()
    {
        if (selectedLabel != null && options != null && options.Length > 0)
            selectedLabel.text = options[selectedIndex].label;

        if (difficultyButtons != null)
        {
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                if (difficultyButtons[i] == null) continue;
                var colors = difficultyButtons[i].colors;
                colors.normalColor = (i == selectedIndex) ? Color.green : Color.white;
                difficultyButtons[i].colors = colors;
            }
        }
    }

    private void AutoLoadFromResources(string folder = "Beatmaps")
    {
        var charts = Resources.LoadAll<TextAsset>(folder);
        var clips  = Resources.LoadAll<AudioClip>(folder);

        var list = new List<BeatmapOption>();
        foreach (var chart in charts)
        {
            var opt = new BeatmapOption { label = chart.name, chartFile = chart };
            opt.audioClip = System.Array.Find(clips, c => c != null && c.name == chart.name);
            list.Add(opt);
        }

        if (list.Count > 0)
            options = list.ToArray();
    }

    void Awake()
    {
        // call when you want automatic population (before Start wiring)
        if (options == null || options.Length == 0)
            AutoLoadFromResources();
    }
}
