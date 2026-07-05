using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongButtonUI : MonoBehaviour
{
    [Header("Core UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text playButtonText;
    [SerializeField] private Image progressFill;
    [SerializeField] private GameObject playingRoot;

    [Header("Controls")]
    public Button playButton;

    [Tooltip("Optional radio-style toggle used only to show/select the current song. This can be grouped.")]
    [SerializeField] private Toggle selectionToggle;

    [Tooltip("Optional on/off toggle used to include or remove this song from skip/auto-play rotation.")]
    public Toggle enableToggle;

    public Image background;

    [Header("Optional Colors")]
    [SerializeField] private Color enabledColor = new Color(0.18f, 0.18f, 0.22f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
    [SerializeField] private Color selectedColor = new Color(0.25f, 0.25f, 0.32f, 1f);

    private int songIndex = -1;
    private float songDuration;
    private Action<int> playCallback;
    private Action<int, bool> enabledCallback;
    private bool eventsWired;

    public bool IsEnabled => enableToggle != null ? enableToggle.isOn : true;

    private void Reset()
    {
        CacheChildReferences();
    }

    private void Awake()
    {
        CacheChildReferences();
    }

    private void CacheChildReferences()
    {
        if (playButton == null)
        {
            playButton = GetComponent<Button>();
        }

        if (background == null)
        {
            background = GetComponent<Image>();
        }

        if (selectionToggle == null)
        {
            Transform selectionChild = transform.Find("SelectionToggle");
            if (selectionChild != null)
            {
                selectionToggle = selectionChild.GetComponent<Toggle>();
            }
        }

        if (enableToggle == null)
        {
            Transform toggleChild = transform.Find("EnableToggle");
            if (toggleChild != null)
            {
                enableToggle = toggleChild.GetComponent<Toggle>();
            }
        }

        if (titleText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0)
            {
                titleText = texts[0];
            }
        }

        if (playButtonText == null && playButton != null)
        {
            playButtonText = playButton.GetComponentInChildren<TMP_Text>(true);
        }
    }

    public void Initialize(
        int index,
        string title,
        float duration,
        ToggleGroup toggleGroup,
        Action<int> onPlay,
        Action<int, bool> onEnabledChanged)
    {
        songIndex = index;
        songDuration = Mathf.Max(0f, duration);
        playCallback = onPlay;
        enabledCallback = onEnabledChanged;

        CacheChildReferences();

        if (titleText != null)
        {
            titleText.SetText(string.IsNullOrWhiteSpace(title) ? $"Track {index + 1}" : title);
        }

        if (selectionToggle != null)
        {
            selectionToggle.group = toggleGroup;
        }

        WireEvents();
        Refresh(selected: false, playing: false, enabled: IsEnabled, currentTime: 0f, duration: songDuration, locked: false);
    }

    private void WireEvents()
    {
        if (eventsWired)
        {
            return;
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (selectionToggle != null)
        {
            selectionToggle.onValueChanged.RemoveAllListeners();
            selectionToggle.onValueChanged.AddListener(OnSelectionToggleChanged);
        }

        if (enableToggle != null)
        {
            enableToggle.onValueChanged.RemoveAllListeners();
            enableToggle.onValueChanged.AddListener(OnEnabledToggleChanged);
        }

        eventsWired = true;
    }

    public void Refresh(bool selected, bool playing, bool enabled, float currentTime, float duration, bool locked)
    {
        songDuration = Mathf.Max(0f, duration);

        if (selectionToggle != null)
        {
            selectionToggle.SetIsOnWithoutNotify(selected);
            selectionToggle.interactable = !locked && enabled;
        }

        if (enableToggle != null)
        {
            enableToggle.SetIsOnWithoutNotify(enabled);
            enableToggle.interactable = !locked;
        }

        if (playButton != null)
        {
            playButton.interactable = !locked && enabled;
        }

        if (playButtonText != null)
        {
            playButtonText.SetText(playing ? "Playing" : "Play");
        }

        if (playingRoot != null)
        {
            playingRoot.SetActive(playing);
        }

        float visibleTime = selected ? currentTime : 0f;
        float fill = songDuration > 0f ? Mathf.Clamp01(visibleTime / songDuration) : 0f;

        if (progressFill != null)
        {
            progressFill.fillAmount = fill;
        }

        if (timerText != null)
        {
            timerText.SetText($"{RadioCOntroller.FormatTime(visibleTime)} / {RadioCOntroller.FormatTime(songDuration)}");
        }

        SetEnabledVisual(enabled, selected);
    }

    public void SetEnabledVisual(bool enabled)
    {
        SetEnabledVisual(enabled, false);
    }

    private void SetEnabledVisual(bool enabled, bool selected)
    {
        if (background == null)
        {
            return;
        }

        if (!enabled)
        {
            background.color = disabledColor;
            return;
        }

        background.color = selected ? selectedColor : enabledColor;
    }

    private void OnPlayClicked()
    {
        if (songIndex < 0)
        {
            return;
        }

        playCallback?.Invoke(songIndex);
    }

    private void OnSelectionToggleChanged(bool isOn)
    {
        if (!isOn || songIndex < 0)
        {
            return;
        }

        playCallback?.Invoke(songIndex);
    }

    private void OnEnabledToggleChanged(bool isOn)
    {
        SetEnabledVisual(isOn);

        if (songIndex >= 0)
        {
            enabledCallback?.Invoke(songIndex, isOn);
        }
    }
}
