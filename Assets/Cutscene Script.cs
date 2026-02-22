using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneManager : MonoBehaviour
{
    [Header("Choice UI")]
    public GameObject choicesPanel;
    public Button[] choiceButtons;


    [Header("Data (preferred)")]
    public CutsceneData cutsceneData;

    [Header("UI References")]
    public TMP_Text dialogueText;
    public Button nextButton;
    public Image backgroundImage;

    [Header("Legacy / Fallback fields (optional)")]
    [TextArea(2, 5)]
    public string[] dialogueLines;
    public AudioClip[] dialogueSFX;
    public AudioClip typingSFX;
    public int typingSFXIntervalChars = 2;
    public Sprite[] backgroundSprites;
    public int[] backgroundChangeIndices;
    public string nextSceneName = "Game";

    [Header("Typewriter")]
    public float typeSpeed = 0.02f;

    private int currentLine = 0;
    private bool isTyping = false;

    private void Start()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        // If a CutsceneData asset is provided, adopt its defaults
        if (cutsceneData != null)
        {
            typingSFX = cutsceneData.typingSFX;
            typingSFXIntervalChars = Mathf.Max(1, cutsceneData.typingSFXIntervalChars);
            typeSpeed = Mathf.Max(0.001f, cutsceneData.typeSpeed);
            nextSceneName = cutsceneData.nextSceneName;
        }

        UpdateBackground();
        ShowLine();
    }

    private void ShowLine()
    {
        StopAllCoroutines();
        StartCoroutine(TypeText(GetLineText(currentLine)));

        AudioClip sfx = GetLineSFX(currentLine);
        if (AudioManager.instance != null && sfx != null)
            AudioManager.instance.PlaySFX(sfx);

        UpdateBackground();

        SetupChoices();
    }
    private void SetupChoices()
    {
        if (cutsceneData == null || cutsceneData.lines == null)
            return;

        CutsceneLine line = cutsceneData.lines[currentLine];

        if (line.hasChoices && line.choices != null && line.choices.Length > 0)
        {
            nextButton.gameObject.SetActive(false);
            choicesPanel.SetActive(true);

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < line.choices.Length)
                {
                    int choiceIndex = i;

                    choiceButtons[i].gameObject.SetActive(true);
                    choiceButtons[i].GetComponentInChildren<TMP_Text>().text =
                        line.choices[i].choiceText;

                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() =>
                    {
                        OnChoiceSelected(line.choices[choiceIndex].nextLineIndex);
                    });
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            choicesPanel.SetActive(false);
            nextButton.gameObject.SetActive(true);
        }
    }

    private void OnChoiceSelected(int nextIndex)
    {
        choicesPanel.SetActive(false);
        nextButton.gameObject.SetActive(true);

        currentLine = nextIndex;
        ShowLine();
    }


    private IEnumerator TypeText(string line)
    {
        isTyping = true;
        dialogueText.text = "";
        int charCount = 0;
        foreach (char c in line)
        {
            dialogueText.text += c;
            charCount++;

            if (typingSFX != null && AudioManager.instance != null && typingSFXIntervalChars > 0)
            {
                if ((charCount % typingSFXIntervalChars) == 0)
                    AudioManager.instance.PlaySFX(typingSFX);
            }

            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;
    }

    private void OnNextClicked()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = GetLineText(currentLine);
            isTyping = false;
            return;
        }

        if (cutsceneData != null &&
            cutsceneData.lines[currentLine].hasChoices)
        {
            return;
        }

        CutsceneLine line = cutsceneData.lines[currentLine];

        if (line.overrideNextLineIndex >= 0)
        {
            currentLine = line.overrideNextLineIndex;
        }
        else
        {
            currentLine++;
        }

        if (currentLine >= GetTotalLines())
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
        Sprite desired = GetLineBackground(currentLine);
        if (desired != null && backgroundImage != null && backgroundImage.sprite != desired)
        {
            backgroundImage.sprite = desired;
        }
    }

    private IEnumerator EndCutscene()
    {
        dialogueText.text = "Loading...";
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(nextSceneName);
    }

    // Helpers (support either CutsceneData or legacy arrays)
    private int GetTotalLines()
    {
        if (cutsceneData != null && cutsceneData.lines != null)
            return cutsceneData.lines.Length;
        return (dialogueLines != null) ? dialogueLines.Length : 0;
    }

    private string GetLineText(int index)
    {
        if (cutsceneData != null && cutsceneData.lines != null)
        {
            if (index >= 0 && index < cutsceneData.lines.Length)
                return cutsceneData.lines[index].text;
            return string.Empty;
        }
        if (dialogueLines != null && index >= 0 && index < dialogueLines.Length)
            return dialogueLines[index];
        return string.Empty;
    }

    private AudioClip GetLineSFX(int index)
    {
        if (cutsceneData != null && cutsceneData.lines != null)
        {
            if (index >= 0 && index < cutsceneData.lines.Length)
                return cutsceneData.lines[index].sfx;
            return null;
        }
        if (dialogueSFX != null && index >= 0 && index < dialogueSFX.Length)
            return dialogueSFX[index];
        return null;
    }

    private Sprite GetLineBackground(int index)
    {
        if (cutsceneData != null && cutsceneData.lines != null)
        {
            if (index >= 0 && index < cutsceneData.lines.Length)
                return cutsceneData.lines[index].background;
            return null;
        }

        // Legacy behavior: pick the last background whose change index <= current line
        Sprite result = null;
        if (backgroundSprites == null || backgroundChangeIndices == null)
            return null;
        for (int i = 0; i < backgroundChangeIndices.Length; i++)
        {
            if (index >= backgroundChangeIndices[i])
            {
                if (i < backgroundSprites.Length)
                    result = backgroundSprites[i];
            }
        }
        return result;
    }


}

