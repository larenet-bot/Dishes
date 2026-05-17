using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Dishes/Achievement")]
public class AchievementData : ScriptableObject
{
    public string id; // unique id, e.g. "first_dish"
    public string title;
    [TextArea] public string description;

    public enum TriggerType { TotalDishes, TotalProfit, EmployeeCount, ProfitPerDish, Custom, EmployeeTypeCount }
    public TriggerType trigger = TriggerType.TotalDishes;

    // threshold used for the trigger types above
    public double threshold = 1.0;

    // Optional: visible in UI even before unlocking
    public bool hidden = false;

    // When using TriggerType.EmployeeTypeCount, set this to the employee name (matches EmployeeDefinition.employeeName).
    public string employeeTypeName = "";

    // Runtime: ScriptableObject must not hold unlocked state (saved separately)
}