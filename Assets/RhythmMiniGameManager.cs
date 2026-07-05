using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RhythmMiniGameToggle : MonoBehaviour
{
    [Header("References")]
    public GameObject rhythmMiniGame;
    public GameObject mainGameUI;

    [Header("Minigame Audio")]
    public AudioSource minigameAudioSource;

    // Keep track of which main UI children we hid so we can restore them on close.
    private List<GameObject> hiddenMainUIChildren = new List<GameObject>();

    private bool isActive = false;
    private bool waitingForStart = false;

    void Update()
    {
        // If minigame is active and waiting for user to start it
        if (isActive && waitingForStart)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartMinigameMusic();
            }
        }
    }

    // Legacy toggle kept for compatibility with existing wiring
    public void ToggleMiniGame()
    {
        if (!isActive) OpenMiniGame(waitForSpace: true);
        else CloseMiniGame();
    }

    public void OpenMiniGame(bool waitForSpace = true, bool showDifficulty = true)
    {
        if (isActive) return;

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
        if (rhythmMiniGame != null) rhythmMiniGame.SetActive(true);

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
}
