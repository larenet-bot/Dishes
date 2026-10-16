using UnityEngine;

[CreateAssetMenu(
    fileName = "GuidedIntroStep",
    menuName = "Guided Intro/Intro Step"
)]
public class GuidedIntroStep : ScriptableObject
{
    [Header("Dialogue")]
    [Tooltip("Name of the character speaking.")]
    public string speakerName = "Duck";

    [TextArea(3, 6)]
    [Tooltip("What the Duck says to the player.")]
    public string dialogueText;

    [Header("Player Instruction")]
    [Tooltip("What the player is being asked to do.")]
    public string instructionText;

    [Header("Completion")]
    [Tooltip("What the player needs to do before this step is completed.")]
    public CompletionType completionType;

    [Tooltip("The number of times the required action must happen before this step is completed.")]
    [Min(1)]
    public int requiredActions = 1;

    [Tooltip("The object involved in completing this step, if required.")]
    public GameObject targetObject;

    [Header("Optional")]
    [Tooltip("Should the target object be highlighted while this step is active?")]
    public bool highlightTarget = true;
}

public enum CompletionType
{
    None,
    DishClicked,
    MoneyEarned,
    ButtonClicked,
    MenuOpened,
    UpgradePurchased,
    SinkPurchased,
    EmployeePurchased,
    MovementPerformed,
    AchievementUnlocked
}