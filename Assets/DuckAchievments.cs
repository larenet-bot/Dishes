using UnityEngine;

[CreateAssetMenu(menuName = "Duck Menu/Achievement", fileName = "New DuckAchievement")]
public class DuckAchievement : ScriptableObject
{
    [Header("Basic")]
    [Tooltip("Display name for the achievement (e.g. 'Mouse Fan')")]
    [SerializeField] private string achievementName = "New Achievement";

    [Tooltip("Optional description shown to the player")]
    [TextArea(2, 6)]
    [SerializeField] private string description;

    [Header("Linking")]
    [Tooltip("Optional object the achievement is associated with (e.g. an Employee ScriptableObject).")]
    [SerializeField] private UnityEngine.Object linkedObject;

    [Tooltip("Optional key describing what stat this achievement tracks (e.g. 'mouse_employees').")]
    [SerializeField] private string linkedKey;

    [Tooltip("How many of the linked item/stat are required to unlock this achievement.")]
    [SerializeField] private int requiredAmount = 1;

    [Header("UI")]
    [Tooltip("Icon shown in the duck menu")]
    [SerializeField] private Sprite icon;

    // Read-only accessors
    public string AchievementName => achievementName;
    public string Description => description;
    public UnityEngine.Object LinkedObject => linkedObject;
    public string LinkedKey => linkedKey;
    public int RequiredAmount => requiredAmount;
    public Sprite Icon => icon;
}