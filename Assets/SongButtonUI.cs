using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongButtonUI : MonoBehaviour
{
    [Header("Controls")]
    public Button playButton;
    public Toggle enableToggle;

    [Header("Display")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI playButtonText;
    public Image background;

    [Header("Progress Bar")]
    [Tooltip("Assign the Image inside this radio card that uses Image Type = Filled, Fill Method = Horizontal.")]
    public Image progressFill;

    private RadioCOntroller radioController;
    private int trackIndex = -1;

    public bool IsEnabled => enableToggle != null ? enableToggle.isOn : true;

    public void Initialize(RadioCOntroller controller, int index, string title)
    {
        AutoWireReferences();

        radioController = controller;
        trackIndex = index;

        if (titleText != null)
            titleText.text = title;

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (enableToggle != null)
        {
            enableToggle.onValueChanged.RemoveListener(SetEnabledVisual);
            enableToggle.onValueChanged.AddListener(SetEnabledVisual);
            SetEnabledVisual(enableToggle.isOn);
        }

        SetProgressFill(0f);
        SetButtonLabel("Play");
    }

    public void Refresh(bool isCurrentTrack, bool isPlaying, float progress, bool isTransitioning)
    {
        bool usable = IsEnabled && !isTransitioning;

        if (playButton != null)
            playButton.interactable = usable;

        if (isCurrentTrack && isPlaying)
        {
            SetButtonLabel("Playing");
        }
        else if (isCurrentTrack)
        {
            SetButtonLabel("Resume");
        }
        else
        {
            SetButtonLabel("Play");
        }

        SetProgressFill(isCurrentTrack ? progress : 0f);
        SetEnabledVisual(IsEnabled);
    }

    public void SetEnabledVisual(bool enabled)
    {
        if (background == null) return;

        background.color = enabled
            ? new Color(0.18f, 0.18f, 0.22f, 1f)
            : new Color(0.1f, 0.1f, 0.1f, 0.5f);
    }

    private void SetProgressFill(float amount)
    {
        if (progressFill == null)
            return;

        progressFill.fillAmount = Mathf.Clamp01(amount);
    }

    private void SetButtonLabel(string label)
    {
        if (playButtonText != null)
            playButtonText.text = label;
    }

    private void OnPlayClicked()
    {
        if (!IsEnabled)
            return;

        if (radioController != null && trackIndex >= 0)
            radioController.PlayTrackFromList(trackIndex);
    }

    private void Reset()
    {
        AutoWireReferences();
        SetProgressFill(0f);
    }

    private void Awake()
    {
        AutoWireReferences();
        SetProgressFill(0f);

        if (enableToggle != null)
        {
            enableToggle.onValueChanged.RemoveListener(SetEnabledVisual);
            enableToggle.onValueChanged.AddListener(SetEnabledVisual);
            SetEnabledVisual(enableToggle.isOn);
        }
    }

    private void AutoWireReferences()
    {
        if (playButton == null)
            playButton = GetComponentInChildren<Button>(true);

        if (enableToggle == null)
        {
            Transform t = FindChildByNameContains(transform, "EnableToggle");
            if (t != null) enableToggle = t.GetComponent<Toggle>();
        }

        if (titleText == null)
        {
            Transform t = FindChildByNameContains(transform, "TitleText");
            if (t != null) titleText = t.GetComponent<TextMeshProUGUI>();
        }

        if (playButtonText == null && playButton != null)
            playButtonText = playButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (background == null)
            background = GetComponent<Image>();

        if (progressFill == null)
        {
            Transform t = FindChildByNameContains(transform, "ProgressFill");
            if (t == null) t = FindChildByNameContains(transform, "ProgressBar");
            if (t == null) t = FindChildByNameContains(transform, "Progress");
            if (t != null) progressFill = t.GetComponent<Image>();
        }
    }

    private Transform FindChildByNameContains(Transform root, string text)
    {
        if (root == null || string.IsNullOrWhiteSpace(text))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name.IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return child;

            Transform found = FindChildByNameContains(child, text);
            if (found != null)
                return found;
        }

        return null;
    }
}
