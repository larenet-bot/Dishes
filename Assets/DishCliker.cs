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

    [Header("Power Washer Hold")]
    [Tooltip("Enables hold-to-clean behavior when the active sink is a power washer.")]
    public bool enablePowerWasherHold = true;

    [Tooltip("When enabled, hold input only counts while the pointer is over the dish image.")]
    public bool requirePointerOverDishImage = true;

    [Header("Technique References")]
    [Tooltip("Optional Turbo Jet skill check UI used by the power washer.")]
    public PowerWasherSkillCheckUI powerWasherSkillCheckUI;

    [Tooltip("Node ID used to check whether the Turbo Jet technique has been purchased.")]
    public string powerWasherTechniqueNodeId = "pw_technique";

    [Tooltip("Optional wash-basin technique that modifies the next manual wash reward.")]
    public WashBasinSoakTechnique washBasinSoakTechnique;

    [Header("Delegated Components")]
    [Tooltip("Component that spawns floating reward text. If null, will be fetched from same GameObject.")]
    public RewardTextSpawner rewardTextSpawner;

    [Tooltip("Component that handles audio and bubble effects. If null, will be fetched from same GameObject.")]
    public AudioEffects audioEffects;

    private Coroutine instantWashCoroutine;
    private DishData currentDish;

    private int currentClicks;
    private int lastSqueakIndex = -1;

    // Power washer hold state.
    private bool isHolding;
    private float holdSeconds;
    private float holdStageUnits;

    // Turbo Jet state.
    private bool isTurboJetSkillCheckActive;
    private float burnEndTime = -1f;
    private float nextTurboJetSkillCheckAt = 30f;
    private float turboJetSkillCheckStartTime = -1f;

    #region Unity Lifecycle

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
    }

    private void Update()
    {
        if (!enablePowerWasherHold || currentDish == null)
        {
            return;
        }

        if (sinkManager == null)
        {
            sinkManager = SinkManager.Instance;
        }

        if (sinkManager == null || sinkManager.CurrentSinkType != SinkManager.SinkType.PowerWasher)
        {
            ResetHoldState();
            return;
        }

        bool holding = Input.GetMouseButton(0);
        if (holding && requirePointerOverDishImage && !IsPointerOverDishImage())
        {
            holding = false;
        }

        if (!holding)
        {
            ResetHoldState();
            return;
        }

        if (HandleActiveTurboJetSkillCheck())
        {
            return;
        }

        isHolding = true;
        holdSeconds += Time.deltaTime;

        if (ShouldTriggerTurboJetSkillCheck())
        {
            TryBeginTurboJetSkillCheck();
            if (isTurboJetSkillCheckActive)
            {
                return;
            }
        }

        float stagesPerSecond = GetPowerWasherStagesPerSecond();
        holdStageUnits += stagesPerSecond * Time.deltaTime;

        ProcessHoldStageUnits();
    }

    #endregion

    #region Initialization and Public API

    /// <summary>
    /// Assigns the active dish and resets local cleaning progress.
    /// </summary>
    public void Init(DishData data)
    {
        currentDish = data;
        currentClicks = 0;
        holdStageUnits = 0f;

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

    #region Power Washer Flow

    private bool HandleActiveTurboJetSkillCheck()
    {
        if (!isTurboJetSkillCheckActive)
        {
            return false;
        }

        if (powerWasherSkillCheckUI == null)
        {
            isTurboJetSkillCheckActive = false;
            return false;
        }

        float timeout = Mathf.Max(0.1f, powerWasherSkillCheckUI.durationSeconds) + 0.35f;

        if (!powerWasherSkillCheckUI.IsActive)
        {
            isTurboJetSkillCheckActive = false;
            return false;
        }

        if (turboJetSkillCheckStartTime > 0f && (Time.time - turboJetSkillCheckStartTime) > timeout)
        {
            powerWasherSkillCheckUI.Cancel();
            OnTurboJetSkillCheckResolved(false);
        }

        return true;
    }

    private void TryBeginTurboJetSkillCheck()
    {
        if (powerWasherSkillCheckUI == null)
        {
            nextTurboJetSkillCheckAt += 30f;
            return;
        }

        isTurboJetSkillCheckActive = true;
        turboJetSkillCheckStartTime = Time.time;
        nextTurboJetSkillCheckAt += 30f;

        try
        {
            powerWasherSkillCheckUI.Begin(OnTurboJetSkillCheckResolved);
        }
        catch
        {
            isTurboJetSkillCheckActive = false;
        }
    }

    private float GetPowerWasherStagesPerSecond()
    {
        float stagesPerSecond = sinkManager.GetPowerWasherBaseStagesPerSecond();
        stagesPerSecond *= sinkManager.GetPowerWasherNozzleMultiplier();

        if (Time.time < burnEndTime)
        {
            stagesPerSecond *= 2f;
        }

        if (sinkManager.HasPowerWasherMomentum())
        {
            sinkManager.GetPowerWasherMomentumSettings(out float startAfter, out float perSecondBonus, out float maxBonus);

            float elapsedAfterThreshold = Mathf.Max(0f, holdSeconds - startAfter);
            int stacks = Mathf.FloorToInt(elapsedAfterThreshold);
            float bonus = Mathf.Min(stacks * perSecondBonus, maxBonus);

            stagesPerSecond *= 1f + bonus;
        }

        return stagesPerSecond;
    }

    private void ProcessHoldStageUnits()
    {
        if (currentDish == null)
        {
            return;
        }

        int finalStageIndex = currentDish.stageSprites.Length - 1;
        int stagesPerClick = upgrades != null ? Mathf.Max(1, upgrades.GetCurrentStagesPerClick()) : 1;

        while (holdStageUnits >= 1f && currentDish != null)
        {
            holdStageUnits -= 1f;

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

    private void ResetHoldState()
    {
        if (!isHolding)
        {
            return;
        }

        isHolding = false;
        holdSeconds = 0f;
        holdStageUnits = 0f;
        nextTurboJetSkillCheckAt = 30f;
        turboJetSkillCheckStartTime = -1f;

        if (!isTurboJetSkillCheckActive)
        {
            return;
        }

        isTurboJetSkillCheckActive = false;
        if (powerWasherSkillCheckUI != null)
        {
            powerWasherSkillCheckUI.Cancel();
        }
    }

    private bool ShouldTriggerTurboJetSkillCheck()
    {
        if (sinkManager == null || string.IsNullOrWhiteSpace(powerWasherTechniqueNodeId))
        {
            return false;
        }

        if (!sinkManager.IsPurchased(powerWasherTechniqueNodeId))
        {
            return false;
        }

        return holdSeconds >= nextTurboJetSkillCheckAt;
    }

    private void OnTurboJetSkillCheckResolved(bool success)
    {
        isTurboJetSkillCheckActive = false;
        turboJetSkillCheckStartTime = -1f;

        if (success)
        {
            burnEndTime = Time.time + 4f;
        }
    }

    private bool IsPointerOverDishImage()
    {
        if (dishVisual == null || dishVisual.dishImage == null)
        {
            return true;
        }

        Canvas canvas = dishVisual.dishImage.canvas;
        Camera canvasCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera = canvas.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            dishVisual.dishImage.rectTransform,
            Input.mousePosition,
            canvasCamera);
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
