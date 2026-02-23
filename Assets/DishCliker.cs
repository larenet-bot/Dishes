// DishCliker.cs
using UnityEngine;
using UnityEngine.UI;

public class DishClicker : MonoBehaviour
{
    [Header("Cleaning Settings")]
    public int clicksRequired = 3;

    [Header("References")]
    public DishVisual dishVisual;
    public SudsOnClick sudsOnClick; // bubbles
    public Upgrades upgrades; // assign in inspector or find at runtime

    [Header("Sink System")]
    public SinkManager sinkManager;

    [Header("Power Washer Hold")]
    public bool enablePowerWasherHold = true;
    public bool requirePointerOverDishImage = true;

    [Header("Techniques")]
    [Tooltip("Power washer Turbo Jet skill check UI (optional).")]
    public PowerWasherSkillCheckUI powerWasherSkillCheckUI;
    [Tooltip("If your node IDs differ, change this to match the Turbo Jet technique node.")]
    public string powerWasherTechniqueNodeId = "pw_technique";

    [Tooltip("Wash basin Overnight Soak technique state (optional).")]
    public WashBasinSoakTechnique washBasinSoakTechnique;

    [Header("Sounds")]
    public AudioClip[] squeakClips;
    private AudioSource audioSource;
    private int lastSqueakIndex = -1;

    [Header("Reward Text")]
    [SerializeField] private GameObject rewardTextPrefab; // BubbleRewardText prefab
    [SerializeField] private Vector3 rewardTextOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private float rewardTextPlaneZ = 0f;

    [Header("Instant Wash Award Text")]
    [SerializeField] private Vector3 instantWashAwardTextOffset = new Vector3(0f, 0.75f, 0f);

    private int currentClicks = 0;
    private DishData currentDish;

    private Coroutine instantWashCoroutine;

    // Power washer hold state
    private bool isHolding = false;
    private float holdSeconds = 0f;
    private float holdStageUnits = 0f;

    // Power washer technique state
    private bool isTurboJetSkillCheckActive = false;
    private float burnEndTime = -1f;

    // Turbo Jet repeats every 30 seconds while holding.
    private float nextTurboJetSkillCheckAt = 30f;
    private float turboJetSkillCheckStartTime = -1f;

    public void Init(DishData data)
    {
        currentDish = data;
        currentClicks = 0;
        holdStageUnits = 0f;
        dishVisual.SetDish(currentDish);
        dishVisual.SetStage(0);
    }

    // --- Public accessors for other systems (Dishwasher, UI, etc.) ---
    public DishData GetCurrentDishData() => currentDish;

    // Returns how many dishes a manual completion would currently award (does not change visuals).
    public long PreviewManualDishesAwarded() => CalculateManualDishesAwarded();

    public void OnDishClicked()
    {
        if (currentDish == null) return;

        int stagesPerClick = upgrades != null ? upgrades.GetCurrentStagesPerClick() : 1;
        int finalStageIndex = currentDish.stageSprites.Length - 1;

        if (currentClicks < finalStageIndex)
        {
            int nextStage = Mathf.Min(currentClicks + stagesPerClick, finalStageIndex);
            currentClicks = nextStage;
            dishVisual?.SetStage(currentClicks);

            if (sudsOnClick != null && Camera.main != null)
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = Camera.main.nearClipPlane;
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                sudsOnClick.BurstBubbles(worldPos);
            }

            return;
        }

        if (currentClicks >= finalStageIndex)
            CompleteDish();
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (sinkManager == null) sinkManager = SinkManager.Instance;
    }

    private void Update()
    {
        if (!enablePowerWasherHold) return;
        if (currentDish == null) return;

        if (sinkManager == null) sinkManager = SinkManager.Instance;
        if (sinkManager == null || sinkManager.CurrentSinkType != SinkManager.SinkType.PowerWasher)
        {
            ResetHoldState();
            return;
        }

        bool holding = Input.GetMouseButton(0);
        if (holding && requirePointerOverDishImage && !IsPointerOverDishImage())
            holding = false;

        if (!holding)
        {
            ResetHoldState();
            return;
        }

        // If the skill check is open, pause washing until it resolves.
        // Add a failsafe so we don't get stuck if the UI is inactive/disabled.
        if (isTurboJetSkillCheckActive)
        {
            if (powerWasherSkillCheckUI == null)
            {
                isTurboJetSkillCheckActive = false;
            }
            else
            {
                float timeout = Mathf.Max(0.1f, powerWasherSkillCheckUI.durationSeconds) + 0.35f;

                // If the UI stopped being active without invoking the callback, resume washing.
                if (!powerWasherSkillCheckUI.IsActive)
                {
                    isTurboJetSkillCheckActive = false;
                }
                else if (turboJetSkillCheckStartTime > 0f && (Time.time - turboJetSkillCheckStartTime) > timeout)
                {
                    // UI is active but not progressing (Update not running) - cancel and treat as a miss.
                    powerWasherSkillCheckUI.Cancel();
                    OnTurboJetSkillCheckResolved(false);
                    return;
                }
                else
                {
                    return;
                }
            }
        }

        isHolding = true;
        holdSeconds += Time.deltaTime;

        // Turbo Jet Mode: every 30 seconds of holding, prompt a skill check.
        if (ShouldTriggerTurboJetSkillCheck())
        {
            if (powerWasherSkillCheckUI != null)
            {
                isTurboJetSkillCheckActive = true;
                turboJetSkillCheckStartTime = Time.time;
                nextTurboJetSkillCheckAt += 30f;

                try
                {
                    powerWasherSkillCheckUI.Begin(OnTurboJetSkillCheckResolved);
                }
                catch
                {
                    // If anything goes wrong, don't soft-lock washing.
                    isTurboJetSkillCheckActive = false;
                }
                return;
            }

            // No UI assigned. Just schedule the next check and keep going.
            nextTurboJetSkillCheckAt += 30f;
        }

        float stagesPerSecond = sinkManager.GetPowerWasherBaseStagesPerSecond();
        stagesPerSecond *= sinkManager.GetPowerWasherNozzleMultiplier();

        // Burn effect from Turbo Jet Mode
        if (Time.time < burnEndTime)
            stagesPerSecond *= 2f;

        if (sinkManager.HasPowerWasherMomentum())
        {
            sinkManager.GetPowerWasherMomentumSettings(out float startAfter, out float perSecondBonus, out float maxBonus);
            float t = Mathf.Max(0f, holdSeconds - startAfter);
            int stacks = Mathf.FloorToInt(t);
            float bonus = Mathf.Min(stacks * perSecondBonus, maxBonus);
            stagesPerSecond *= (1f + bonus);
        }

        holdStageUnits += stagesPerSecond * Time.deltaTime;
        ProcessHoldStageUnits();
    }

    private void ProcessHoldStageUnits()
    {
        if (currentDish == null) return;
        int finalStageIndex = currentDish.stageSprites.Length - 1;

        int stagesPerClick = 1;
        if (upgrades != null)
            stagesPerClick = Mathf.Max(1, upgrades.GetCurrentStagesPerClick());

        while (holdStageUnits >= 1f && currentDish != null)
        {
            holdStageUnits -= 1f;

            if (currentClicks < finalStageIndex)
            {
                currentClicks = Mathf.Min(currentClicks + stagesPerClick, finalStageIndex);
                dishVisual?.SetStage(currentClicks);
                BurstBubbles();
                continue;
            }

            // One extra "stage unit" past the final stage completes the dish.
            CompleteDish();
        }
    }

    private void ResetHoldState()
    {
        if (!isHolding) return;
        isHolding = false;
        holdSeconds = 0f;
        holdStageUnits = 0f;

        nextTurboJetSkillCheckAt = 30f;
        turboJetSkillCheckStartTime = -1f;

        if (isTurboJetSkillCheckActive)
        {
            isTurboJetSkillCheckActive = false;
            if (powerWasherSkillCheckUI != null)
                powerWasherSkillCheckUI.Cancel();
        }
    }

    private bool IsPointerOverDishImage()
    {
        if (dishVisual == null || dishVisual.dishImage == null) return true;

        Canvas canvas = dishVisual.dishImage.canvas;
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(
            dishVisual.dishImage.rectTransform,
            Input.mousePosition,
            cam
        );
    }

    private void CompleteDish()
    {
        CompleteDishInternal(isManual: true, spawnRewardAtDish: false);
    }

    private void CompleteDishInternal(bool isManual, bool spawnRewardAtDish)
    {
        if (currentDish == null) return;
        if (ScoreManager.Instance == null) return;

        long dishesAwarded = CalculateManualDishesAwarded();

        currentClicks = 0;
        dishVisual?.SetStage(0);

        float reward = ScoreManager.Instance.OnDishCleaned(currentDish, dishesAwarded);

        if (isManual && washBasinSoakTechnique != null)
            washBasinSoakTechnique.OnManualWashCompleted(dishesAwarded);

        PlayRandomSqueak();

        if (spawnRewardAtDish)
        {
            if (TryGetDishWorldPosition(out Vector3 dishWorld))
                SpawnRewardTextAtWorld(reward, dishWorld);
            else
                SpawnRewardText(reward);
        }
        else
        {
            SpawnRewardText(reward);
        }
    }

    /// <summary>
    /// Bubble award: rapidly applies "wash ticks" to the current dish.
    /// - ticksPerSecond: how many ticks happen each second
    /// - secondsPerStage: duration scales with current dish stage count
    /// </summary>
    public void StartInstantWash(float ticksPerSecond, float secondsPerStage, string awardTitle = "Instant Wash", int stageCountOverride = 0)
    {
        if (currentDish == null) return;

        if (instantWashCoroutine != null)
            StopCoroutine(instantWashCoroutine);

        ticksPerSecond = Mathf.Max(0.1f, ticksPerSecond);
        secondsPerStage = Mathf.Max(0.1f, secondsPerStage);

        int stageCount = stageCountOverride > 0
            ? stageCountOverride
            : ((currentDish.stageSprites != null) ? currentDish.stageSprites.Length : 1);
        stageCount = Mathf.Max(1, stageCount);
        float totalDuration = secondsPerStage * stageCount;

        instantWashCoroutine = StartCoroutine(InstantWashCoroutine(ticksPerSecond, totalDuration, awardTitle));
    }

    private System.Collections.IEnumerator InstantWashCoroutine(float ticksPerSecond, float totalDuration, string awardTitle)
    {
        float interval = 1f / Mathf.Max(0.1f, ticksPerSecond);
        float endAt = Time.time + Mathf.Max(0.05f, totalDuration);

        while (Time.time < endAt)
        {
            if (currentDish == null) yield break;

            ApplyInstantWashTick(awardTitle);
            yield return new WaitForSeconds(interval);
        }

        instantWashCoroutine = null;
    }

    private void ApplyInstantWashTick(string awardTitle)
    {
        if (currentDish == null) return;

        // Award title above the dish each wash.
        SpawnInstantWashAwardText(awardTitle);

        int stagesPerClick = upgrades != null ? upgrades.GetCurrentStagesPerClick() : 1;
        stagesPerClick = Mathf.Max(1, stagesPerClick);

        int finalStageIndex = currentDish.stageSprites.Length - 1;

        if (currentClicks < finalStageIndex)
        {
            currentClicks = Mathf.Min(currentClicks + stagesPerClick, finalStageIndex);
            dishVisual?.SetStage(currentClicks);
            return;
        }

        // One more tick past the final stage completes the dish.
        CompleteDishInternal(isManual: false, spawnRewardAtDish: true);
    }

    private void SpawnRewardText(float rewardAmount)
    {
        if (rewardTextPrefab == null) return;
        if (Camera.main == null) return;
        if (rewardAmount <= 0f) return;

        if (!TryGetMouseWorldPosition(out Vector3 worldPos))
            return;

        worldPos.z = rewardTextPlaneZ;
        worldPos += rewardTextOffset;

        GameObject go = Instantiate(rewardTextPrefab, worldPos, Quaternion.identity);
        var floating = go.GetComponent<BubbleRewardText>();
        if (floating != null)
        {
            string formatted = BigNumberFormatter.FormatMoney((double)rewardAmount);
            floating.Initialize("+ " + formatted);
        }
    }

    private void SpawnRewardTextAtWorld(float rewardAmount, Vector3 worldPos)
    {
        if (rewardTextPrefab == null) return;
        if (rewardAmount <= 0f) return;

        worldPos.z = rewardTextPlaneZ;
        worldPos += rewardTextOffset;

        GameObject go = Instantiate(rewardTextPrefab, worldPos, Quaternion.identity);
        var floating = go.GetComponent<BubbleRewardText>();
        if (floating != null)
        {
            string formatted = BigNumberFormatter.FormatMoney((double)rewardAmount);
            floating.Initialize("+ " + formatted);
        }
    }

    private void SpawnInstantWashAwardText(string awardTitle)
    {
        if (rewardTextPrefab == null) return;
        if (string.IsNullOrWhiteSpace(awardTitle)) return;

        if (!TryGetDishWorldPosition(out Vector3 dishWorld))
            return;

        dishWorld.z = rewardTextPlaneZ;
        dishWorld += instantWashAwardTextOffset;

        GameObject go = Instantiate(rewardTextPrefab, dishWorld, Quaternion.identity);
        var floating = go.GetComponent<BubbleRewardText>();
        if (floating != null)
            floating.Initialize(awardTitle);
    }

    private bool TryGetMouseWorldPosition(out Vector3 worldPos)
    {
        worldPos = default;
        if (Camera.main == null) return false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, rewardTextPlaneZ));
        if (plane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }

        worldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));
        worldPos.z = rewardTextPlaneZ;
        return true;
    }

    private bool TryGetDishWorldPosition(out Vector3 worldPos)
    {
        worldPos = default;
        if (dishVisual == null || dishVisual.dishImage == null) return false;
        if (Camera.main == null) return false;

        // Convert the dish UI rect center to a screen point, then to world on the same plane.
        RectTransform rt = dishVisual.dishImage.rectTransform;
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

        Canvas canvas = dishVisual.dishImage.canvas;
        Camera uiCam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = canvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, worldCenter);

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

    // Wash basin logic lives here for now (lowest risk): it only changes how many dishes
    // we award when the dish completes. Stages/clicking remains unchanged.
    private long CalculateManualDishesAwarded()
    {
        int baseIncrement = 1;
        if (ScoreManager.Instance != null)
            baseIncrement = Mathf.Max(1, ScoreManager.Instance.GetDishCountIncrement());

        long awarded = baseIncrement;

        if (sinkManager == null) sinkManager = SinkManager.Instance;
        if (sinkManager != null && sinkManager.CurrentSinkType == SinkManager.SinkType.WashBasin)
        {
            int mult = Mathf.Max(1, sinkManager.GetWashBasinManualMultiplier()); // 2 when basin
            awarded = (long)baseIncrement * mult;

            // These will only matter once those nodes are purchasable.
            awarded += sinkManager.GetWashBasinFlatBonusDishes();

            if (sinkManager.TryRollWashBasinExtraDishes(out int extra))
                awarded += Mathf.Max(0, extra);

            // Overnight Soak doubles the next manual wash (preview-only here).
            if (washBasinSoakTechnique != null)
                awarded = washBasinSoakTechnique.PreviewApplySoak(awarded);
        }

        if (awarded < 1) awarded = 1;
        return awarded;
    }

    private bool ShouldTriggerTurboJetSkillCheck()
    {
        if (sinkManager == null) return false;
        if (string.IsNullOrWhiteSpace(powerWasherTechniqueNodeId)) return false;

        // Must own the technique node.
        if (!sinkManager.IsPurchased(powerWasherTechniqueNodeId))
            return false;

        // Trigger every 30 seconds of holding.
        return holdSeconds >= nextTurboJetSkillCheckAt;
    }

    private void OnTurboJetSkillCheckResolved(bool success)
    {
        isTurboJetSkillCheckActive = false;
        turboJetSkillCheckStartTime = -1f;

        if (success)
            burnEndTime = Time.time + 4f;
    }

    private void BurstBubbles()
    {
        if (sudsOnClick == null) return;
        if (Camera.main == null) return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.nearClipPlane;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        sudsOnClick.BurstBubbles(worldPos);
    }

    private void PlayRandomSqueak()
    {
        if (squeakClips == null || squeakClips.Length == 0) return;

        int index;
        do { index = Random.Range(0, squeakClips.Length); }
        while (index == lastSqueakIndex && squeakClips.Length > 3);

        lastSqueakIndex = index;
        audioSource.PlayOneShot(squeakClips[index]);
        Debug.Log($" Played: {lastSqueakIndex}");
    }
}