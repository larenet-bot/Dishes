using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class JazzBandBookingManager : MonoBehaviour
{
    public static JazzBandBookingManager Instance { get; private set; }

    [Header("Budget")]
    [Tooltip("This is the fake booking budget for the minigame, not the player's real profit.")]
    [Min(0f)]
    public float startingBudget = 1500f;

    [Header("Available Performers")]
    [Tooltip("Drag all performer assets available for this gig into this list.")]
    public List<PerformerData> availablePerformers = new List<PerformerData>();

    [Header("Selected Band")]
    [SerializeField] private PerformerData selectedDrummer;
    [SerializeField] private PerformerData selectedBassist;
    [SerializeField] private PerformerData selectedSinger;
    [SerializeField] private PerformerData selectedSaxPlayer;

    [Header("Latest Score Preview")]
    [SerializeField] private int rawTalentPreview;
    [SerializeField] private int chemistryBonusPreview;
    [SerializeField] private int chemistryPenaltyPreview;
    [SerializeField] private int finalScorePreview;
    [SerializeField] private bool bandIsComplete;

    [Header("Events")]
    [Tooltip("UI can listen to this later so it refreshes whenever the band changes.")]
    public UnityEvent onSelectionChanged;

    [Tooltip("UI can listen to this later to show the result screen.")]
    public UnityEvent onBookingFinished;

    [Header("Debug")]
    public bool logDebugMessages = true;

    [Tooltip("Useful if you want a clean booking screen every time the minigame opens.")]
    public bool clearSelectionOnStart = true;

    [Header("Debug Test Band")]
    public PerformerData debugDrummer;
    public PerformerData debugBassist;
    public PerformerData debugSinger;
    public PerformerData debugSaxPlayer;

    private JazzPerformanceScoreResult latestScoreResult;

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
        if (clearSelectionOnStart)
        {
            ClearBand(false);
        }
        else
        {
            UpdateScorePreview();
        }
    }

    public bool SelectPerformer(PerformerData performer)
    {
        if (performer == null)
        {
            Debug.LogWarning("[JazzBandBookingManager] Tried to select a null performer.");
            return false;
        }

        PerformerData currentlySelectedForRole = GetSelectedForRole(performer.role);

        if (currentlySelectedForRole == performer)
        {
            if (logDebugMessages)
                Debug.Log($"[JazzBandBookingManager] {performer.performerName} is already selected.");

            return true;
        }

        if (!CanAffordPerformer(performer))
        {
            if (logDebugMessages)
            {
                Debug.Log(
                    $"[JazzBandBookingManager] Cannot afford {performer.performerName}. " +
                    $"Cost: {BigNumberFormatter.FormatMoney(performer.cost)}, " +
                    $"Remaining after swap would be below zero."
                );
            }

            return false;
        }

        SetSelectedForRole(performer.role, performer);

        if (logDebugMessages)
        {
            Debug.Log(
                $"[JazzBandBookingManager] Selected {performer.performerName} as {performer.GetRoleDisplayName()}."
            );
        }

        UpdateScorePreview();
        onSelectionChanged?.Invoke();

        return true;
    }

    public void RemovePerformer(PerformerRole role)
    {
        PerformerData removed = GetSelectedForRole(role);

        SetSelectedForRole(role, null);

        if (logDebugMessages && removed != null)
        {
            Debug.Log($"[JazzBandBookingManager] Removed {removed.performerName} from {GetRoleDisplayName(role)}.");
        }

        UpdateScorePreview();
        onSelectionChanged?.Invoke();
    }

    public void ClearBand()
    {
        ClearBand(true);
    }

    public void ClearBand(bool invokeEvent)
    {
        selectedDrummer = null;
        selectedBassist = null;
        selectedSinger = null;
        selectedSaxPlayer = null;

        UpdateScorePreview();

        if (invokeEvent)
            onSelectionChanged?.Invoke();

        if (logDebugMessages)
            Debug.Log("[JazzBandBookingManager] Band cleared.");
    }

    public bool CanAffordPerformer(PerformerData performer)
    {
        if (performer == null)
            return false;

        PerformerData currentlySelectedForRole = GetSelectedForRole(performer.role);

        float currentRoleCost = currentlySelectedForRole != null
            ? currentlySelectedForRole.cost
            : 0f;

        float projectedSpent =
            GetSpentBudget() -
            currentRoleCost +
            performer.cost;

        return projectedSpent <= startingBudget;
    }

    public bool IsSelected(PerformerData performer)
    {
        if (performer == null)
            return false;

        return selectedDrummer == performer ||
               selectedBassist == performer ||
               selectedSinger == performer ||
               selectedSaxPlayer == performer;
    }

    public bool HasCompleteBand()
    {
        return selectedDrummer != null &&
               selectedBassist != null &&
               selectedSinger != null &&
               selectedSaxPlayer != null;
    }

    public bool IsOverBudget()
    {
        return GetSpentBudget() > startingBudget;
    }

    public float GetSpentBudget()
    {
        float spent = 0f;

        if (selectedDrummer != null)
            spent += selectedDrummer.cost;

        if (selectedBassist != null)
            spent += selectedBassist.cost;

        if (selectedSinger != null)
            spent += selectedSinger.cost;

        if (selectedSaxPlayer != null)
            spent += selectedSaxPlayer.cost;

        return spent;
    }

    public float GetRemainingBudget()
    {
        return startingBudget - GetSpentBudget();
    }

    public string GetStartingBudgetText()
    {
        return BigNumberFormatter.FormatMoney(startingBudget);
    }

    public string GetSpentBudgetText()
    {
        return BigNumberFormatter.FormatMoney(GetSpentBudget());
    }

    public string GetRemainingBudgetText()
    {
        return BigNumberFormatter.FormatMoney(GetRemainingBudget());
    }

    public PerformerData GetSelectedForRole(PerformerRole role)
    {
        switch (role)
        {
            case PerformerRole.Drummer:
                return selectedDrummer;

            case PerformerRole.Bassist:
                return selectedBassist;

            case PerformerRole.Singer:
                return selectedSinger;

            case PerformerRole.SaxPlayer:
                return selectedSaxPlayer;

            default:
                return null;
        }
    }

    public string GetSelectedNameForRole(PerformerRole role)
    {
        PerformerData selected = GetSelectedForRole(role);

        if (selected == null)
            return "Empty";

        return selected.performerName;
    }

    public List<PerformerData> GetSelectedPerformers()
    {
        List<PerformerData> selected = new List<PerformerData>();

        if (selectedDrummer != null)
            selected.Add(selectedDrummer);

        if (selectedBassist != null)
            selected.Add(selectedBassist);

        if (selectedSinger != null)
            selected.Add(selectedSinger);

        if (selectedSaxPlayer != null)
            selected.Add(selectedSaxPlayer);

        return selected;
    }

    public JazzPerformanceScoreResult GetLatestScoreResult()
    {
        if (latestScoreResult == null)
        {
            UpdateScorePreview();
        }

        return latestScoreResult;
    }

    public JazzPerformanceScoreResult CalculateCurrentScore(bool logDebug = false)
    {
        latestScoreResult = JazzPerformanceScorer.CalculateScore(GetSelectedPerformers(), logDebug);
        CopyScoreResultToInspectorFields();
        return latestScoreResult;
    }

    public JazzPerformanceScoreResult FinishBooking()
    {
        JazzPerformanceScoreResult result = CalculateCurrentScore(true);

        if (!result.hasCompleteBand)
        {
            if (logDebugMessages)
            {
                Debug.LogWarning(
                    "[JazzBandBookingManager] Cannot finish booking. Missing: " + GetMissingRolesText()
                );
            }

            return result;
        }

        if (IsOverBudget())
        {
            if (logDebugMessages)
            {
                Debug.LogWarning("[JazzBandBookingManager] Cannot finish booking. Band is over budget.");
            }

            return result;
        }

        if (logDebugMessages)
        {
            Debug.Log(
                $"[JazzBandBookingManager] Booking finished. Final Performance Score: {result.finalPerformanceScore}"
            );
        }

        onBookingFinished?.Invoke();

        return result;
    }

    public string GetMissingRolesText()
    {
        JazzPerformanceScoreResult result = GetLatestScoreResult();

        if (result == null || result.missingRoles == null || result.missingRoles.Count == 0)
            return "None";

        string text = "";

        for (int i = 0; i < result.missingRoles.Count; i++)
        {
            text += GetRoleDisplayName(result.missingRoles[i]);

            if (i < result.missingRoles.Count - 1)
                text += ", ";
        }

        return text;
    }

    public string GetRoleDisplayName(PerformerRole role)
    {
        switch (role)
        {
            case PerformerRole.Drummer:
                return "Drummer";

            case PerformerRole.Bassist:
                return "Bass Player";

            case PerformerRole.Singer:
                return "Singer";

            case PerformerRole.SaxPlayer:
                return "Sax Player";

            default:
                return role.ToString();
        }
    }

    private void SetSelectedForRole(PerformerRole role, PerformerData performer)
    {
        switch (role)
        {
            case PerformerRole.Drummer:
                selectedDrummer = performer;
                break;

            case PerformerRole.Bassist:
                selectedBassist = performer;
                break;

            case PerformerRole.Singer:
                selectedSinger = performer;
                break;

            case PerformerRole.SaxPlayer:
                selectedSaxPlayer = performer;
                break;
        }
    }

    private void UpdateScorePreview()
    {
        latestScoreResult = JazzPerformanceScorer.CalculateScore(GetSelectedPerformers(), false);
        CopyScoreResultToInspectorFields();
    }

    private void CopyScoreResultToInspectorFields()
    {
        if (latestScoreResult == null)
        {
            rawTalentPreview = 0;
            chemistryBonusPreview = 0;
            chemistryPenaltyPreview = 0;
            finalScorePreview = 0;
            bandIsComplete = false;
            return;
        }

        rawTalentPreview = latestScoreResult.rawTalentScore;
        chemistryBonusPreview = latestScoreResult.chemistryBonus;
        chemistryPenaltyPreview = latestScoreResult.chemistryPenalty;
        finalScorePreview = latestScoreResult.finalPerformanceScore;
        bandIsComplete = latestScoreResult.hasCompleteBand;
    }

    [ContextMenu("Debug: Select Test Band")]
    private void DebugSelectTestBand()
    {
        ClearBand(false);

        SelectPerformer(debugDrummer);
        SelectPerformer(debugBassist);
        SelectPerformer(debugSinger);
        SelectPerformer(debugSaxPlayer);

        CalculateCurrentScore(true);
    }

    [ContextMenu("Debug: Finish Booking")]
    private void DebugFinishBooking()
    {
        FinishBooking();
    }

    [ContextMenu("Debug: Clear Band")]
    private void DebugClearBand()
    {
        ClearBand();
    }
}