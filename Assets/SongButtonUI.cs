//csharp Assets\SongButtonUI.cs
using UnityEngine;
using UnityEngine.UI;

public class SongButtonUI : MonoBehaviour
{
    public Button playButton;
    public Toggle enableToggle;
    public Image background;

    public bool IsEnabled => enableToggle != null ? enableToggle.isOn : true;

    public void SetEnabledVisual(bool enabled)
    {
        if (background == null) return;
        background.color = enabled
            ? new Color(0.18f, 0.18f, 0.22f, 1f)
            : new Color(0.1f, 0.1f, 0.1f, 0.5f);
    }

    private void Reset()
    {
        // Try to auto-wire common children when component is added in editor
        if (playButton == null) playButton = GetComponent<Button>();
        if (enableToggle == null)
        {
            var t = transform.Find("EnableToggle");
            if (t != null) enableToggle = t.GetComponent<Toggle>();
        }
        if (background == null) background = GetComponent<Image>();
    }

    private void Awake()
    {
        if (enableToggle != null)
        {
            enableToggle.onValueChanged.AddListener(v => SetEnabledVisual(v));
            SetEnabledVisual(enableToggle.isOn);
        }
    }
}