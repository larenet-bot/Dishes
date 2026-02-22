using UnityEngine;

[CreateAssetMenu(fileName = "NewCutscene", menuName = "Cutscenes/CutsceneData")]
public class CutsceneData : ScriptableObject
{
    public CutsceneLine[] lines;

    [Header("Global Settings")]
    public AudioClip typingSFX;
    public int typingSFXIntervalChars = 2;
    public float typeSpeed = 0.02f;
    public string nextSceneName = "Game";
}

[System.Serializable]
public class DialogueChoice
{
    [TextArea(2, 3)]
    public string choiceText;

    [Tooltip("Index in the lines array to jump to")]
    public int nextLineIndex;

    [Tooltip("Optional: force the branch id to switch to when this choice is selected. Set to -1 to use the branchId of the target line.")]
    public int nextBranchId = -1;
}

[System.Serializable]
public class CutsceneLine
{
    [TextArea(2, 5)]
    public string text;

    [Tooltip("Optional sound to play when this line is shown")]
    public AudioClip sfx;

    [Tooltip("Optional background sprite to switch to for this line. Leave null to keep previous.")]
    public Sprite background;

    [Tooltip("Optional additional pause after the line finishes (seconds).")]
    public float postDelay = 0f;

    [Header("Branching")]
    [Tooltip("Branch id for this line. Use 0 for main/default branch. Lines in other branches will not affect background/legacy branch logic unless the manager's current branch matches.")]
    public int branchId = 0;

    [Header("Choices (Optional)")]
    public bool hasChoices;
    public DialogueChoice[] choices;


    [Header("Flow Control")]
    [Tooltip("If >= 0, cutscene will jump to this index instead of going to the next line.")]
    public int overrideNextLineIndex = -1;

    
}
