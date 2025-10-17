using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text dialogueText;
    public Button nextButton;
    public Image backgroundImage;

    [Header("Dialogue Settings")]
    [TextArea(2, 5)]
    public string[] dialogueLines;

    [Header("Background Settings")]
    public Sprite[] backgroundSprites;          // Sprites for backgrounds
    public int[] backgroundChangeIndices;       // Line numbers where each background changes

    [Header("Scene Transition")]
    public string nextSceneName = "MainGameScene";

    private int currentLine = 0;
    private bool isTyping = false;
    private int currentBackgroundIndex = -1;

    private void Start()
    {
        nextButton.onClick.AddListener(OnNextClicked);
        ShowLine();
        UpdateBackground();
    }

    private void ShowLine()
    {
        StopAllCoroutines();
        StartCoroutine(TypeText(dialogueLines[currentLine]));
        UpdateBackground();
    }

    private IEnumerator TypeText(string line)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.02f);
        }
        isTyping = false;
    }

    private void OnNextClicked()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = dialogueLines[currentLine];
            isTyping = false;
            return;
        }

        currentLine++;

        if (currentLine >= dialogueLines.Length)
        {
            StartCoroutine(EndCutscene());
        }
        else
        {
            ShowLine();
        }
    }

    private void UpdateBackground()
    {
        for (int i = 0; i < backgroundChangeIndices.Length; i++)
        {
            if (currentLine >= backgroundChangeIndices[i])
            {
                // Update only if background index changes
                if (currentBackgroundIndex != i)
                {
                    currentBackgroundIndex = i;
                    backgroundImage.sprite = backgroundSprites[i];
                }
            }
        }
    }

    private IEnumerator EndCutscene()
    {
        dialogueText.text = "Loading...";
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(nextSceneName);
    }
}

/*
    * HOW TO MATCH BACKGROUNDS TO DIALOGUE:
    * -------------------------------------
    * example:
    * 0: "Welcome to Dish Duty!"
    * 1: "You just got hired as a dishwasher."
    * 2: "The manager glares at you."
    * 3: "Time to clean some dishes."
    *
    *  then assign:
    * 
    * backgroundSprites[0] = introPosterSprite
    * backgroundSprites[1] = playerSprite
    * backgroundSprites[2] = managerSprite
    *
    * And your backgroundChangeIndices array would be:
    * [0, 1, 2]
    *
    * Meaning:
    * - At dialogue line 0 -> show introPosterSprite
    * - At dialogue line 1 -> switch to playerSprite
    * - At dialogue line 2 -> switch to managerSprite
    *
    * IMPORTANT:
    * The order of backgroundSprites and backgroundChangeIndices must match.
    * Index 0 in both arrays is linked together, same with index 1, 2, etc.
    */