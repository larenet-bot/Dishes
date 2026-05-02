using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JazzSelectedBandSlotUI : MonoBehaviour
{
    [Header("Role")]
    [SerializeField] private PerformerRole role;

    [Header("References")]
    [SerializeField] private JazzBandBookingManager bookingManager;

    [Header("Text")]
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text performerNameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text talentStarsText;

    [Header("Images")]
    [SerializeField] private Image portraitImage;

    [Header("State Objects")]
    [Tooltip("Optional object shown when no performer is selected for this role.")]
    [SerializeField] private GameObject emptyStateObject;

    [Tooltip("Optional object shown when this role has a selected performer.")]
    [SerializeField] private GameObject filledStateObject;

    [Header("Buttons")]
    [SerializeField] private Button removeButton;

    private void Awake()
    {
        if (removeButton != null)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(RemoveSelectedPerformer);
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

    public void SetRole(PerformerRole newRole)
    {
        role = newRole;
        Refresh();
    }

    public void Refresh()
    {
        if (bookingManager == null)
            bookingManager = JazzBandBookingManager.Instance;

        if (roleText != null)
            roleText.text = GetRoleDisplayName(role);

        if (bookingManager == null)
        {
            ShowEmpty();
            return;
        }

        PerformerData selected = bookingManager.GetSelectedForRole(role);

        if (selected == null)
        {
            ShowEmpty();
            return;
        }

        ShowFilled(selected);
    }

    private void ShowEmpty()
    {
        if (emptyStateObject != null)
            emptyStateObject.SetActive(true);

        if (filledStateObject != null)
            filledStateObject.SetActive(false);

        if (performerNameText != null)
            performerNameText.text = "Empty";

        if (costText != null)
            costText.text = "";

        if (talentStarsText != null)
            talentStarsText.text = "";

        if (portraitImage != null)
            portraitImage.enabled = false;

        if (removeButton != null)
            removeButton.interactable = false;
    }

    private void ShowFilled(PerformerData selected)
    {
        if (emptyStateObject != null)
            emptyStateObject.SetActive(false);

        if (filledStateObject != null)
            filledStateObject.SetActive(true);

        if (performerNameText != null)
            performerNameText.text = selected.performerName;

        if (costText != null)
            costText.text = selected.GetFormattedCost();

        if (talentStarsText != null)
            talentStarsText.text = selected.GetStarRatingText();

        if (portraitImage != null)
        {
            if (selected.portrait != null)
            {
                portraitImage.sprite = selected.portrait;
                portraitImage.enabled = true;
            }
            else
            {
                portraitImage.enabled = false;
            }
        }

        if (removeButton != null)
            removeButton.interactable = true;
    }

    private void RemoveSelectedPerformer()
    {
        if (bookingManager == null)
            bookingManager = JazzBandBookingManager.Instance;

        if (bookingManager == null)
            return;

        bookingManager.RemovePerformer(role);
    }

    private string GetRoleDisplayName(PerformerRole roleToName)
    {
        switch (roleToName)
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
                return roleToName.ToString();
        }
    }
}