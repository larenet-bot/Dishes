using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls manual dish cleaning, hold-to-clean behavior for the power washer,
/// wash-basin reward rules, and floating reward text.
/// </summary>
/// <remarks>
/// This component is responsible for:
/// <list type="bullet">
/// <item><description>Advancing dish cleaning stages from clicks or hold input.</description></item>
/// <item><description>Completing dishes and forwarding rewards to <see cref="ScoreManager"/>.</description></item>
/// <item><description>Applying sink-specific behavior such as power washer hold logic and wash-basin dish bonuses.</description></item>
/// <item><description>Displaying bubble bursts, squeak audio, and floating reward text.</description></item>
/// </list>
/// </remarks>
public class DishClicker : MonoBehaviour
{
    [Header("Cleaning Settings")]
    [Tooltip("Legacy click requirement value. Dish stage progression currently follows the active dish data and upgrade settings.")]
    public int clicksRequired = 3;

    [Header("Scene References")]
    [Tooltip("Visual component that displays the current dish state.")]
    public DishVisual dishVisual;

    [Tooltip("Optional bubble burst effect played during washing.")]
    public SudsOnClick sudsOnClick;

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

    [Header("Audio")]
    [Tooltip("Random squeak clips played when a dish is completed.")]
    public AudioClip[] squeakClips;

    [Header("Reward Text")]
    [SerializeField, Tooltip("Prefab used for floating reward text.")]
    private GameObject rewardTextPrefab;

    [SerializeField, Tooltip("World-space offset used for standard reward text.")]
    private Vector3 rewardTextOffset = new Vector3(0f, 0.5f, 0f);

    [SerializeField, Tooltip("World-space Z plane used when spawning floating text.")]
    private float rewardTextPlaneZ = 0f;

    [Header("Instant Wash Text")]
    [SerializeField, Tooltip("World-space offset used for the instant wash title text.")]
    private Vector3 instantWashAwardTextOffset = new Vector3(0f, 0.75f, 0f);

    private AudioSource audioSource;
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
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (sinkManager == null)
        {
            sinkManager = SinkManager.Instance;
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
            BurstBubblesAtMouse();
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

    /// <summary>
    /// Returns true while a Turbo Jet skill check is active and dish processing should pause.
    /// </summary>
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

    /// <summary>
    /// Starts the Turbo Jet skill check when the timing threshold is reached.
    /// </summary>
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
            // Prevent the washer from remaining locked if the UI cannot start.
            isTurboJetSkillCheckActive = false;
        }
    }

    /// <summary>
    /// Converts active power washer modifiers into stage progress per second.
    /// </summary>
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

    /// <summary>
    /// Applies accumulated hold progress to the current dish.
    /// </summary>
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
                BurstBubblesAtMouse();
                continue;
            }

            // One additional unit beyond the last visible stage completes the dish.
            CompleteDish();
        }
    }

    /// <summary>
    /// Clears all temporary hold-state values.
    /// </summary>
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

    /// <summary>
    /// Returns true when the Turbo Jet technique is owned and its next timing window has been reached.
    /// </summary>
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

    /// <summary>
    /// Applies the Turbo Jet result.
    /// A successful check grants a short burn window that doubles washer speed.
    /// </summary>
    private void OnTurboJetSkillCheckResolved(bool success)
    {
        isTurboJetSkillCheckActive = false;
        turboJetSkillCheckStartTime = -1f;

        if (success)
        {
            burnEndTime = Time.time + 4f;
        }
    }

    /// <summary>
    /// Returns true if the pointer is inside the current dish image rect.
    /// </summary>
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

    /// <summary>
    /// Completes the current dish as a manual wash.
    /// </summary>
    private void CompleteDish()
    {
        CompleteDishInternal(isManual: true, spawnRewardAtDish: false);
    }

    /// <summary>
    /// Completes the current dish, forwards the reward to <see cref="ScoreManager"/>,
    /// and displays the appropriate visual feedback.
    /// </summary>
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

        PlayRandomSqueak();

        if (spawnRewardAtDish && TryGetDishWorldPosition(out Vector3 dishWorld))
        {
            SpawnRewardTextAtWorld(reward, dishWorld);
            return;
        }

        SpawnRewardText(reward);
    }

    /// <summary>
    /// Calculates how many dishes a manual completion should award.
    /// Wash-basin modifiers are applied here so click progression remains separate from reward logic.
    /// </summary>
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

    /// <summary>
    /// Applies a timed stream of wash ticks to the current dish.
    /// </summary>
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

    /// <summary>
    /// Applies one instant-wash tick.
    /// </summary>
    private void ApplyInstantWashTick(string awardTitle)
    {
        if (currentDish == null)
        {
            return;
        }

        SpawnInstantWashAwardText(awardTitle);

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

    #region Floating Text and Position Helpers

    /// <summary>
    /// Spawns reward text near the pointer position.
    /// </summary>
    private void SpawnRewardText(float rewardAmount)
    {
        if (rewardTextPrefab == null || Camera.main == null || rewardAmount <= 0f)
        {
            return;
        }

        if (!TryGetMouseWorldPosition(out Vector3 worldPosition))
        {
            return;
        }

        worldPosition.z = rewardTextPlaneZ;
        worldPosition += rewardTextOffset;

        SpawnFloatingText(worldPosition, "+ " + BigNumberFormatter.FormatMoney((double)rewardAmount));
    }

    /// <summary>
    /// Spawns reward text at a supplied world position.
    /// </summary>
    private void SpawnRewardTextAtWorld(float rewardAmount, Vector3 worldPosition)
    {
        if (rewardTextPrefab == null || rewardAmount <= 0f)
        {
            return;
        }

        worldPosition.z = rewardTextPlaneZ;
        worldPosition += rewardTextOffset;

        SpawnFloatingText(worldPosition, "+ " + BigNumberFormatter.FormatMoney((double)rewardAmount));
    }

    /// <summary>
    /// Spawns the instant wash title above the dish.
    /// </summary>
    private void SpawnInstantWashAwardText(string awardTitle)
    {
        if (rewardTextPrefab == null || string.IsNullOrWhiteSpace(awardTitle))
        {
            return;
        }

        if (!TryGetDishWorldPosition(out Vector3 dishWorld))
        {
            return;
        }

        dishWorld.z = rewardTextPlaneZ;
        dishWorld += instantWashAwardTextOffset;

        SpawnFloatingText(dishWorld, awardTitle);
    }

    /// <summary>
    /// Creates floating text using the configured prefab.
    /// </summary>
    private void SpawnFloatingText(Vector3 worldPosition, string message)
    {
        GameObject instance = Instantiate(rewardTextPrefab, worldPosition, Quaternion.identity);
        BubbleRewardText floatingText = instance.GetComponent<BubbleRewardText>();

        if (floatingText != null)
        {
            floatingText.Initialize(message);
        }
    }

    /// <summary>
    /// Converts the current pointer position to a world position on the reward-text plane.
    /// </summary>
    private bool TryGetMouseWorldPosition(out Vector3 worldPos)
    {
        worldPos = default;
        if (Camera.main == null)
        {
            return false;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, rewardTextPlaneZ));
        if (plane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }

        worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));
        worldPos.z = rewardTextPlaneZ;
        return true;
    }

    /// <summary>
    /// Converts the dish image center to a world position on the reward-text plane.
    /// </summary>
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
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, rewardTextPlaneZ));
        if (plane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }

        worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Camera.main.nearClipPlane));
        worldPos.z = rewardTextPlaneZ;
        return true;
    }

    #endregion

    #region Audio and Visual Effects

    /// <summary>
    /// Plays the configured bubble burst at the current pointer position.
    /// </summary>
    private void BurstBubblesAtMouse()
    {
        if (sudsOnClick == null || Camera.main == null)
        {
            return;
        }

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.nearClipPlane;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        sudsOnClick.BurstBubbles(worldPosition);
    }

    /// <summary>
    /// Plays a completion squeak clip, avoiding immediate repetition when possible.
    /// </summary>
    private void PlayRandomSqueak()
    {
        if (audioSource == null || squeakClips == null || squeakClips.Length == 0)
        {
            return;
        }

        int index;
        do
        {
            index = Random.Range(0, squeakClips.Length);
        }
        while (index == lastSqueakIndex && squeakClips.Length > 3);

        lastSqueakIndex = index;
        audioSource.PlayOneShot(squeakClips[index]);
    }

    #endregion
}
