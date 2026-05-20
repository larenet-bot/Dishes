using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generic minigame cooldown UI binder.
/// Attach this to an empty GameObject named CooldownManager.
/// It stores cooldown end times in KitchenBusinessProgress, so cooldowns keep counting while scenes are unloaded.
/// </summary>
public class CooldownManager : MonoBehaviour
{
    [Serializable]
    public class MinigameCooldownEntry
    {
        [Header("Identity")]
        [Tooltip("Unique id for this minigame inside this kitchen. Use the same id in KitchenBusinessMenu cards.")]
        public string minigameId = "minigame_1";

        [Header("Button UI")]
        public Button minigameButton;
        public TMP_Text buttonText;

        [Tooltip("Optional. Drag the radial fill Image from the button here.")]
        public Image fillImage;

        [Header("Optional Timer Override")]
        public bool useCustomCooldownTime = false;
        public float customCooldownSeconds = 300f;

        [Header("Ready Text")]
        [Tooltip("If empty, the script restores whatever text the button had at startup.")]
        public string readyTextOverride = "";

        [Header("Offscreen Notification")]
        [Tooltip("Target checked when cooldown ends. Leave empty to use the minigame button itself.")]
        public Transform notificationTarget;

        [HideInInspector] public bool wasCoolingDownLastFrame;

        [HideInInspector] public string originalButtonText;
    }

    [Header("Kitchen Identity")]
    [Tooltip("Used if autoResolveKitchenId cannot find LoanManager or KitchenIdentity.")]
    [SerializeField] private string kitchenId = "kitchen_1";

    [SerializeField] private bool autoResolveKitchenId = true;
    [SerializeField] private LoanManager loanManager;
    [SerializeField] private KitchenIdentity kitchenIdentity;

    [Header("Global Cooldown Settings")]
    [Tooltip("Default cooldown length for every minigame, unless an entry uses customCooldownSeconds.")]
    [SerializeField] private float defaultCooldownSeconds = 300f;

    [Tooltip("Starts each configured minigame cooldown once per app session. It will not restart just because this scene reloads.")]
    [SerializeField] private bool startOnCooldownOnInitialAppStartup = true;

    [Header("Button Display")]
    [SerializeField] private bool disableButtonDuringCooldown = true;

    [Tooltip("Example: 'Wait ' gives 'Wait 04:59'. Leave empty for only the timer.")]
    [SerializeField] private string cooldownTextPrefix = "";

    [Tooltip("True = 04:59. False = 299s.")]
    [SerializeField] private bool showMinutesAndSeconds = true;

    [Header("Fill Image")]
    [Tooltip("If true, the script forces the image to radial filled mode.")]
    [SerializeField] private bool forceRadialFill = true;

    [Tooltip("True means the fill starts empty and fills up as the cooldown finishes.")]
    [SerializeField] private bool fillFromEmptyToFull = true;

    [Tooltip("If true, the fill image hides when the cooldown ends.")]
    [SerializeField] private bool hideFillWhenReady = false;

    [Header("Minigames")]
    [SerializeField] private MinigameCooldownEntry[] minigames;

    private void Awake()
    {
        ResolveKitchenId();
        CacheOriginalButtonText();
        SetupFillImages();
    }

    private void Start()
    {
        ResolveKitchenId();

        if (startOnCooldownOnInitialAppStartup)
        {
            SeedInitialCooldownsOnceThisSession();
        }

        RefreshAllUI();
    }

    private void Update()
    {
        RefreshAllUI();
    }

    /// <summary>
    /// Use this from the reward screen close button OnClick.
    /// First minigame entry is 0, second is 1, etc.
    /// </summary>
    public void StartCooldown(int minigameIndex)
    {
        if (!IsValidIndex(minigameIndex))
        {
            return;
        }

        ResolveKitchenId();

        MinigameCooldownEntry entry = minigames[minigameIndex];
        KitchenBusinessProgress.StartMinigameCooldown(
            kitchenId,
            entry.minigameId,
            GetCooldownDuration(entry)
        );

        RefreshEntryUI(entry);
    }

    /// <summary>
    /// Convenience method for a scene with one minigame.
    /// </summary>
    public void StartCooldownFirst()
    {
        StartCooldown(0);
    }

    /// <summary>
    /// Optional method if you prefer passing a minigame id from Unity events.
    /// </summary>
    public void StartCooldownById(string minigameId)
    {
        if (string.IsNullOrWhiteSpace(minigameId) || minigames == null)
        {
            return;
        }

        for (int i = 0; i < minigames.Length; i++)
        {
            if (minigames[i] == null)
            {
                continue;
            }

            if (string.Equals(minigames[i].minigameId, minigameId, StringComparison.OrdinalIgnoreCase))
            {
                StartCooldown(i);
                return;
            }
        }
    }

    public void StartCooldownAll()
    {
        if (minigames == null)
        {
            return;
        }

        for (int i = 0; i < minigames.Length; i++)
        {
            StartCooldown(i);
        }
    }

    public bool IsOnCooldown(int minigameIndex)
    {
        if (!IsValidIndex(minigameIndex))
        {
            return false;
        }

        ResolveKitchenId();
        return KitchenBusinessProgress.IsMinigameOnCooldown(kitchenId, minigames[minigameIndex].minigameId);
    }

    private void ResolveKitchenId()
    {
        if (!autoResolveKitchenId)
        {
            return;
        }

        if (loanManager == null)
        {
            loanManager = FindFirstObjectByType<LoanManager>();
        }

        if (loanManager != null && !string.IsNullOrWhiteSpace(loanManager.GetKitchenId()))
        {
            kitchenId = loanManager.GetKitchenId();
            return;
        }

        if (kitchenIdentity == null)
        {
            kitchenIdentity = FindFirstObjectByType<KitchenIdentity>();
        }

        if (kitchenIdentity != null && !string.IsNullOrWhiteSpace(kitchenIdentity.KitchenId))
        {
            kitchenId = kitchenIdentity.KitchenId;
        }
    }

    private void CacheOriginalButtonText()
    {
        if (minigames == null)
        {
            return;
        }

        for (int i = 0; i < minigames.Length; i++)
        {
            MinigameCooldownEntry entry = minigames[i];

            if (entry == null || entry.buttonText == null)
            {
                continue;
            }

            entry.originalButtonText = entry.buttonText.text;
        }
    }

    private void SetupFillImages()
    {
        if (minigames == null)
        {
            return;
        }

        for (int i = 0; i < minigames.Length; i++)
        {
            MinigameCooldownEntry entry = minigames[i];

            if (entry == null || entry.fillImage == null)
            {
                continue;
            }

            if (forceRadialFill)
            {
                entry.fillImage.type = Image.Type.Filled;
                entry.fillImage.fillMethod = Image.FillMethod.Radial360;
            }
        }
    }

    private void SeedInitialCooldownsOnceThisSession()
    {
        if (minigames == null)
        {
            return;
        }

        for (int i = 0; i < minigames.Length; i++)
        {
            MinigameCooldownEntry entry = minigames[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.minigameId))
            {
                continue;
            }

            KitchenBusinessProgress.EnsureInitialMinigameCooldownStartedThisSession(
                kitchenId,
                entry.minigameId,
                GetCooldownDuration(entry)
            );
        }
    }

    private void RefreshAllUI()
    {
        if (minigames == null)
        {
            return;
        }

        for (int i = 0; i < minigames.Length; i++)
        {
            RefreshEntryUI(minigames[i]);
        }
    }

    private void RefreshEntryUI(MinigameCooldownEntry entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.minigameId))
        {
            return;
        }

        bool isCoolingDown = KitchenBusinessProgress.IsMinigameOnCooldown(kitchenId, entry.minigameId);
        float remainingSeconds = KitchenBusinessProgress.GetMinigameCooldownRemainingSeconds(kitchenId, entry.minigameId);
        float progress01 = KitchenBusinessProgress.GetMinigameCooldownProgress01(kitchenId, entry.minigameId);
        bool becameReadyThisFrame = entry.wasCoolingDownLastFrame && !isCoolingDown;

        if (becameReadyThisFrame)
        {
            Transform target = entry.notificationTarget;

            if (target == null && entry.minigameButton != null)
                target = entry.minigameButton.transform;

            if (OffscreenNotificationManager.Instance != null)
                OffscreenNotificationManager.Instance.NotifyUntilVisible(target);
        }

        entry.wasCoolingDownLastFrame = isCoolingDown;

        if (entry.minigameButton != null)
        {
            entry.minigameButton.interactable = !isCoolingDown || !disableButtonDuringCooldown;
        }

        if (entry.buttonText != null)
        {
            entry.buttonText.text = isCoolingDown
                ? cooldownTextPrefix + FormatTime(remainingSeconds)
                : GetReadyText(entry);
        }

        if (entry.fillImage != null)
        {
            if (forceRadialFill)
            {
                entry.fillImage.type = Image.Type.Filled;
                entry.fillImage.fillMethod = Image.FillMethod.Radial360;
            }

            entry.fillImage.fillAmount = fillFromEmptyToFull ? progress01 : 1f - progress01;

            if (hideFillWhenReady)
            {
                entry.fillImage.gameObject.SetActive(isCoolingDown);
            }
            else
            {
                entry.fillImage.gameObject.SetActive(true);
            }
        }
    }

    private float GetCooldownDuration(MinigameCooldownEntry entry)
    {
        if (entry != null && entry.useCustomCooldownTime)
        {
            return Mathf.Max(0.1f, entry.customCooldownSeconds);
        }

        return Mathf.Max(0.1f, defaultCooldownSeconds);
    }

    private string GetReadyText(MinigameCooldownEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.readyTextOverride))
        {
            return entry.readyTextOverride;
        }

        return entry.originalButtonText;
    }

    private string FormatTime(float seconds)
    {
        seconds = Mathf.Ceil(seconds);

        if (!showMinutesAndSeconds)
        {
            return $"{seconds:0}s";
        }

        int totalSeconds = Mathf.CeilToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;

        return $"{minutes:00}:{secs:00}";
    }

    private bool IsValidIndex(int index)
    {
        if (minigames == null)
        {
            return false;
        }

        if (index < 0 || index >= minigames.Length)
        {
            Debug.LogWarning($"[CooldownManager] Invalid minigame index: {index}");
            return false;
        }

        if (minigames[index] == null)
        {
            Debug.LogWarning($"[CooldownManager] Minigame entry {index} is null.");
            return false;
        }

        return true;
    }
}
