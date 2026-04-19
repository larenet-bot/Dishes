using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls manual dish cleaning, hold-to-clean behavior for the power washer,
/// wash-basin reward rules, and overall dish progression. Floating text and audio
/// are delegated to separate components to keep this class focused.
/// </summary>
public class DishClicker : MonoBehaviour
{
    [Header("Cleaning Settings")]
    [Tooltip("Legacy click requirement value. Dish stage progression currently follows the active dish data and upgrade settings.")]
    public int clicksRequired = 3;

    [Header("Scene References")]
    [Tooltip("Visual component that displays the current dish state.")]
    public DishVisual dishVisual;

    [Tooltip("Upgrade source used to determine how many stages are cleared per interaction.")]
    public Upgrades upgrades;

    [Header("Sink References")]
    [Tooltip("Optional direct reference. Falls back to SinkManager.Instance at runtime.")]
    public SinkManager sinkManager;

    [Header("Delegated Components")]
    [Tooltip("Component that spawns floating reward text. If null, will be fetched from same GameObject.")]
    public RewardTextSpawner rewardTextSpawner;

    [Tooltip("Component that handles audio and bubble effects. If null, will be fetched from same GameObject.")]
    public AudioEffects audioEffects;

    [Header("Power Washer")]
    [Tooltip("Optional component that handles the power-washer hold flow. If null, will be fetched from same GameObject.")]
    public PowerWasherController powerWasherController;

    [Header("Techniques")]
    [Tooltip("Optional wash-basin technique that modifies the next manual wash reward.")]
    public WashBasinSoakTechnique washBasinSoakTechnique;

    private Coroutine instantWashCoroutine;
    private DishData currentDish;

    private int currentClicks;
    private int lastSqueakIndex = -1;

    private void Start()
    {
        if (sinkManager == null)
        {
            sinkManager = SinkManager.Instance;
        }

        if (rewardTextSpawner == null)
        {
            rewardTextSpawner = GetComponent<RewardTextSpawner>();
        }

        if (audioEffects == null)
        {
            audioEffects = GetComponent<AudioEffects>();
        }

        if (powerWasherController == null)
        {
            powerWasherController = GetComponent<PowerWasherController>();
            if (powerWasherController != null)
            {
                powerWasherController.Initialize(this);
            }
        }
    }

    #region Initialization and Public API

    /// <summary>
    /// Assigns the active dish and resets local cleaning progress.
    /// </summary>
    public void Init(DishData data)
    {
        currentDish = data;
        currentClicks = 0;

        if (powerWasherController != null)
        {
            powerWasherController.OnDishAssigned(data);
        }

        dishVisual.SetDish(currentDish);
        dishVisual.SetStage(0);
    }

    /// <summary>
    /// Returns the currently assigned dish data.
    /// </summary>
    public DishData GetCurrentDishData() => currentDish;

    /// <summary>
    /// Returns the number of dishes a manual completion would award without completing the current dish.
    /// </summary>
    public long PreviewManualDishesAwarded() => CalculateManualDishesAwarded();

    /// <summary>
    /// Handles a standard click-based wash interaction.
    /// </summary>
    public void OnDishClicked()
    {
        if (currentDish == null)
        {
            return;
        }

        int stagesPerClick = upgrades != null ? upgrades.GetCurrentStagesPerClick() : 1;
        int finalStageIndex = currentDish.stageSprites.Length - 1;

        if (currentClicks < finalStageIndex)
        {
            currentClicks = Mathf.Min(currentClicks + stagesPerClick, finalStageIndex);
            dishVisual?.SetStage(currentClicks);
            audioEffects?.BurstBubblesAtMouse();
            return;
        }

        CompleteDish();
    }

    /// <summary>
    /// Starts a temporary automatic wash effect, usually triggered by an external reward.
    /// </summary>
    /// <param name="ticksPerSecond">How many wash ticks should occur each second.</param>
    /// <param name="secondsPerStage">How long each dish stage should contribute to the total duration.</param>
    /// <param name="awardTitle">Floating text shown above the dish while the effect is active.</param>
    /// <param name="stageCountOverride">Optional manual stage count used instead of the current dish sprite count.</param>
    public void StartInstantWash(float ticksPerSecond, float secondsPerStage, string awardTitle = "Instant Wash", int stageCountOverride = 0)
    {
        if (currentDish == null)
        {
            return;
        }

        if (instantWashCoroutine != null)
        {
            StopCoroutine(instantWashCoroutine);
        }

        ticksPerSecond = Mathf.Max(0.1f, ticksPerSecond);
        secondsPerStage = Mathf.Max(0.1f, secondsPerStage);

        int stageCount = stageCountOverride > 0
            ? stageCountOverride
            : (currentDish.stageSprites != null ? currentDish.stageSprites.Length : 1);

        stageCount = Mathf.Max(1, stageCount);
        float totalDuration = secondsPerStage * stageCount;

        instantWashCoroutine = StartCoroutine(InstantWashCoroutine(ticksPerSecond, totalDuration, awardTitle));
    }

    #endregion

    #region Power Washer API (called from PowerWasherController)

    /// <summary>
    /// Advance the dish by a number of stage-units produced by the power washer.
    /// Each unit behaves like one wash "tick" (matching previous while-loop behavior).
    /// This method consumes integer stage-units and applies visual/audio changes and completion.
    /// </summary>
    public void ApplyStageUnitsFromPowerWasher(int stageUnits)
    {
        if (currentDish == null || stageUnits <= 0)
            return;

        int finalStageIndex = currentDish.stageSprites.Length - 1;
        int stagesPerClick = upgrades != null ? Mathf.Max(1, upgrades.GetCurrentStagesPerClick()) : 1;

        while (stageUnits > 0 && currentDish != null)
        {
            stageUnits--;

            if (currentClicks < finalStageIndex)
            {
                currentClicks = Mathf.Min(currentClicks + stagesPerClick, finalStageIndex);
                dishVisual?.SetStage(currentClicks);
                audioEffects?.BurstBubblesAtMouse();
                continue;
            }

            // One additional unit beyond the last visible stage completes the dish.
            CompleteDish();
        }
    }

    #endregion

    #region Dish Completion and Reward Calculation

    private void CompleteDish()
    {
        CompleteDishInternal(isManual: true, spawnRewardAtDish: false);
    }

    private void CompleteDishInternal(bool isManual, bool spawnRewardAtDish)
    {
        if (currentDish == null || ScoreManager.Instance == null)
        {
            return;
        }

        long dishesAwarded = CalculateManualDishesAwarded();

        currentClicks = 0;
        dishVisual?.SetStage(0);

        float reward = ScoreManager.Instance.OnDishCleaned(currentDish, dishesAwarded);

        if (isManual && washBasinSoakTechnique != null)
        {
            washBasinSoakTechnique.OnManualWashCompleted(dishesAwarded);
        }

        audioEffects?.PlayRandomSqueak();

        if (spawnRewardAtDish && rewardTextSpawner != null && TryGetDishWorldPosition(out Vector3 dishWorld))
        {
            rewardTextSpawner.SpawnRewardTextAtWorld(reward, dishWorld);
            return;
        }

        rewardTextSpawner?.SpawnRewardText(reward);
    }

    private long CalculateManualDishesAwarded()
    {
        int baseIncrement = 1;
        if (ScoreManager.Instance != null)
        {
            baseIncrement = Mathf.Max(1, ScoreManager.Instance.GetDishCountIncrement());
        }

        long awarded = baseIncrement;

        if (sinkManager == null)
        {
            sinkManager = SinkManager.Instance;
        }

        if (sinkManager != null && sinkManager.CurrentSinkType == SinkManager.SinkType.WashBasin)
        {
            int multiplier = Mathf.Max(1, sinkManager.GetWashBasinManualMultiplier());
            awarded = (long)baseIncrement * multiplier;

            awarded += sinkManager.GetWashBasinFlatBonusDishes();

            if (sinkManager.TryRollWashBasinExtraDishes(out int extra))
            {
                awarded += Mathf.Max(0, extra);
            }

            if (washBasinSoakTechnique != null)
            {
                awarded = washBasinSoakTechnique.PreviewApplySoak(awarded);
            }
        }

        if (awarded < 1)
        {
            awarded = 1;
        }

        return awarded;
    }

    #endregion

    #region Instant Wash

    private IEnumerator InstantWashCoroutine(float ticksPerSecond, float totalDuration, string awardTitle)
    {
        float interval = 1f / Mathf.Max(0.1f, ticksPerSecond);
        float endAt = Time.time + Mathf.Max(0.05f, totalDuration);

        while (Time.time < endAt)
        {
            if (currentDish == null)
            {
                yield break;
            }

            ApplyInstantWashTick(awardTitle);
            yield return new WaitForSeconds(interval);
        }

        instantWashCoroutine = null;
    }

    private void ApplyInstantWashTick(string awardTitle)
    {
        if (currentDish == null)
        {
            return;
        }

        rewardTextSpawner?.SpawnInstantWashAwardText(awardTitle, dishVisual);

        int stagesPerClick = upgrades != null ? Mathf.Max(1, upgrades.GetCurrentStagesPerClick()) : 1;
        int finalStageIndex = currentDish.stageSprites.Length - 1;

        if (currentClicks < finalStageIndex)
        {
            currentClicks = Mathf.Min(currentClicks + stagesPerClick, finalStageIndex);
            dishVisual?.SetStage(currentClicks);
            return;
        }

        // One additional tick beyond the last visible stage completes the dish.
        CompleteDishInternal(isManual: false, spawnRewardAtDish: true);
    }

    #endregion

    #region Helpers

    private bool TryGetDishWorldPosition(out Vector3 worldPos)
    {
        worldPos = default;
        if (dishVisual == null || dishVisual.dishImage == null || Camera.main == null)
        {
            return false;
        }

        RectTransform rectTransform = dishVisual.dishImage.rectTransform;
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

        Canvas canvas = dishVisual.dishImage.canvas;
        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);

        Ray ray = Camera.main.ScreenPointToRay(screenPoint);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, 0f));
        if (plane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }

        worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Camera.main.nearClipPlane));
        worldPos.z = 0f;
        return true;
    }

    #endregion
}
