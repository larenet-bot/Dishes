using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JazzPerformerCardUI : MonoBehaviour
{
    [Header("Main References")]
    [SerializeField] private PerformerData performerData;
    [SerializeField] private JazzBandBookingManager bookingManager;

    [Header("Text References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text talentStarsText;

    [Header("Scouting Report Text")]
    [SerializeField] private TMP_Text temperamentText;
    [SerializeField] private TMP_Text worksWellWithText;
    [SerializeField] private TMP_Text watchOutForText;

    [Header("Images")]
    [Tooltip("The Image component used as the card background.")]
    [SerializeField] private Image backgroundImage;

    [SerializeField] private Image portraitImage;

    [Tooltip("Optional image or object used to show that this card is selected.")]
    [SerializeField] private GameObject selectedMarker;

    [Tooltip("Optional dark overlay shown when the performer cannot be afforded.")]
    [SerializeField] private GameObject unaffordableOverlay;

    [Header("Role Colors")]
    [SerializeField] private Color drummerColor = new Color(0.55f, 0.18f, 0.18f);
    [SerializeField] private Color bassistColor = new Color(0.18f, 0.32f, 0.55f);
    [SerializeField] private Color singerColor = new Color(0.45f, 0.22f, 0.55f);
    [SerializeField] private Color saxPlayerColor = new Color(0.55f, 0.42f, 0.16f);
    [SerializeField] private Color fallbackColor = new Color(0.25f, 0.25f, 0.25f);

    [Header("Button")]
    [SerializeField] private Button selectButton;
    [SerializeField] private TMP_Text selectButtonText;

    [Header("Optional Fade")]
    [Tooltip("Optional CanvasGroup on the card root. Used to fade unaffordable cards.")]
    [SerializeField] private CanvasGroup cardCanvasGroup;

    [Header("Visual Settings")]
    [Range(0.1f, 1f)]
    [SerializeField] private float unaffordableAlpha = 0.55f;

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        WireButton();
    }

    private void OnEnable()
    {
        SubscribeToManagerEvent();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromManagerEvent();
    }

    public void Initialize(PerformerData data, JazzBandBookingManager manager)
    {
        performerData = data;
        bookingManager = manager;

        WireButton();
        SubscribeToManagerEvent();
        Refresh();
    }

    private void WireButton()
    {
        if (selectButton == null)
            return;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelectClicked);
    }

    private void SubscribeToManagerEvent()
    {
        if (bookingManager == null)
            bookingManager = JazzBandBookingManager.Instance;

        if (bookingManager == null)
            return;

        bookingManager.onSelectionChanged.RemoveListener(Refresh);
        bookingManager.onSelectionChanged.AddListener(Refresh);
    }

    private void UnsubscribeFromManagerEvent()
    {
        if (bookingManager == null)
            return;

        bookingManager.onSelectionChanged.RemoveListener(Refresh);
    }

    private void OnSelectClicked()
    {
        if (performerData == null)
            return;

        if (bookingManager == null)
            bookingManager = JazzBandBookingManager.Instance;

        if (bookingManager == null)
        {
            Debug.LogWarning("[JazzPerformerCardUI] No JazzBandBookingManager found.");
            return;
        }

        bookingManager.SelectPerformer(performerData);
    }

    public void Refresh()
    {
        if (performerData == null)
        {
            ClearCard();
            return;
        }

        if (bookingManager == null)
            bookingManager = JazzBandBookingManager.Instance;

        RefreshText();
        RefreshPortrait();
        RefreshRoleColor();
        RefreshSelectionState();
    }

    private void RefreshText()
    {
        if (nameText != null)
            nameText.text = performerData.performerName;

        if (roleText != null)
            roleText.text = performerData.GetRoleDisplayName();

        if (costText != null)
            costText.text = $"Cost: {performerData.GetFormattedCost()}";

        if (talentStarsText != null)
            talentStarsText.text = $"Talent: {performerData.GetStarRatingText()}";

        if (temperamentText != null)
            temperamentText.text = performerData.temperamentText;

        if (worksWellWithText != null)
            worksWellWithText.text = performerData.worksWellWithText;

        if (watchOutForText != null)
            watchOutForText.text = performerData.watchOutForText;
    }

    private void RefreshPortrait()
    {
        if (portraitImage == null)
            return;

        if (performerData.portrait != null)
        {
            portraitImage.sprite = performerData.portrait;
            portraitImage.enabled = true;
        }
        else
        {
            portraitImage.enabled = false;
        }
    }

    private void RefreshRoleColor()
    {
        if (backgroundImage == null || performerData == null)
            return;

        backgroundImage.color = GetColorForRole(performerData.role);
    }

    private Color GetColorForRole(PerformerRole role)
    {
        switch (role)
        {
            case PerformerRole.Drummer:
                return drummerColor;

            case PerformerRole.Bassist:
                return bassistColor;

            case PerformerRole.Singer:
                return singerColor;

            case PerformerRole.SaxPlayer:
                return saxPlayerColor;

            default:
                return fallbackColor;
        }
    }

    private void RefreshSelectionState()
    {
        bool hasManager = bookingManager != null;
        bool isSelected = hasManager && bookingManager.IsSelected(performerData);
        bool canAfford = hasManager && bookingManager.CanAffordPerformer(performerData);

        PerformerData currentlySelectedForRole = null;

        if (hasManager)
            currentlySelectedForRole = bookingManager.GetSelectedForRole(performerData.role);

        bool roleAlreadyFilledBySomeoneElse =
            currentlySelectedForRole != null &&
            currentlySelectedForRole != performerData;

        if (selectedMarker != null)
            selectedMarker.SetActive(isSelected);

        if (unaffordableOverlay != null)
            unaffordableOverlay.SetActive(!isSelected && !canAfford);

        if (cardCanvasGroup != null)
            cardCanvasGroup.alpha = (!isSelected && !canAfford) ? unaffordableAlpha : 1f;

        if (selectButton != null)
            selectButton.interactable = !isSelected && canAfford;

        if (selectButtonText != null)
        {
            if (isSelected)
                selectButtonText.text = "Selected";
            else if (!canAfford)
                selectButtonText.text = "Too Expensive";
            else if (roleAlreadyFilledBySomeoneElse)
                selectButtonText.text = "Replace";
            else
                selectButtonText.text = "Select";
        }
    }

    private void ClearCard()
    {
        if (backgroundImage != null)
            backgroundImage.color = fallbackColor;

        if (nameText != null)
            nameText.text = "No Performer";

        if (roleText != null)
            roleText.text = "";

        if (costText != null)
            costText.text = "";

        if (talentStarsText != null)
            talentStarsText.text = "";

        if (temperamentText != null)
            temperamentText.text = "";

        if (worksWellWithText != null)
            worksWellWithText.text = "";

        if (watchOutForText != null)
            watchOutForText.text = "";

        if (portraitImage != null)
            portraitImage.enabled = false;

        if (selectedMarker != null)
            selectedMarker.SetActive(false);

        if (unaffordableOverlay != null)
            unaffordableOverlay.SetActive(false);

        if (selectButton != null)
            selectButton.interactable = false;

        if (selectButtonText != null)
            selectButtonText.text = "Unavailable";
    }

    public PerformerData GetPerformerData()
    {
        return performerData;
    }
}