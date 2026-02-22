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

    private int currentClicks = 0;
    private DishData currentDish;

    // Power washer hold state
    private bool isHolding = false;
    private float holdSeconds = 0f;
    private float holdStageUnits = 0f;

    // Power washer technique state
    private bool turboJetResolvedThisHold = false;
    private bool isTurboJetSkillCheckActive = false;
    private float burnEndTime = -1f;

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
        if (isTurboJetSkillCheckActive)
            return;

        isHolding = true;
        holdSeconds += Time.deltaTime;

        // Turbo Jet Mode: after 30 seconds of holding, prompt a skill check.
        if (!turboJetResolvedThisHold && ShouldTriggerTurboJetSkillCheck())
        {
            if (powerWasherSkillCheckUI != null)
            {
                isTurboJetSkillCheckActive = true;
                powerWasherSkillCheckUI.Begin(OnTurboJetSkillCheckResolved);
                return;
            }

            // No UI assigned. Mark it resolved so we don't spam.
            turboJetResolvedThisHold = true;
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

        while (holdStageUnits >= 1f && currentDish != null)
        {
            holdStageUnits -= 1f;

            if (currentClicks < finalStageIndex)
            {
                currentClicks += 1;
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

        turboJetResolvedThisHold = false;

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
        if (currentDish == null) return;
        if (ScoreManager.Instance == null) return;

        long dishesAwarded = CalculateManualDishesAwarded();

        currentClicks = 0;
        dishVisual?.SetStage(0);

        ScoreManager.Instance.OnDishCleaned(currentDish, dishesAwarded);

        // After a manual completion, let wash basin technique state advance.
        if (washBasinSoakTechnique != null)
            washBasinSoakTechnique.OnManualWashCompleted(dishesAwarded);

        PlayRandomSqueak();
        BurstBubbles();
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

        // Trigger at 30 seconds of holding.
        return holdSeconds >= 30f;
    }

    private void OnTurboJetSkillCheckResolved(bool success)
    {
        isTurboJetSkillCheckActive = false;
        turboJetResolvedThisHold = true;

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
