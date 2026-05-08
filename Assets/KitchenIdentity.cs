using UnityEngine;

/// <summary>
/// Optional helper. Add this to one GameObject in a kitchen scene only if SaveManager
/// cannot resolve the kitchen id from LoanManager.
/// </summary>
public class KitchenIdentity : MonoBehaviour
{
    [SerializeField] private string kitchenId = "kitchen_1";

    public string KitchenId => kitchenId;
}
