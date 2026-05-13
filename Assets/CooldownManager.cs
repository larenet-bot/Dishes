using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CooldownManager : MonoBehaviour
{
    [Serializable]
    public class MinigameCooldownEntry
    {
        [Header("Label")]
        public string minigameName = "Minigame";

        [Header("Button UI")]
        public Button minigameButton;
        public TMP_Text buttonText;

        [Tooltip("Optional. This should be an Image on or inside the button. Set its Image Type to Filled.")]
        public Image fillImage;

        [Header("Optional Timer Override")]
        public bool useCustomCooldownTime = false;
        public float customCooldownSeconds = 300f;

        [Header("Ready Text")]
        [Tooltip("If empty, this script restores whatever text the button had at startup.")]
        public string readyTextOverride = "";

        [HideInInspector] public string originalButtonText;
        [HideInInspector] public float remainingSeconds;
        [HideInInspector] public bool isCoolingDown;
    }

    [Header("Global Cooldown Settings")]
    [Tooltip("Default cooldown length for every minigame, unless an entry uses customCooldownSeconds.")]
    [SerializeField] private float defaultCooldownSeconds = 300f;

    [Tooltip("When true, all minigame buttons begin on cooldown when the scene starts.")]
    [SerializeField] private bool startOnCooldown = true;

    [Tooltip("Use this if cooldowns should keep counting while the game is paused with Time.timeScale = 0.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Button Display")]
    [SerializeField] private bool disableButtonDuringCooldown = true;

    [Tooltip("Example: 'Wait ' gives 'Wait 04:59'. Leave empty for only the timer.")]
    [SerializeField] private string cooldownTextPrefix = "";

    [Tooltip("True = 04:59. False = 299s.")]
    [SerializeField] private bool showMinutesAndSeconds = true;

    [Header("Fill Image")]
    [Tooltip("True means the fill starts empty and fills up as the cooldown finishes.")]
    [SerializeField] private bool fillFromEmptyToFull = true;

    [Tooltip("If true, the fill image hides when the cooldown ends.")]
    [SerializeField] private bool hideFillWhenReady = false;

    [Header("Minigames")]
    [SerializeField] private MinigameCooldownEntry[] minigames;

    private void Awake()
    {
        for (int i = 0; i < minigames.Length; i++)
        {
            MinigameCooldownEntry entry = minigames[i];

            if (entry == null)
                continue;

            if (entry.buttonText != null)
                entry.originalButtonText = entry.buttonText.text;

            SetupFillImage(entry);
        }
    }

    private void Start()
    {
        if (startOnCooldown)
        {
            StartCooldownAll();
        }
        else
        {
            for (int i = 0; i < minigames.Length; i++)
                FinishCooldown(i, instant: true);
        }
    }

    private void Update()
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        for (int i = 0; i < minigames.Length; i++)
        {
            MinigameCooldownEntry entry = minigames[i];

            if (entry == null || !entry.isCoolingDown)
                continue;

            entry.remainingSeconds -= deltaTime;

            if (entry.remainingSeconds <= 0f)
            {
                FinishCooldown(i);
                continue;
            }

            UpdateCooldownUI(entry);
        }
    }

    // Use this from a close/reward button OnClick.
    // In the Button OnClick inspector, choose CooldownManager -> StartCooldown(int).
    public void StartCooldown(int minigameIndex)
    {
        if (!IsValidIndex(minigameIndex))
            return;

        MinigameCooldownEntry entry = minigames[minigameIndex];

        float duration = GetCooldownDuration(entry);
        entry.remainingSeconds = duration;
        entry.isCoolingDown = true;

        if (entry.minigameButton != null && disableButtonDuringCooldown)
            entry.minigameButton.interactable = false;

        if (entry.fillImage != null)
            entry.fillImage.gameObject.SetActive(true);

        UpdateCooldownUI(entry);
    }

    // Handy if you only have one minigame for now.
    public void StartCooldownFirst()
    {
        StartCooldown(0);
    }

    // Handy for testing, or if every minigame should go on cooldown together.
    public void StartCooldownAll()
    {
        for (int i = 0; i < minigames.Length; i++)
            StartCooldown(i);
    }

    // Optional: lets you call cooldowns by name from Unity events.
    public void StartCooldownByName(string minigameName)
    {
        if (string.IsNullOrWhiteSpace(minigameName))
            return;

        for (int i = 0; i < minigames.Length; i++)
        {
            if (minigames[i] == null)
                continue;

            if (string.Equals(minigames[i].minigameName, minigameName, StringComparison.OrdinalIgnoreCase))
            {
                StartCooldown(i);
                return;
            }
        }
    }

    public bool IsOnCooldown(int minigameIndex)
    {
        if (!IsValidIndex(minigameIndex))
            return false;

        return minigames[minigameIndex].isCoolingDown;
    }

    private void FinishCooldown(int minigameIndex, bool instant = false)
    {
        if (!IsValidIndex(minigameIndex))
            return;

        MinigameCooldownEntry entry = minigames[minigameIndex];

        entry.remainingSeconds = 0f;
        entry.isCoolingDown = false;

        if (entry.minigameButton != null)
            entry.minigameButton.interactable = true;

        if (entry.buttonText != null)
        {
            if (!string.IsNullOrWhiteSpace(entry.readyTextOverride))
                entry.buttonText.text = entry.readyTextOverride;
            else
                entry.buttonText.text = entry.originalButtonText;
        }

        if (entry.fillImage != null)
        {
            entry.fillImage.fillAmount = fillFromEmptyToFull ? 1f : 0f;

            if (hideFillWhenReady)
                entry.fillImage.gameObject.SetActive(false);
        }

        if (!instant)
            Debug.Log($"[CooldownManager] {entry.minigameName} cooldown finished.");
    }

    private void UpdateCooldownUI(MinigameCooldownEntry entry)
    {
        float duration = GetCooldownDuration(entry);
        float remaining = Mathf.Max(0f, entry.remainingSeconds);

        if (entry.buttonText != null)
            entry.buttonText.text = cooldownTextPrefix + FormatTime(remaining);

        if (entry.fillImage != null)
        {
            float progress = duration <= 0f ? 1f : 1f - Mathf.Clamp01(remaining / duration);
            entry.fillImage.fillAmount = fillFromEmptyToFull ? progress : 1f - progress;
        }
    }

    private void SetupFillImage(MinigameCooldownEntry entry)
    {
        if (entry.fillImage == null)
            return;

        entry.fillImage.type = Image.Type.Filled;

        if (entry.isCoolingDown)
            return;

        entry.fillImage.fillAmount = fillFromEmptyToFull ? 1f : 0f;

        if (hideFillWhenReady)
            entry.fillImage.gameObject.SetActive(false);
    }

    private float GetCooldownDuration(MinigameCooldownEntry entry)
    {
        if (entry == null)
            return Mathf.Max(0.1f, defaultCooldownSeconds);

        if (entry.useCustomCooldownTime)
            return Mathf.Max(0.1f, entry.customCooldownSeconds);

        return Mathf.Max(0.1f, defaultCooldownSeconds);
    }

    private string FormatTime(float seconds)
    {
        seconds = Mathf.Ceil(seconds);

        if (!showMinutesAndSeconds)
            return $"{seconds:0}s";

        int totalSeconds = Mathf.CeilToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;

        return $"{minutes:00}:{secs:00}";
    }

    private bool IsValidIndex(int index)
    {
        if (minigames == null)
            return false;

        if (index < 0 || index >= minigames.Length)
        {
            Debug.LogWarning($"[CooldownManager] Invalid minigame index: {index}");
            return false;
        }

        return minigames[index] != null;
    }
}