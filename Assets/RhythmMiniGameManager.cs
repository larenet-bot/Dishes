using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class RhythmMiniGameToggle : MonoBehaviour
{
    [Header("References")]
    public GameObject rhythmMiniGame;
    public GameObject mainGameUI;

    [Header("Minigame Audio")]
    public AudioSource minigameAudioSource;

    [Header("Unlock")]
    [Tooltip("Number of cleaned dishes required before the minigame UI appears.")]
    public long requiredDishesToUnlock = 100;

    [Tooltip("Optional UI GameObject (e.g. the button) that opens the minigame. If null, this GameObject will be used.")]
    public GameObject openMinigameButton;

    // Keep track of which main UI children we hid so we can restore them on close.
    private List<GameObject> hiddenMainUIChildren = new List<GameObject>();

    private bool isActive = false;
    private bool waitingForStart = false;
    private bool isUnlocked = false;

    // Cached Button + click action to ensure the button reliably invokes ToggleMiniGame.
    private Button cachedButton;
    private UnityAction cachedClickAction;

    void Awake()
    {
        if (openMinigameButton == null)
            openMinigameButton = this.gameObject;
    }

    void Start()
    {
        // Initialize visibility and wiring
        InitializeButton();
        UpdateUnlockState(); // apply initial state immediately
    }

    void OnDestroy()
    {
        // Clean up listener to avoid dangling references in editor/play mode
        if (cachedButton != null && cachedClickAction != null)
            cachedButton.onClick.RemoveListener(cachedClickAction);
    }

    private void InitializeButton()
    {
        if (openMinigameButton == null) return;

        cachedButton = openMinigameButton.GetComponent<Button>();
        cachedClickAction = ToggleMiniGame;

        if (cachedButton != null)
        {
            // Remove then add to avoid duplicate listeners
            cachedButton.onClick.RemoveListener(cachedClickAction);
            cachedButton.onClick.AddListener(cachedClickAction);
        }

        // Ensure CanvasGroup exists so we can control visibility/raycast blocking reliably
        var cg = openMinigameButton.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = openMinigameButton.AddComponent<CanvasGroup>();

        // Start hidden if locked
        long totalDishes = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalDishes() : 0L;
        bool startUnlocked = totalDishes >= requiredDishesToUnlock;
        ApplyLockedState(!startUnlocked);
    }

    void Update()
    {
        Debug.Log("[RhythmMiniGameToggle] UPDATE RUNNING");

        // Keep enforcing lock/unlock UI state until unlocked
        UpdateUnlockState();

        if (isActive && waitingForStart)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartMinigameMusic();
            }
        }
    }

    private void UpdateUnlockState()
    {
        long totalDishes = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalDishes() : 0L;
        Debug.Log(
        $"[RhythmMiniGameToggle] Unlock check | " +
        $"Dishes = {totalDishes} | " +
        $"Required = {requiredDishesToUnlock} | " +
        $"isUnlocked = {isUnlocked}"
    );
        if (totalDishes >= requiredDishesToUnlock)
        {
            if (!isUnlocked)
            {
                isUnlocked = true;
                ApplyLockedState(false);
                Debug.Log($"[RhythmMiniGameToggle] Minigame unlocked (dishes: {totalDishes}).");
            }
            return;
        }

        // locked state
        if (isUnlocked)
            isUnlocked = false;

        ApplyLockedState();
    }

    private void ApplyLockedState()
    {
        if (openMinigameButton == null) return;

        // Make sure button is not interactable and not blocking raycasts
        var btn = openMinigameButton.GetComponent<Button>();
        if (btn != null && btn.interactable)
            btn.interactable = false;

        var cg = openMinigameButton.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            // Add a CanvasGroup to control visibility & raycasts without relying solely on SetActive.
            cg = openMinigameButton.AddComponent<CanvasGroup>();
        }

        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // Also try to deactivate the GameObject to make it invisible (in case no CanvasGroup exists).
        if (openMinigameButton.activeSelf)
            openMinigameButton.SetActive(false);
    }

    private void ApplyUnlockedState()
    {
        if (openMinigameButton == null) return;

        // Ensure visible
        if (!openMinigameButton.activeSelf)
            openMinigameButton.SetActive(true);

        var btn = openMinigameButton.GetComponent<Button>();
        if (btn != null && !btn.interactable)
            btn.interactable = true;

        var cg = openMinigameButton.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }
    }

    // Legacy toggle kept for compatibility with existing wiring
    public void ToggleMiniGame()
    {
        Debug.Log(
            $"[RhythmMiniGameToggle] ToggleMiniGame CALLED | " +
            $"isUnlocked = {isUnlocked} | " +
            $"isActive = {isActive}"
        );

        if (!isUnlocked)
        {
            Debug.LogWarning(
                "[RhythmMiniGameToggle] Toggle ignored because isUnlocked is FALSE."
            );
            return;
        }

        if (!isActive)
            OpenMiniGame(waitForSpace: true);
        else
            CloseMiniGame();
    }

    public void OpenMiniGame(bool waitForSpace = true, bool showDifficulty = true)
    {
        if (isActive) return;

        // Safety guard in case this is invoked while still locked.
        if (!isUnlocked)
        {
            Debug.Log($"[RhythmMiniGameToggle] Attempted to open minigame while locked. Requires {requiredDishesToUnlock} cleaned dishes.");
            return;
        }

        // Optionally show the difficulty/beatmap selector if present in scene.
        if (showDifficulty)
        {
            var selector = FindAnyObjectByType<BeatmapSelector>();
            if (selector != null && selector.beatmapPanel != null)
                selector.beatmapPanel.SetActive(true);
        }

        RestoreScaledTime();
        DishPileManager.Instance?.CompleteActiveCompletedDishAnimations();

        isActive = true;

        Debug.Log($"[RhythmMiniGameToggle] Opening minigame. rhythmMiniGame = {rhythmMiniGame}");

        if (rhythmMiniGame != null)
        {
            rhythmMiniGame.SetActive(true);

            Debug.Log(
                $"[RhythmMiniGameToggle] rhythmMiniGame active after SetActive: " +
                $"{rhythmMiniGame.activeSelf}"
            );
        }
        else
        {
            Debug.LogError(
                "[RhythmMiniGameToggle] rhythmMiniGame is NULL!"
            );
        }
        // Instead of disabling the entire mainGameUI (which also disables employee UI),
        // hide only top-level children under mainGameUI except those that contain an EmployeeManager.
        hiddenMainUIChildren.Clear();
        if (mainGameUI != null)
        {
            var emp = FindAnyObjectByType<EmployeeManager>();
            Transform empRoot = emp != null ? emp.transform : null;

            foreach (Transform child in mainGameUI.transform)
            {
                // If the employee manager is a descendant of this child, skip hiding it
                if (empRoot != null && (child == empRoot || empRoot.IsChildOf(child)))
                    continue;

                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    hiddenMainUIChildren.Add(child.gameObject);
                }
            }
        }

        if (AudioManager.instance != null)
            AudioManager.instance.MuteMainMusic(true);

        waitingForStart = waitForSpace;
    }

    // Close the mini-game UI and stop minigame audio
    public void CloseMiniGame()
    {
        if (!isActive) return;

        RestoreScaledTime();
        DishPileManager.Instance?.CompleteActiveCompletedDishAnimations();

        isActive = false;
        if (rhythmMiniGame != null) rhythmMiniGame.SetActive(false);

        // Restore previously hidden main UI children so the UI returns to its prior state.
        if (mainGameUI != null)
        {
            foreach (var go in hiddenMainUIChildren)
            {
                if (go != null)
                    go.SetActive(true);
            }
            hiddenMainUIChildren.Clear();
        }

        if (minigameAudioSource != null)
            minigameAudioSource.Stop();

        if (AudioManager.instance != null)
        {
            AudioManager.instance.MuteMainMusic(false);
            AudioManager.instance.RestoreMainMusic();
        }

        waitingForStart = false;
    }

    private void RestoreScaledTime()
    {
        Time.timeScale = 1f;
    }

    private void StartMinigameMusic()
    {
        if (minigameAudioSource != null)
        {
            minigameAudioSource.time = 0f;
            minigameAudioSource.Play();
        }

        waitingForStart = false;
    }

    // Add this overload to support the bool parameter
    private void ApplyLockedState(bool locked)
    {
        if (locked)
            ApplyLockedState();
        else
            ApplyUnlockedState();
    }

    }
