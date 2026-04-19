
using UnityEngine;

/// <summary>
/// Handles power-washer hold-to-clean behavior, Turbo Jet skill-checks and momentum.
/// This component is separated from DishClicker; it calls back into DishClicker to
/// apply stage-units when the washer accumulates them.
/// </summary>
public class PowerWasherController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("DishClicker owner to apply stage progress to.")]
    public DishClicker dishClicker;

    [Tooltip("Optional direct reference. Falls back to SinkManager.Instance at runtime.")]
    public SinkManager sinkManager;

    [Header("Hold Settings")]
    [Tooltip("Enables hold-to-clean behavior when the active sink is a power washer.")]
    public bool enablePowerWasherHold = true;

    [Tooltip("When enabled, hold input only counts while the pointer is over the dish image.")]
    public bool requirePointerOverDishImage = true;

    [Header("Technique / Skill Check")]
    [Tooltip("Optional Turbo Jet skill check UI used by the power washer.")]
    public PowerWasherSkillCheckUI powerWasherSkillCheckUI;

    [Tooltip("Node ID used to check whether the Turbo Jet technique has been purchased.")]
    public string powerWasherTechniqueNodeId = "pw_technique";

    // Internal state
    private bool isHolding;
    private float holdSeconds;
    private float holdStageUnits;

    private bool isTurboJetSkillCheckActive;
    private float burnEndTime = -1f;
    private float nextTurboJetSkillCheckAt = 30f;
    private float turboJetSkillCheckStartTime = -1f;

    public void Initialize(DishClicker owner)
    {
        dishClicker = owner ?? dishClicker;
        if (sinkManager == null)
            sinkManager = SinkManager.Instance;
    }

    public void OnDishAssigned(DishData data)
    {
        // Reset per-dish hold state when a new dish is assigned.
        ResetHoldState();
    }

    private void Update()
    {
        if (!enablePowerWasherHold || dishClicker == null)
        {
            return;
        }

        if (dishClicker.GetCurrentDishData() == null)
        {
            ResetHoldState();
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

        // Consume whole units and send them to DishClicker
        if (holdStageUnits >= 1f)
        {
            int whole = Mathf.FloorToInt(holdStageUnits);
            holdStageUnits -= whole;

            if (dishClicker != null)
            {
                dishClicker.ApplyStageUnitsFromPowerWasher(whole);
            }
        }
    }

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
        if (sinkManager == null)
            sinkManager = SinkManager.Instance;

        float stagesPerSecond = sinkManager != null ? sinkManager.GetPowerWasherBaseStagesPerSecond() : 2f;
        stagesPerSecond *= (sinkManager != null) ? sinkManager.GetPowerWasherNozzleMultiplier() : 1f;

        if (Time.time < burnEndTime)
        {
            stagesPerSecond *= 2f;
        }

        if (sinkManager != null && sinkManager.HasPowerWasherMomentum())
        {
            sinkManager.GetPowerWasherMomentumSettings(out float startAfter, out float perSecondBonus, out float maxBonus);

            float elapsedAfterThreshold = Mathf.Max(0f, holdSeconds - startAfter);
            int stacks = Mathf.FloorToInt(elapsedAfterThreshold);
            float bonus = Mathf.Min(stacks * perSecondBonus, maxBonus);

            stagesPerSecond *= 1f + bonus;
        }

        return stagesPerSecond;
    }

    private void ResetHoldState()
    {
        if (!isHolding && !isTurboJetSkillCheckActive)
        {
            holdStageUnits = 0f;
            holdSeconds = 0f;
            nextTurboJetSkillCheckAt = 30f;
            turboJetSkillCheckStartTime = -1f;
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
        if (dishClicker == null || dishClicker.dishVisual == null || dishClicker.dishVisual.dishImage == null)
        {
            return true;
        }

        Canvas canvas = dishClicker.dishVisual.dishImage.canvas;
        Camera canvasCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera = canvas.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            dishClicker.dishVisual.dishImage.rectTransform,
            Input.mousePosition,
            canvasCamera);
    }
}
