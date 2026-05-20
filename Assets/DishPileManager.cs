using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages dirty dish pile visuals, clean dish pile visuals,
/// and the completed-dish movement animation.
///
/// Put this on an empty GameObject named DishPileManager.
///
/// Dirty pile images:
/// Element 0 = plate pile only
/// Element 1 = bowl + plate pile
/// Element 2 = sauce pan + bowl + plate pile
///
/// Clean pile images:
/// Same order, but the next image does not appear when the dish unlocks.
/// It appears only after that newly unlocked dish type has been cleaned.
/// </summary>
public class DishPileManager : MonoBehaviour
{
    public static DishPileManager Instance { get; private set; }

    [Header("Dirty Dish Pile Images")]
    [Tooltip("Assign combined dirty pile Images in the same order as DishSpawner.allDishes.")]
    [SerializeField] private Image[] dirtyPileImagesByUnlockOrder;

    [Header("Clean Dish Pile Images")]
    [Tooltip("Assign combined clean pile Images in the same order as DishSpawner.allDishes.")]
    [SerializeField] private Image[] cleanPileImagesByCleanOrder;

    [Tooltip("If true, clean pile element 0 is visible at game start. If false, no clean pile appears until the first dish is cleaned.")]
    [SerializeField] private bool showFirstCleanPileBeforeAnyDishCleaned = true;

    [Tooltip("Saves the clean pile's highest cleaned dish index separately from the main save.")]
    [SerializeField] private bool saveCleanPileProgressWithPlayerPrefs = true;

    [Tooltip("Used for PlayerPrefs clean pile save. Keep this different per kitchen if needed.")]
    [SerializeField] private string kitchenId = "kitchen_1";

    [Tooltip("If true, tries to pull the kitchen id from LoanManager when one exists.")]
    [SerializeField] private bool autoUseLoanManagerKitchenId = true;

    [Tooltip("Base PlayerPrefs key for clean pile progress.")]
    [SerializeField] private string cleanPileProgressPrefsKey = "CLEAN_DISH_PILE_INDEX";

    [Header("References")]
    [Tooltip("Optional. If left empty, this manager uses ScoreManager.Instance.dishSpawner.")]
    [SerializeField] private DishSpawner dishSpawner;

    [Tooltip("Optional. If left empty, this manager uses ScoreManager.Instance.")]
    [SerializeField] private ScoreManager scoreManager;

    [Header("Completed Dish Motion")]
    [Tooltip("How fast the completed dish moves left in UI anchored-position units per second.")]
    [SerializeField] private float moveSpeed = 800f;

    [Tooltip("How far left the completed dish moves in UI anchored-position units.")]
    [SerializeField] private float moveDistance = 350f;

    [Tooltip("Final scale multiplier applied to the completed dish copy. 0.35 means it ends at 35% of its starting scale.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float shrinkMultiplier = 0.35f;

    private long lastCheckedTotalDishes = long.MinValue;
    private int lastActiveDirtyPileIndex = -2;

    private int highestCleanedDishIndex = -1;
    private int lastActiveCleanPileIndex = -2;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ResolveReferences();
        LoadCleanPileProgress();

        RefreshDirtyPileImages(force: true);
        RefreshCleanPileImages(force: true);
    }

    private void Update()
    {
        RefreshDirtyPileImages(force: false);
    }

    public void AnimateCompletedDish(DishVisual completedDishVisual)
    {
        if (completedDishVisual == null || completedDishVisual.dishImage == null)
        {
            return;
        }

        int completedDishIndex = GetDishIndex(completedDishVisual.GetDishData());

        Image sourceImage = completedDishVisual.dishImage;

        if (sourceImage.sprite == null)
        {
            return;
        }

        RectTransform sourceRect = sourceImage.rectTransform;
        Transform parent = sourceRect.parent;

        if (parent == null)
        {
            return;
        }

        GameObject cleanDishCopy = new GameObject(
            "CompletedDish_ToCleanPile",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(LayoutElement)
        );

        cleanDishCopy.transform.SetParent(parent, false);

        RectTransform copyRect = cleanDishCopy.GetComponent<RectTransform>();
        CopyRectTransform(sourceRect, copyRect);

        Image copyImage = cleanDishCopy.GetComponent<Image>();
        CopyImage(sourceImage, copyImage);

        CanvasGroup canvasGroup = cleanDishCopy.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        LayoutElement layoutElement = cleanDishCopy.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        copyImage.raycastTarget = false;

        // Put the completed-copy directly behind the real dish image.
        // The real dish image can immediately become the next dirty dish and stay clickable.
        copyRect.SetSiblingIndex(sourceRect.GetSiblingIndex());

        StartCoroutine(AnimateToCleanPile(copyRect, completedDishIndex));
    }

    public void RefreshDirtyPileImagesNow()
    {
        RefreshDirtyPileImages(force: true);
    }

    public void RefreshCleanPileImagesNow()
    {
        RefreshCleanPileImages(force: true);
    }

    public void ResetCleanPileProgress()
    {
        highestCleanedDishIndex = showFirstCleanPileBeforeAnyDishCleaned ? 0 : -1;
        lastActiveCleanPileIndex = -2;

        if (saveCleanPileProgressWithPlayerPrefs)
        {
            PlayerPrefs.DeleteKey(GetCleanPilePrefsKey());
            PlayerPrefs.Save();
        }

        RefreshCleanPileImages(force: true);
    }

    private void ResolveReferences()
    {
        if (scoreManager == null)
        {
            scoreManager = ScoreManager.Instance;
        }

        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }

        if (dishSpawner == null && scoreManager != null)
        {
            dishSpawner = scoreManager.dishSpawner;
        }

        if (dishSpawner == null)
        {
            dishSpawner = FindFirstObjectByType<DishSpawner>();
        }
    }

    private void RefreshDirtyPileImages(bool force)
    {
        if (dirtyPileImagesByUnlockOrder == null || dirtyPileImagesByUnlockOrder.Length == 0)
        {
            return;
        }

        ResolveReferences();

        long totalDishes = scoreManager != null ? scoreManager.GetTotalDishes() : 0L;

        if (!force && totalDishes == lastCheckedTotalDishes)
        {
            return;
        }

        lastCheckedTotalDishes = totalDishes;

        int activePileIndex = GetHighestUnlockedDishIndex(totalDishes);

        if (!force && activePileIndex == lastActiveDirtyPileIndex)
        {
            return;
        }

        lastActiveDirtyPileIndex = activePileIndex;

        for (int i = 0; i < dirtyPileImagesByUnlockOrder.Length; i++)
        {
            Image pileImage = dirtyPileImagesByUnlockOrder[i];

            if (pileImage == null)
            {
                continue;
            }

            bool shouldShow = i == activePileIndex;

            if (pileImage.gameObject.activeSelf != shouldShow)
            {
                pileImage.gameObject.SetActive(shouldShow);
            }
        }
    }

    private void RefreshCleanPileImages(bool force)
    {
        if (cleanPileImagesByCleanOrder == null || cleanPileImagesByCleanOrder.Length == 0)
        {
            return;
        }

        int activePileIndex = ClampCleanPileIndex(highestCleanedDishIndex);

        if (!force && activePileIndex == lastActiveCleanPileIndex)
        {
            return;
        }

        lastActiveCleanPileIndex = activePileIndex;

        for (int i = 0; i < cleanPileImagesByCleanOrder.Length; i++)
        {
            Image pileImage = cleanPileImagesByCleanOrder[i];

            if (pileImage == null)
            {
                continue;
            }

            bool shouldShow = i == activePileIndex;

            if (pileImage.gameObject.activeSelf != shouldShow)
            {
                pileImage.gameObject.SetActive(shouldShow);
            }
        }
    }

    private int GetHighestUnlockedDishIndex(long totalDishes)
    {
        if (dishSpawner == null || dishSpawner.allDishes == null || dishSpawner.allDishes.Count == 0)
        {
            return dirtyPileImagesByUnlockOrder.Length > 0 ? 0 : -1;
        }

        int highestUnlockedIndex = -1;
        int maxIndexToCheck = Mathf.Min(dirtyPileImagesByUnlockOrder.Length, dishSpawner.allDishes.Count);

        for (int i = 0; i < maxIndexToCheck; i++)
        {
            DishData dish = dishSpawner.allDishes[i];

            if (dish == null)
            {
                continue;
            }

            if (totalDishes >= dish.unlockAtDishCount)
            {
                highestUnlockedIndex = i;
            }
        }

        if (highestUnlockedIndex < 0 && dirtyPileImagesByUnlockOrder.Length > 0)
        {
            highestUnlockedIndex = 0;
        }

        return highestUnlockedIndex;
    }

    private int GetDishIndex(DishData dishData)
    {
        ResolveReferences();

        if (dishData == null || dishSpawner == null || dishSpawner.allDishes == null)
        {
            return -1;
        }

        for (int i = 0; i < dishSpawner.allDishes.Count; i++)
        {
            DishData dish = dishSpawner.allDishes[i];

            if (dish == null)
            {
                continue;
            }

            if (dish == dishData)
            {
                return i;
            }

            if (dish.name == dishData.name)
            {
                return i;
            }
        }

        return -1;
    }

    private void RegisterCleanedDishIndex(int cleanedDishIndex)
    {
        if (cleanedDishIndex < 0)
        {
            return;
        }

        int clampedIndex = ClampCleanPileIndex(cleanedDishIndex);

        if (clampedIndex < 0)
        {
            return;
        }

        if (clampedIndex <= highestCleanedDishIndex)
        {
            return;
        }

        highestCleanedDishIndex = clampedIndex;

        SaveCleanPileProgress();
        RefreshCleanPileImages(force: true);
    }

    private int ClampCleanPileIndex(int index)
    {
        if (cleanPileImagesByCleanOrder == null || cleanPileImagesByCleanOrder.Length == 0)
        {
            return -1;
        }

        if (index < 0)
        {
            return -1;
        }

        return Mathf.Clamp(index, 0, cleanPileImagesByCleanOrder.Length - 1);
    }

    private void LoadCleanPileProgress()
    {
        int defaultIndex = showFirstCleanPileBeforeAnyDishCleaned ? 0 : -1;

        highestCleanedDishIndex = defaultIndex;

        if (!saveCleanPileProgressWithPlayerPrefs)
        {
            highestCleanedDishIndex = ClampCleanPileIndex(highestCleanedDishIndex);
            return;
        }

        string key = GetCleanPilePrefsKey();

        if (PlayerPrefs.HasKey(key))
        {
            highestCleanedDishIndex = PlayerPrefs.GetInt(key, defaultIndex);
        }

        highestCleanedDishIndex = ClampCleanPileIndex(highestCleanedDishIndex);
    }

    private void SaveCleanPileProgress()
    {
        if (!saveCleanPileProgressWithPlayerPrefs)
        {
            return;
        }

        PlayerPrefs.SetInt(GetCleanPilePrefsKey(), highestCleanedDishIndex);
        PlayerPrefs.Save();
    }

    private string GetCleanPilePrefsKey()
    {
        return $"{cleanPileProgressPrefsKey}_{ResolveKitchenId()}";
    }

    private string ResolveKitchenId()
    {
        if (autoUseLoanManagerKitchenId)
        {
            LoanManager loanManager = FindFirstObjectByType<LoanManager>();

            if (loanManager != null && !string.IsNullOrWhiteSpace(loanManager.GetKitchenId()))
            {
                return loanManager.GetKitchenId();
            }
        }

        return string.IsNullOrWhiteSpace(kitchenId) ? "kitchen_1" : kitchenId;
    }

    private void CopyRectTransform(RectTransform source, RectTransform copy)
    {
        copy.anchorMin = source.anchorMin;
        copy.anchorMax = source.anchorMax;
        copy.pivot = source.pivot;
        copy.anchoredPosition = source.anchoredPosition;
        copy.sizeDelta = source.sizeDelta;
        copy.localRotation = source.localRotation;
        copy.localScale = source.localScale;
    }

    private void CopyImage(Image source, Image copy)
    {
        copy.sprite = source.sprite;
        copy.overrideSprite = source.overrideSprite;
        copy.color = source.color;
        copy.material = source.material;
        copy.type = source.type;
        copy.preserveAspect = source.preserveAspect;
        copy.fillCenter = source.fillCenter;
        copy.fillMethod = source.fillMethod;
        copy.fillOrigin = source.fillOrigin;
        copy.fillAmount = source.fillAmount;
        copy.fillClockwise = source.fillClockwise;
    }

    private IEnumerator AnimateToCleanPile(RectTransform dishRect, int completedDishIndex)
    {
        if (dishRect == null)
        {
            RegisterCleanedDishIndex(completedDishIndex);
            yield break;
        }

        Vector2 startPosition = dishRect.anchoredPosition;
        Vector2 targetPosition = startPosition + Vector2.left * moveDistance;

        Vector3 startScale = dishRect.localScale;
        Vector3 targetScale = startScale * shrinkMultiplier;

        float safeSpeed = Mathf.Max(1f, moveSpeed);
        float duration = Mathf.Max(0.01f, Mathf.Abs(moveDistance) / safeSpeed);
        float elapsed = 0f;

        while (elapsed < duration && dishRect != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            dishRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            dishRect.localScale = Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        if (dishRect != null)
        {
            Destroy(dishRect.gameObject);
        }

        RegisterCleanedDishIndex(completedDishIndex);
    }
}