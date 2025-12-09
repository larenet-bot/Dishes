using UnityEngine;

[CreateAssetMenu(fileName = "NewCutscene", menuName = "Cutscenes/CutsceneData")]
public class CutsceneData : ScriptableObject
{
    public CutsceneLine[] lines;

    [Header("Global Settings")]
    public AudioClip typingSFX;
    public int typingSFXIntervalChars = 2;
    public float typeSpeed = 0.02f;
    public string nextSceneName = "MainGameScene";
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
}