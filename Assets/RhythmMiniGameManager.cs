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
        if (rhythmMiniGame == null)
        {
            foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name == "RythmMiniGame" && candidate.scene.IsValid())
                {
                    rhythmMiniGame = candidate;
                    break;
                }
            }
        }

        if (openMinigameButton == null)
            openMinigameButton = gameObject;
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
        // Keep enforcing lock/unlock UI state until unlocked to prevent other systems from enabling the control prematurely.
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

        var btn = openMinigameButton.GetComponent<Button>();
        if (btn != null)
            btn.interactable = false;

        var cg = openMinigameButton.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = openMinigameButton.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

       
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
        long dishes = ScoreManager.Instance != null
            ? ScoreManager.Instance.GetTotalDishes()
            : 0L;

        Debug.Log(
            $"[RhythmMiniGameToggle] Click received. " +
            $"Unlocked={isUnlocked}, Active={isActive}, Dishes={dishes}"
        );

        // Recheck the actual dish requirement when clicked.
        if (dishes >= requiredDishesToUnlock)
        {
            isUnlocked = true;
            ApplyLockedState(false);
        }
        else
        {
            isUnlocked = false;
            Debug.LogWarning(
                $"[RhythmMiniGameToggle] Need {requiredDishesToUnlock} dishes; currently have {dishes}."
            );
            return;
        }

        if (isActive)
            CloseMiniGame();
        else
            OpenMiniGame(waitForSpace: true);
    }


    public void OpenMiniGame(bool waitForSpace = true, bool showDifficulty = true)
    {
        Debug.Log($"[RhythmMiniGameToggle] OpenMiniGame called. " +
                  $"Unlocked={isUnlocked}, Active={isActive}, " +
                  $"RhythmUI={(rhythmMiniGame != null ? rhythmMiniGame.name : "NULL")}, " +
                  $"MainUI={(mainGameUI != null ? mainGameUI.name : "NULL")}");

        if (!isUnlocked)
        {
            Debug.LogWarning("[RhythmMiniGameToggle] Cannot open: minigame is locked.");
            return;
        }

        if (rhythmMiniGame == null)
        {
            Debug.LogError("[RhythmMiniGameToggle] rhythmMiniGame is not assigned!");
            return;
        }

        rhythmMiniGame.SetActive(true);
        Debug.Log($"[UI DEBUG] Minigame object: {rhythmMiniGame.name}");
        Debug.Log($"[UI DEBUG] Minigame activeSelf: {rhythmMiniGame.activeSelf}");
        Debug.Log($"[UI DEBUG] Minigame activeInHierarchy: {rhythmMiniGame.activeInHierarchy}");

        Debug.Log($"[UI DEBUG] Main UI reference: " +
            (mainGameUI != null ? mainGameUI.name : "NULL"));

        if (mainGameUI != null)
        {
            Debug.Log($"[UI DEBUG] Main UI active: {mainGameUI.activeSelf}");

            foreach (Transform child in mainGameUI.transform)
            {
                Debug.Log($"[UI DEBUG] Main UI child: {child.name}, " +
                          $"activeSelf: {child.gameObject.activeSelf}");
            }
        }

        foreach (Transform child in rhythmMiniGame.transform)
        {
            Debug.Log($"[UI DEBUG] Minigame child: {child.name}, " +
                      $"activeSelf: {child.gameObject.activeSelf}");
        }


        RestoreScaledTime();
        DishPileManager.Instance?.CompleteActiveCompletedDishAnimations();

        hiddenMainUIChildren.Clear();

        // Hide the main game's UI children.
        if (mainGameUI != null)
        {
            foreach (Transform child in mainGameUI.transform)
            {
                // Do not hide the rhythm minigame or a parent containing it.
                if (child == rhythmMiniGame.transform ||
                    rhythmMiniGame.transform.IsChildOf(child))
                {
                    continue;
                }

                // Keep the employee UI visible if required by your setup.
                var emp = FindAnyObjectByType<EmployeeManager>();
                Transform empRoot = emp != null ? emp.transform : null;

                if (empRoot != null &&
                    (child == empRoot || empRoot.IsChildOf(child)))
                {
                    continue;
                }

                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    hiddenMainUIChildren.Add(child.gameObject);

                    Debug.Log($"[RhythmMiniGameToggle] Hid main UI: {child.name}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[RhythmMiniGameToggle] mainGameUI is not assigned!");
        }

        Canvas rhythmCanvas = rhythmMiniGame.GetComponentInChildren<Canvas>(true);

        Debug.Log(
            $"[RhythmMiniGameToggle] " +
            $"Object={rhythmMiniGame.name}, " +
            $"ActiveSelf={rhythmMiniGame.activeSelf}, " +
            $"ActiveInHierarchy={rhythmMiniGame.activeInHierarchy}, " +
            $"Canvas={(rhythmCanvas != null ? rhythmCanvas.name : "NONE")}, " +
            $"CanvasEnabled={(rhythmCanvas != null && rhythmCanvas.enabled)}"
        );

        if (showDifficulty)
        {
            var selector = FindAnyObjectByType<BeatmapSelector>();

            if (selector != null && selector.beatmapPanel != null)
                selector.beatmapPanel.SetActive(true);
        }

        if (AudioManager.instance != null)
            AudioManager.instance.MuteMainMusic(true);

        isActive = true;
        waitingForStart = waitForSpace;

        Debug.Log("[RhythmMiniGameToggle] Minigame opened successfully.");
    }

    // Close the mini-game UI and stop minigame audio
    public void CloseMiniGame()
    {
        

        RestoreScaledTime();
        DishPileManager.Instance?.CompleteActiveCompletedDishAnimations();

        isActive = false;
        if (rhythmMiniGame != null) rhythmMiniGame.SetActive(false);

        // Restore the main UI root.
        if (mainGameUI != null)
        {
            mainGameUI.SetActive(true);

            // Restore children that were recorded when opening the minigame.
            foreach (Transform child in mainGameUI.transform)
            {
                child.gameObject.SetActive(true);
            }

            Debug.Log(
                $"[RhythmMiniGameToggle] Restored main UI: {mainGameUI.name}, " +
                $"Children: {mainGameUI.transform.childCount}"
            );
        }

        // Clear the saved list.
        hiddenMainUIChildren.Clear();

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
