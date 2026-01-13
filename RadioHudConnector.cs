csharp Assets/RadioHudConnector.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple connector that wires a HUD radio button to the Upgrades radio purchase panel (preferred)
/// or to the Radio panel if Upgrades isn't present. Also exposes an optional song-count label so
/// you can see how many songs are currently assigned to the Radio component in the inspector.
/// 
/// Usage:
/// - Attach this component to the HUD radio Button GameObject (or assign the Button field).
/// - Assign the `Upgrades` and/or `Radio` references in the inspector (the script will auto-find them
///   if left null).
/// - Optional: assign a `TMP_Text` to `songCountText` to show number of configured songs.
/// 
/// This keeps UI wiring in one place and makes the Radio's `RadioSongs` array editable in the Radio
/// component (no extra work required to change songs in the editor).
/// </summary>
public class RadioHudConnector : MonoBehaviour
{
    [Header("References (optional - will try to auto-find)")]
    public Button hudButton;
    public Upgrades upgrades;
    public Radio radio;

    [Header("Optional UI")]
    [Tooltip("Shows how many songs are configured on the Radio (editor/runtime).")]
    public TMP_Text songCountText;

    private void Awake()
    {
        // auto-find if null
        if (upgrades == null) upgrades = FindFirstObjectByType<Upgrades>();
        if (radio == null) radio = FindFirstObjectByType<Radio>();
        if (hudButton == null) hudButton = GetComponent<Button>();

        if (hudButton == null)
        {
            Debug.LogWarning("[RadioHudConnector] hudButton not assigned and no Button component found on the GameObject.");
            return;
        }

        // Clear existing listeners and wire to appropriate handler
        hudButton.onClick.RemoveAllListeners();

        if (upgrades != null)
        {
            // Prefer opening the Upgrades radio purchase/description panel so it shows price/Buy button like soap/glove.
            hudButton.onClick.AddListener(() => upgrades.OpenRadioMenu());

            // Make the Upgrades instance aware of this HUD button (so Upgrades can enable/disable it)
            // It's safe to assign even if Upgrades has already assigned its own reference.
            upgrades.radioOpenButton = hudButton;
        }
        else if (radio != null)
        {
            // Fallback: open the Radio panel directly (will only open if radio unlocked)
            hudButton.onClick.AddListener(() => radio.OpenPanel());
        }
        else
        {
            Debug.LogWarning("[RadioHudConnector] Neither Upgrades nor Radio found. Assign one in the inspector.");
        }
    }

    private void Start()
    {
        RefreshSongCountLabel();
    }

    private void OnValidate()
    {
        // keep label up to date in editor when fields change
        RefreshSongCountLabel();
    }

    private void RefreshSongCountLabel()
    {
        if (songCountText == null) return;
        if (radio == null) radio = FindFirstObjectByType<Radio>();

        int count = 0;
        if (radio != null && radio.RadioSongs != null)
            count = radio.RadioSongs.Length;

        songCountText.text = $"Songs: {count}";
    }
}