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

    [Tooltip("Optional global background track to play for this cutscene. Per-line track (if provided) overrides this.")]
    public Track backgroundTrack;
}

public enum FadeMode
{
    None = 0,
    FadeOutIn = 1,   // fade to black, optional hold, then fade back in after advancing
    FadeOutOnly = 2, // fade to black and remain black (no fade back in)
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

    public AudioClip sfx;
    public Sprite background;
    public Track backgroundTrack;
    public float postDelay = 0f;

    [Header("Screen Fade")]
    [Tooltip("Which fade (if any) should occur before advancing to the next line.")]
    public FadeMode fadeMode = FadeMode.None;

    [Tooltip("How long the fade takes.")]
    public float fadeDuration = 1f;

    [Tooltip("How long to stay black before fading back in. Only applied for FadeOutIn.")]
    public float blackScreenTime = 0f;

    [Header("Branching")]
    public int branchId = 0;

    [Header("Choices (Optional)")]
    public bool hasChoices;
    public DialogueChoice[] choices;

    [Header("Flow Control")]
    public int overrideNextLineIndex = -1;
}