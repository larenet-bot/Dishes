using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Central manager for modal-style gameplay menus.
///
/// Add this to one MenuManager object in the scene, then add every menu panel
/// to the Managed Menus array. Each menu can use its own existing open button.
/// The manager watches for any listed menu becoming active, closes the others,
/// closes the active menu when the player clicks outside its main panel, and
/// gives Escape one consistent behavior across the scene.
/// </summary>
public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Serializable]
    public class ManagedMenu
    {
        [Tooltip("Optional label used only for inspector readability and debug logs.")]
        public string menuName;

        [Tooltip("Root GameObject that becomes active when this menu is open.")]
        public GameObject menuRoot;

        [Tooltip("The visible/interactable panel. Clicks outside this RectTransform close the menu.")]
        public RectTransform mainPanel;

        [Tooltip("Close this menu when Escape is pressed.")]
        public bool closeOnEscape = true;

        [Tooltip("Close this menu when the player clicks outside the main panel.")]
        public bool closeOnOutsideClick = true;

        [Tooltip("When this menu opens, close every other managed menu.")]
        public bool closeOtherMenusWhenOpened = true;

        [Tooltip("Allow other menus to close this menu when they open.")]
        public bool canBeClosedByOtherMenus = true;

        [Tooltip("Optional hook for the menu's existing close method, such as CloseEmployeesPanel or CloseMopMenu. If empty, menuRoot.SetActive(false) is used.")]
        public UnityEvent onCloseRequested;

        [Tooltip("Optional hook called once when the manager notices this menu has opened.")]
        public UnityEvent onOpenDetected;

        [NonSerialized] public bool wasOpen;
    }

    [Header("Managed Menus")]
    [SerializeField] private ManagedMenu[] menus = Array.Empty<ManagedMenu>();

    [Header("Pause Menu")]
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private RectTransform pauseMainPanel;
    [SerializeField] private bool escapeOpensPauseWhenNoMenuOpen = true;
    [SerializeField] private bool escapeClosesPauseMenu = true;
    [SerializeField] private bool pauseClosesOnOutsideClick = false;
    [SerializeField] private bool closePauseWhenManagedMenuOpens = true;

    [Header("Input")]
    [SerializeField] private bool handleEscape = true;
    [SerializeField] private bool handleOutsideClick = true;
    [SerializeField] private int mouseButtonForOutsideClick = 0;

    [Header("Debug")]
    [SerializeField] private bool logMenuChanges = false;

    private ManagedMenu activeMenu;
    private bool isClosingMenus;

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
        RefreshOpenStateSnapshot();
        activeMenu = FindLastOpenMenu();

        if (activeMenu != null && activeMenu.closeOtherMenusWhenOpened)
        {
            CloseOtherMenus(activeMenu);
            RefreshOpenStateSnapshot();
        }
    }

    private void Update()
    {
        if (handleEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapePressed();
            return;
        }

        if (handleOutsideClick && Input.GetMouseButtonDown(mouseButtonForOutsideClick))
        {
            HandleOutsideClickPressed();
        }
    }

    private void LateUpdate()
    {
        DetectMenuStateChanges();
    }

    public void OpenMenu(GameObject menuRoot)
    {
        if (menuRoot == null)
        {
            return;
        }

        ManagedMenu managedMenu = FindMenuByRoot(menuRoot);

        if (managedMenu != null)
        {
            OpenMenu(managedMenu);
            return;
        }

        menuRoot.SetActive(true);
    }

    public void CloseMenu(GameObject menuRoot)
    {
        ManagedMenu managedMenu = FindMenuByRoot(menuRoot);

        if (managedMenu != null)
        {
            CloseMenu(managedMenu);
            return;
        }

        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }
    }

    public void ToggleMenu(GameObject menuRoot)
    {
        if (menuRoot == null)
        {
            return;
        }

        if (menuRoot.activeSelf)
        {
            CloseMenu(menuRoot);
        }
        else
        {
            OpenMenu(menuRoot);
        }
    }

    public void CloseActiveMenu()
    {
        ManagedMenu menuToClose = GetActiveOpenMenu();

        if (menuToClose != null)
        {
            CloseMenu(menuToClose);
            return;
        }

        if (IsPauseMenuOpen() && escapeClosesPauseMenu && pauseMenu != null)
        {
            pauseMenu.Resume();
        }
    }

    public void CloseAllMenus()
    {
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i] != null && IsMenuOpen(menus[i]))
            {
                CloseMenu(menus[i]);
            }
        }

        if (IsPauseMenuOpen() && pauseMenu != null)
        {
            pauseMenu.Resume();
        }
    }

    public bool HasOpenManagedMenu()
    {
        return GetActiveOpenMenu() != null;
    }

    public bool HasAnyMenuOpen()
    {
        return HasOpenManagedMenu() || IsPauseMenuOpen();
    }

    private void OpenMenu(ManagedMenu menu)
    {
        if (menu == null || menu.menuRoot == null)
        {
            return;
        }

        menu.menuRoot.SetActive(true);
        SetActiveMenu(menu);
        RefreshOpenStateSnapshot();
    }

    private void CloseMenu(ManagedMenu menu)
    {
        if (menu == null || menu.menuRoot == null)
        {
            return;
        }

        bool wasActuallyOpen = IsMenuOpen(menu);

        isClosingMenus = true;

        if (menu.onCloseRequested != null)
        {
            menu.onCloseRequested.Invoke();
        }

        if (menu.menuRoot != null && menu.menuRoot.activeSelf)
        {
            menu.menuRoot.SetActive(false);
        }

        isClosingMenus = false;

        if (activeMenu == menu)
        {
            activeMenu = null;
        }

        menu.wasOpen = false;

        if (wasActuallyOpen && logMenuChanges)
        {
            Debug.Log($"[MenuManager] Closed menu: {GetMenuLabel(menu)}");
        }
    }

    private void DetectMenuStateChanges()
    {
        if (isClosingMenus || menus == null)
        {
            return;
        }

        ManagedMenu newlyOpened = null;

        for (int i = 0; i < menus.Length; i++)
        {
            ManagedMenu menu = menus[i];

            if (menu == null || menu.menuRoot == null)
            {
                continue;
            }

            bool isOpen = IsMenuOpen(menu);

            if (isOpen && !menu.wasOpen)
            {
                newlyOpened = menu;
            }

            if (!isOpen && menu.wasOpen && activeMenu == menu)
            {
                activeMenu = null;
            }
        }

        if (newlyOpened != null)
        {
            SetActiveMenu(newlyOpened);
        }
        else if (activeMenu == null || !IsMenuOpen(activeMenu))
        {
            activeMenu = FindLastOpenMenu();
        }

        RefreshOpenStateSnapshot();
    }

    private void SetActiveMenu(ManagedMenu menu)
    {
        if (menu == null)
        {
            activeMenu = null;
            return;
        }

        activeMenu = menu;

        if (closePauseWhenManagedMenuOpens && IsPauseMenuOpen() && pauseMenu != null)
        {
            pauseMenu.Resume();
        }

        if (menu.closeOtherMenusWhenOpened)
        {
            CloseOtherMenus(menu);
        }

        if (menu.onOpenDetected != null)
        {
            menu.onOpenDetected.Invoke();
        }

        if (logMenuChanges)
        {
            Debug.Log($"[MenuManager] Active menu: {GetMenuLabel(menu)}");
        }
    }

    private void CloseOtherMenus(ManagedMenu menuToKeepOpen)
    {
        if (menus == null)
        {
            return;
        }

        for (int i = 0; i < menus.Length; i++)
        {
            ManagedMenu other = menus[i];

            if (other == null || other == menuToKeepOpen)
            {
                continue;
            }

            if (!other.canBeClosedByOtherMenus)
            {
                continue;
            }

            if (IsMenuOpen(other))
            {
                CloseMenu(other);
            }
        }
    }

    private void HandleEscapePressed()
    {
        ManagedMenu openMenu = GetActiveOpenMenu();

        if (openMenu != null && openMenu.closeOnEscape)
        {
            CloseMenu(openMenu);
            return;
        }

        if (IsPauseMenuOpen())
        {
            if (escapeClosesPauseMenu && pauseMenu != null)
            {
                pauseMenu.Resume();
            }

            return;
        }

        if (escapeOpensPauseWhenNoMenuOpen && pauseMenu != null)
        {
            pauseMenu.Pause();
        }
    }

    private void HandleOutsideClickPressed()
    {
        ManagedMenu openMenu = GetActiveOpenMenu();

        if (openMenu != null)
        {
            if (!openMenu.closeOnOutsideClick)
            {
                return;
            }

            if (PointerIsInsideMenu(openMenu))
            {
                return;
            }

            CloseMenu(openMenu);
            return;
        }

        if (!pauseClosesOnOutsideClick || !IsPauseMenuOpen() || pauseMenu == null)
        {
            return;
        }

        RectTransform pausePanel = pauseMainPanel;

        if (pausePanel == null && pauseMenu.PauseMenuRoot != null)
        {
            pausePanel = pauseMenu.PauseMenuRoot.GetComponent<RectTransform>();
        }

        if (pausePanel != null && RectTransformContainsPointer(pausePanel))
        {
            return;
        }

        pauseMenu.Resume();
    }

    private bool PointerIsInsideMenu(ManagedMenu menu)
    {
        if (menu == null)
        {
            return false;
        }

        if (menu.mainPanel != null)
        {
            return RectTransformContainsPointer(menu.mainPanel);
        }

        if (menu.menuRoot != null)
        {
            RectTransform rootRect = menu.menuRoot.GetComponent<RectTransform>();

            if (rootRect != null && RectTransformContainsPointer(rootRect))
            {
                return true;
            }
        }

        return PointerRaycastHitsMenu(menu);
    }

    private bool RectTransformContainsPointer(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return false;
        }

        Camera eventCamera = GetEventCamera(rectTransform);
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, eventCamera);
    }

    private Camera GetEventCamera(RectTransform rectTransform)
    {
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();

        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        if (canvas.worldCamera != null)
        {
            return canvas.worldCamera;
        }

        return Camera.main;
    }

    private bool PointerRaycastHitsMenu(ManagedMenu menu)
    {
        if (EventSystem.current == null || menu == null || menu.menuRoot == null)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        for (int i = 0; i < results.Count; i++)
        {
            GameObject hit = results[i].gameObject;

            if (hit != null && hit.transform.IsChildOf(menu.menuRoot.transform))
            {
                return true;
            }
        }

        return false;
    }

    private ManagedMenu GetActiveOpenMenu()
    {
        if (activeMenu != null && IsMenuOpen(activeMenu))
        {
            return activeMenu;
        }

        activeMenu = FindLastOpenMenu();
        return activeMenu;
    }

    private ManagedMenu FindLastOpenMenu()
    {
        ManagedMenu result = null;

        if (menus == null)
        {
            return null;
        }

        for (int i = 0; i < menus.Length; i++)
        {
            ManagedMenu menu = menus[i];

            if (menu != null && IsMenuOpen(menu))
            {
                result = menu;
            }
        }

        return result;
    }

    private ManagedMenu FindMenuByRoot(GameObject menuRoot)
    {
        if (menuRoot == null || menus == null)
        {
            return null;
        }

        for (int i = 0; i < menus.Length; i++)
        {
            ManagedMenu menu = menus[i];

            if (menu != null && menu.menuRoot == menuRoot)
            {
                return menu;
            }
        }

        return null;
    }

    private bool IsMenuOpen(ManagedMenu menu)
    {
        return menu != null && menu.menuRoot != null && menu.menuRoot.activeSelf;
    }

    private bool IsPauseMenuOpen()
    {
        return pauseMenu != null && pauseMenu.IsPauseMenuOpen;
    }

    private void RefreshOpenStateSnapshot()
    {
        if (menus == null)
        {
            return;
        }

        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i] != null)
            {
                menus[i].wasOpen = IsMenuOpen(menus[i]);
            }
        }
    }

    private string GetMenuLabel(ManagedMenu menu)
    {
        if (menu == null)
        {
            return "None";
        }

        if (!string.IsNullOrWhiteSpace(menu.menuName))
        {
            return menu.menuName;
        }

        return menu.menuRoot != null ? menu.menuRoot.name : "Unnamed Menu";
    }
}
