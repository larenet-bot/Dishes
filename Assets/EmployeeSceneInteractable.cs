using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach this to an employee's in-scene image or animation object.
/// EmployeeManager also auto-adds/configures this on every object listed in
/// each EmployeeDefinition.firstPurchaseAnimationObjects array.
///
/// UI objects need a raycastable Graphic, such as Image with Raycast Target on.
/// World objects need a Collider2D or Collider for OnMouse events.
/// </summary>
[DisallowMultipleComponent]
public class EmployeeSceneInteractable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Employee Link")]
    [SerializeField] private EmployeeManager employeeManager;
    [SerializeField] private int employeeIndex = -1;

    [Header("Behavior")]
    [Tooltip("When true, this object only responds after the linked employee has been bought at least once.")]
    [SerializeField] private bool onlyRespondWhenEmployeeOwned = true;

    [Tooltip("Hide the tooltip if this object is disabled while hovered.")]
    [SerializeField] private bool hideTooltipOnDisable = true;

    private bool hovering;
    private int lastClickFrame = -1;

    public void Configure(EmployeeManager manager, int index)
    {
        employeeManager = manager;
        employeeIndex = index;
        EnsureUIRaycastTarget();
    }

    private void Awake()
    {
        if (employeeManager == null)
            employeeManager = FindFirstObjectByType<EmployeeManager>();

        EnsureUIRaycastTarget();
    }

    private void OnEnable()
    {
        if (employeeManager == null)
            employeeManager = FindFirstObjectByType<EmployeeManager>();
    }

    private void OnDisable()
    {
        if (hideTooltipOnDisable && hovering)
            HideDescription();

        hovering = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowDescription();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideDescription();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClickSfx();
    }

    private void OnMouseEnter()
    {
        ShowDescription();
    }

    private void OnMouseExit()
    {
        HideDescription();
    }

    private void OnMouseDown()
    {
        PlayClickSfx();
    }

    private void ShowDescription()
    {
        if (!CanRespond())
            return;

        hovering = true;
        employeeManager.ShowEmployeeDescription(employeeIndex);
    }

    private void HideDescription()
    {
        hovering = false;

        if (employeeManager != null)
            employeeManager.ClearDescription();
    }

    private void PlayClickSfx()
    {
        if (lastClickFrame == Time.frameCount)
            return;

        lastClickFrame = Time.frameCount;

        if (!CanRespond())
            return;

        employeeManager.PlayEmployeePurchaseSfx(employeeIndex);
    }

    private bool CanRespond()
    {
        if (employeeManager == null)
            employeeManager = FindFirstObjectByType<EmployeeManager>();

        if (employeeManager == null)
            return false;

        if (!employeeManager.IsValidEmployeeIndex(employeeIndex))
            return false;

        if (onlyRespondWhenEmployeeOwned && !employeeManager.IsEmployeeOwned(employeeIndex))
            return false;

        return true;
    }

    private void EnsureUIRaycastTarget()
    {
        Graphic graphic = GetComponent<Graphic>();

        if (graphic != null)
            graphic.raycastTarget = true;
    }
}
