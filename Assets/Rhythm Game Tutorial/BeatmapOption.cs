using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class BeatmapOption
{
    public string label;
    public TextAsset chartFile;
    public AudioClip audioClip;
    public Sprite icon; // optional for button visuals
}

public class BeatmapSelector : MonoBehaviour
{
    [Header("References")]
    public NoteSpawner noteSpawner;
    public RhythmMiniGameToggle miniToggle;

    [Header("Beatmaps")]
    public BeatmapOption[] options;

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
            startButton.onClick.AddListener(() => StartSelected(startImmediately));
        }

        UpdateUI();
        ApplySelectionToSpawner();
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

        // Open the mini-game UI. If immediate == true we won't wait for space to start.
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

        if (noteSpawner != null)
        {
            noteSpawner.chartFile = opt.chartFile;
            if (noteSpawner.musicSource != null)
                noteSpawner.musicSource.clip = opt.audioClip;
        }

        if (miniToggle != null && miniToggle.minigameAudioSource != null)
        {
            miniToggle.minigameAudioSource.clip = opt.audioClip;
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
}