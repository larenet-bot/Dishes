using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JazzBookingScreenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JazzBandBookingManager bookingManager;

    [Header("Budget Text")]
    [SerializeField] private TMP_Text startingBudgetText;
    [SerializeField] private TMP_Text spentBudgetText;
    [SerializeField] private TMP_Text remainingBudgetText;

    [Header("Band Status Text")]
    [SerializeField] private TMP_Text missingRolesText;
    [SerializeField] private TMP_Text managerNotesText;

    [Header("Optional Debug Preview Text")]
    [Tooltip("Optional. Leave blank if you do not want the player to see raw scoring yet.")]
    [SerializeField] private TMP_Text debugScorePreviewText;

    [Header("Start Show Button")]
    [SerializeField] private Button startShowButton;
    [SerializeField] private TMP_Text startShowButtonText;

    [Header("Selected Band Slots")]
    [SerializeField] private JazzSelectedBandSlotUI[] selectedBandSlots;

    [Header("Options")]
    [Tooltip("Recommended false for normal player-facing UI.")]
    [SerializeField] private bool showDebugScorePreview = false;

    private void Awake()
    {
        if (startShowButton != null)
        {
            startShowButton.onClick.RemoveAllListeners();
            startShowButton.onClick.AddListener(OnStartShowClicked);
        }
    }

    private void OnEnable()
    {
        if (bookingManager == null)
            bookingManager = JazzBandBookingManager.Instance;

        if (bookingManager != null)
        {
            bookingManager.onSelectionChanged.RemoveListener(Refresh);
            bookingManager.onSelectionChanged.AddListener(Refresh);
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (bookingManager != null)
            bookingManager.onSelectionChanged.RemoveListener(Refresh);
    }

    public void Refresh()
    {
        if (bookingManager == null)
            bookingManager = JazzBandBookingManager.Instance;

        if (bookingManager == null)
        {
            SetStartButton(false, "No Manager");
            return;
        }

        RefreshBudget();
        RefreshBandSlots();
        RefreshBandStatus();
        RefreshStartButton();
    }

    private void RefreshBudget()
    {
        if (startingBudgetText != null)
            startingBudgetText.text = $"Budget: {bookingManager.GetStartingBudgetText()}";

        if (spentBudgetText != null)
            spentBudgetText.text = $"Spent: {bookingManager.GetSpentBudgetText()}";

        if (remainingBudgetText != null)
            remainingBudgetText.text = $"Remaining: {bookingManager.GetRemainingBudgetText()}";
    }

    private void RefreshBandSlots()
    {
        if (selectedBandSlots == null)
            return;

        for (int i = 0; i < selectedBandSlots.Length; i++)
        {
            if (selectedBandSlots[i] != null)
                selectedBandSlots[i].Refresh();
        }
    }

    private void RefreshBandStatus()
    {
        JazzPerformanceScoreResult result = bookingManager.GetLatestScoreResult();

        if (missingRolesText != null)
        {
            if (bookingManager.HasCompleteBand())
                missingRolesText.text = "Band complete.";
            else
                missingRolesText.text = $"Still need: {bookingManager.GetMissingRolesText()}";
        }

        if (managerNotesText != null)
            managerNotesText.text = BuildManagerNotes(result);

        if (debugScorePreviewText != null)
        {
            if (showDebugScorePreview && result != null)
            {
                debugScorePreviewText.gameObject.SetActive(true);
                debugScorePreviewText.text =
                    $"Raw Talent: {result.rawTalentScore}\n" +
                    $"Chemistry: {result.GetNetChemistry()}\n" +
                    $"Projected Score: {result.finalPerformanceScore}";
            }
            else
            {
                debugScorePreviewText.gameObject.SetActive(false);
            }
        }
    }

    private void RefreshStartButton()
    {
        if (bookingManager.IsOverBudget())
        {
            SetStartButton(false, "Over Budget");
            return;
        }

        if (!bookingManager.HasCompleteBand())
        {
            SetStartButton(false, "Complete Band");
            return;
        }

        SetStartButton(true, "Start Show");
    }

    private void SetStartButton(bool interactable, string text)
    {
        if (startShowButton != null)
            startShowButton.interactable = interactable;

        if (startShowButtonText != null)
            startShowButtonText.text = text;
    }

    private void OnStartShowClicked()
    {
        if (bookingManager == null)
            bookingManager = JazzBandBookingManager.Instance;

        if (bookingManager == null)
            return;

        JazzPerformanceScoreResult result = bookingManager.FinishBooking();

        if (result == null)
            return;

        Refresh();
    }

    private string BuildManagerNotes(JazzPerformanceScoreResult result)
    {
        if (bookingManager == null)
            return "No manager assigned.";

        if (!bookingManager.HasCompleteBand())
            return "You still need every chair filled before the room opens.";

        if (bookingManager.IsOverBudget())
            return "The numbers do not work. Cut the booking cost before you commit.";

        if (result == null)
            return "No read on the room yet.";

        int netChemistry = result.GetNetChemistry();

        if (result.finalPerformanceScore >= 390)
            return "This lineup looks like a packed house if nobody gets in their own way.";

        if (result.rawTalentScore >= 320 && netChemistry < 0)
            return "Plenty of talent, but the room could turn sharp once the solos start.";

        if (result.rawTalentScore < 230 && netChemistry >= 25)
            return "Not much star power, but this group may hold together better than expected.";

        if (netChemistry >= 40)
            return "The room looks steady. These players should give each other space.";

        if (netChemistry >= 15)
            return "The lineup has some good fits. It should hold if the set starts clean.";

        if (netChemistry <= -40)
            return "This band has problems on paper. Talent may not save the night.";

        if (netChemistry < 0)
            return "There may be tension in the room. Watch who has to share the spotlight.";

        return "The lineup looks workable. Nothing obvious breaks it yet.";
    }
}