using UnityEngine;

public class JazzMiniGameManager : MonoBehaviour
{
    public static JazzMiniGameManager Instance { get; private set; }

    [Header("Main UI References")]
    [Tooltip("Root object for the full jazz minigame UI.")]
    [SerializeField] private GameObject jazzUI;

    [Tooltip("Root object for the normal dishwashing game UI.")]
    [SerializeField] private GameObject mainGameUI;

    [Header("Jazz System References")]
    [SerializeField] private JazzBandBookingManager bookingManager;
    [SerializeField] private JazzBookingScreenUI bookingScreenUI;
    [SerializeField] private JazzPerformerCardListUI performerCardListUI;

    [Header("Open Behavior")]
    [Tooltip("Recommended true. Gives the player a clean band selection each time the minigame opens.")]
    [SerializeField] private bool clearBandOnOpen = true;

    [Tooltip("Only turn this on if your card list does not already build itself when enabled.")]
    [SerializeField] private bool rebuildCardsOnOpen = false;

    [Header("Objects To Disable While Open")]
    [Tooltip("Put bubbles, ducks, dish click areas, or other objects here if they overlap or keep interacting while the menu is open.")]
    [SerializeField] private GameObject[] objectsToDisableWhileOpen;

    [Header("Menus To Close When Opening")]
    [Tooltip("Optional. Add shop menus, employee menus, pause subpanels, or other panels that should close before the jazz screen opens.")]
    [SerializeField] private GameObject[] menusToCloseWhenOpening;

    [Header("State")]
    [SerializeField] private bool isActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (bookingManager == null)
            bookingManager = FindFirstObjectByType<JazzBandBookingManager>();

        if (bookingScreenUI == null)
            bookingScreenUI = FindFirstObjectByType<JazzBookingScreenUI>();

        if (performerCardListUI == null)
            performerCardListUI = FindFirstObjectByType<JazzPerformerCardListUI>();
    }

    private void Start()
    {
        if (jazzUI != null)
            jazzUI.SetActive(false);

        if (mainGameUI != null)
            mainGameUI.SetActive(true);

        SetDisabledObjectsActive(true);

        isActive = false;
    }

    public void OpenMiniGame()
    {
        if (isActive)
            return;

        isActive = true;

        CloseOtherMenus();

        if (clearBandOnOpen && bookingManager != null)
            bookingManager.ClearBand(false);

        if (mainGameUI != null)
            mainGameUI.SetActive(false);

        SetDisabledObjectsActive(false);

        if (jazzUI != null)
            jazzUI.SetActive(true);

        if (rebuildCardsOnOpen && performerCardListUI != null)
            performerCardListUI.BuildCards();

        if (bookingScreenUI != null)
            bookingScreenUI.Refresh();

        Debug.Log("[JazzMiniGameManager] Jazz minigame opened.");
    }

    public void CloseMiniGame()
    {
        if (!isActive)
            return;

        isActive = false;

        if (jazzUI != null)
            jazzUI.SetActive(false);

        if (mainGameUI != null)
            mainGameUI.SetActive(true);

        SetDisabledObjectsActive(true);

        Debug.Log("[JazzMiniGameManager] Jazz minigame closed.");
    }

    public void ToggleMiniGame()
    {
        if (isActive)
            CloseMiniGame();
        else
            OpenMiniGame();
    }

    public bool IsActive()
    {
        return isActive;
    }

    private void CloseOtherMenus()
    {
        if (menusToCloseWhenOpening == null)
            return;

        for (int i = 0; i < menusToCloseWhenOpening.Length; i++)
        {
            if (menusToCloseWhenOpening[i] != null)
                menusToCloseWhenOpening[i].SetActive(false);
        }
    }

    private void SetDisabledObjectsActive(bool active)
    {
        if (objectsToDisableWhileOpen == null)
            return;

        for (int i = 0; i < objectsToDisableWhileOpen.Length; i++)
        {
            if (objectsToDisableWhileOpen[i] != null)
                objectsToDisableWhileOpen[i].SetActive(active);
        }
    }
}