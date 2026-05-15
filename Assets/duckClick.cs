using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Animator), typeof(AudioSource))]
public class DuckClick : MonoBehaviour
{
    private Animator animator;
    private AudioSource audioSource;

    // Audio lists
    public AudioClip[] defaultClips;
    public AudioClip[] altClips;

    // UI: duck menu panel and content containers (assign in inspector)
    [Header("Duck Menu UI")]
    public GameObject duckMenuPanel;           // Root panel for duck menu (set inactive by default)
    public Transform dishesContent;            // Parent for dish entries
    public Transform achievementsContent;      // Parent for achievement entries
    public GameObject dishItemPrefab;          // Prefab for a dish entry (Image + TMP_Text + optional LockedOverlay child)
    public GameObject[] achievementPrefabs;    // Assign your achievement prefabs here (they will be instantiated into achievementsContent)

    private void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (duckMenuPanel != null)
            duckMenuPanel.SetActive(false);
    }

    void OnMouseDown()
    {
        StartCoroutine(PressRoutine());
        ToggleDuckMenu();
    }

    IEnumerator PressRoutine()
    {
        animator.SetBool("Pressed", true);

        // Read the preference at click-time so toggling in settings takes effect immediately
        bool useAlt = PlayerPrefs.GetInt("DuckAlternate", 0) == 1;

        AudioClip clipToPlay = null;

        AudioClip[] sourceArray = useAlt ? altClips : defaultClips;

        if (sourceArray != null && sourceArray.Length > 0)
        {
            clipToPlay = sourceArray[Random.Range(0, sourceArray.Length)];
        }
        else
        {
            // fallback to the AudioSource's default clip if lists are empty
            clipToPlay = audioSource.clip;
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }

        yield return new WaitForSeconds(0.2f); // match animation length

        animator.SetBool("Pressed", false);
    }

    // AnimationEvent receiver required by some animation clips
    public void Pressed()
    {
        ResetPressed();
    }

    public void ResetPressed()
    {
        animator.SetBool("Pressed", false);
    }

    // Toggle the duck menu panel and populate when opening
    public void ToggleDuckMenu()
    {
        if (duckMenuPanel == null)
        {
            Debug.LogWarning("[DuckClick] duckMenuPanel not assigned.");
            return;
        }

        bool willOpen = !duckMenuPanel.activeSelf;
        duckMenuPanel.SetActive(willOpen);

        if (willOpen)
            PopulateDuckMenu();
    }

    // Populate dishes & achievements quadrants
    private void PopulateDuckMenu()
    {
        // Clear existing children
        if (dishesContent != null)
        {
            for (int i = dishesContent.childCount - 1; i >= 0; i--)
            {
                Destroy(dishesContent.GetChild(i).gameObject);
            }
        }

        if (achievementsContent != null)
        {
            for (int i = achievementsContent.childCount - 1; i >= 0; i--)
            {
                Destroy(achievementsContent.GetChild(i).gameObject);
            }
        }

        // Populate dishes
        var score = ScoreManager.Instance;
        if (score == null)
        {
            Debug.LogWarning("[DuckClick] ScoreManager.Instance not found. Cannot populate dishes.");
            return;
        }

        long totalDishes = score.GetTotalDishesCount();
        var allDishes = score.allDishes;

        if (allDishes != null && dishItemPrefab != null && dishesContent != null)
        {
            foreach (var dish in allDishes)
            {
                if (dish == null) continue;

                GameObject item = Instantiate(dishItemPrefab, dishesContent, false);
                item.transform.localScale = Vector3.one;

                // Set name text (supports TMP or legacy Text)
                var tmp = item.GetComponentInChildren<TMP_Text>();
                if (tmp != null) tmp.text = string.IsNullOrEmpty(dish.displayName) ? dish.name : dish.displayName;
                else
                {
                    var legacy = item.GetComponentInChildren<Text>();
                    if (legacy != null) legacy.text = string.IsNullOrEmpty(dish.displayName) ? dish.name : dish.displayName;
                }

                // Set icon if prefab has an Image and DishData has stage sprite(s). Use last sprite (clean) if present.
                var icon = item.GetComponentInChildren<Image>();
                if (icon != null && dish.stageSprites != null && dish.stageSprites.Length > 0)
                {
                    icon.sprite = dish.stageSprites[dish.stageSprites.Length - 1];
                    icon.preserveAspect = true;
                }

                // Locked overlay convention: child named "LockedOverlay" will be enabled when locked.
                Transform lockedOverlay = item.transform.Find("LockedOverlay");
                bool unlocked = totalDishes >= dish.unlockAtDishCount;
                if (lockedOverlay != null)
                    lockedOverlay.gameObject.SetActive(!unlocked);

                // Optionally style locked entries (reduce alpha) if no overlay is present
                if (lockedOverlay == null && icon != null)
                {
                    icon.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                }
            }
        }
        else
        {
            if (dishItemPrefab == null) Debug.LogWarning("[DuckClick] dishItemPrefab not assigned.");
            if (dishesContent == null) Debug.LogWarning("[DuckClick] dishesContent not assigned.");
        }

        // Populate achievements quadrant by instantiating assigned prefabs
        if (achievementPrefabs != null && achievementPrefabs.Length > 0 && achievementsContent != null)
        {
            foreach (var prefab in achievementPrefabs)
            {
                if (prefab == null) continue;
                Instantiate(prefab, achievementsContent, false).transform.localScale = Vector3.one;
            }
        }
        else
        {
            // No prefabs assigned — leave empty or the user can wire runtime-created achievements
            if (achievementsContent == null)
                Debug.LogWarning("[DuckClick] achievementsContent not assigned.");
        }
    }
}