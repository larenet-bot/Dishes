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
    public Image fadeImage;

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

    // Track currently active background track clip so we avoid re-starting the same music
    private AudioClip activeBackgroundTrackClip = null;

    // Only stop the typing coroutine instead of all coroutines
    private Coroutine typingCoroutine;

    private void Start()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(() => StartCoroutine(OnNextClicked()));

        // If a CutsceneData asset is provided, adopt its defaults
        if (cutsceneData != null)
        {
            typingSFX = cutsceneData.typingSFX;
            typingSFXIntervalChars = Mathf.Max(1, cutsceneData.typingSFXIntervalChars);
            typeSpeed = Mathf.Max(0.001f, cutsceneData.typeSpeed);
            nextSceneName = cutsceneData.nextSceneName;

            // If a global background track is assigned, start it now.
            if (cutsceneData.backgroundTrack != null && cutsceneData.backgroundTrack.trackAudioClip != null && AudioManager.instance != null)
            {
                activeBackgroundTrackClip = cutsceneData.backgroundTrack.trackAudioClip;
                AudioManager.instance.PlayMusic(activeBackgroundTrackClip);
            }
        }

        UpdateBackground();
        ShowLine();
    }

    private void ShowLine()
    {
        // Stop only the typing coroutine so other coroutines keep running
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        typingCoroutine = StartCoroutine(TypeText(GetLineText(currentLine)));

        AudioClip sfx = GetLineSFX(currentLine);
        if (AudioManager.instance != null && sfx != null)
            AudioManager.instance.PlaySFXExclusive(sfx);

        UpdateBackground();

        SetupChoices();

        // Update next button label/state (will show "Finish" on last real line)
        UpdateNextButtonState();
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
                        // Start coroutine so fade/wait logic can use yields
                        StartCoroutine(OnChoiceSelected(line.choices[choiceIndex].nextLineIndex));
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

        // Keep button label/state in sync after toggling choices
        UpdateNextButtonState();
    }

    private IEnumerator OnChoiceSelected(int nextIndex)
    {
        choicesPanel.SetActive(false);
        nextButton.gameObject.SetActive(true);

        // Determine current cutscene line data (before advancing)
        CutsceneLine currentCutLine = null;
        if (cutsceneData != null && cutsceneData.lines != null &&
            currentLine >= 0 && currentLine < cutsceneData.lines.Length)
        {
            currentCutLine = cutsceneData.lines[currentLine];
        }

        // Fade before advancing depending on mode
        if (currentCutLine != null)
        {
            if (currentCutLine.fadeMode == FadeMode.FadeOutIn || currentCutLine.fadeMode == FadeMode.FadeOutOnly)
            {
                yield return StartCoroutine(FadeToBlack(currentCutLine.fadeDuration));

                if (currentCutLine.fadeMode == FadeMode.FadeOutIn && currentCutLine.blackScreenTime > 0)
                    yield return new WaitForSeconds(currentCutLine.blackScreenTime);
            }
        }

        currentLine = nextIndex;
        ShowLine();

        // Fade back in if requested
        if (currentCutLine != null && currentCutLine.fadeMode == FadeMode.FadeOutIn)
        {
            yield return StartCoroutine(FadeFromBlack(currentCutLine.fadeDuration));
        }
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

        // Typing finished; clear typingCoroutine reference
        typingCoroutine = null;

        // Ensure button label/state updates once typing completes
        UpdateNextButtonState();
    }

    private IEnumerator OnNextClicked()
    {
        // If currently typing, stop typing and reveal the full line (still allow this even on last line).
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            dialogueText.text = GetLineText(currentLine);
            isTyping = false;
            UpdateNextButtonState();
            yield break;
        }

        int total = GetTotalLines();
        if (total == 0) yield break;

        // If the current line has choices, Next should do nothing.
        if (cutsceneData != null &&
            cutsceneData.lines != null &&
            currentLine >= 0 &&
            currentLine < cutsceneData.lines.Length &&
            cutsceneData.lines[currentLine].hasChoices)
        {
            yield break;
        }

        // Resolve the next line index (respect override if provided)
        int nextIndex;
        CutsceneLine currentCutLine = null;
        if (cutsceneData != null && cutsceneData.lines != null &&
            currentLine >= 0 && currentLine < cutsceneData.lines.Length)
        {
            currentCutLine = cutsceneData.lines[currentLine];
        }

        if (currentCutLine != null && currentCutLine.overrideNextLineIndex >= 0)
            nextIndex = currentCutLine.overrideNextLineIndex;
        else
            nextIndex = currentLine + 1;

        // If the resolved next index goes past the end, treat as finish.
        if (nextIndex >= total)
        {
            StartCoroutine(EndCutscene());
            yield break;
        }

        // Fade if requested (match choice behavior)
        if (currentCutLine != null)
        {
            if (currentCutLine.fadeMode == FadeMode.FadeOutIn || currentCutLine.fadeMode == FadeMode.FadeOutOnly)
            {
                yield return StartCoroutine(FadeToBlack(currentCutLine.fadeDuration));

                if (currentCutLine.fadeMode == FadeMode.FadeOutIn && currentCutLine.blackScreenTime > 0)
                    yield return new WaitForSeconds(currentCutLine.blackScreenTime);
            }
        }

        currentLine = nextIndex;
        ShowLine();

        if (currentCutLine != null && currentCutLine.fadeMode == FadeMode.FadeOutIn)
        {
            yield return StartCoroutine(FadeFromBlack(currentCutLine.fadeDuration));
        }
    }

    private void UpdateBackground()
    {
        Sprite desired = GetLineBackground(currentLine);
        if (desired != null && backgroundImage != null && backgroundImage.sprite != desired)
        {
            backgroundImage.sprite = desired;
        }

        // Music/track handling: per-line track overrides global cutscene track.
        AudioClip desiredTrack = GetLineTrackAudioClip(currentLine);

        // If no per-line track, nothing to do (keep previous / global)
        if (desiredTrack != null && AudioManager.instance != null)
        {
            if (activeBackgroundTrackClip != desiredTrack)
            {
                activeBackgroundTrackClip = desiredTrack;
                AudioManager.instance.PlayMusic(activeBackgroundTrackClip);
            }
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

    private AudioClip GetLineTrackAudioClip(int index)
    {
        // Per-line track takes precedence
        if (cutsceneData != null && cutsceneData.lines != null)
        {
            if (index >= 0 && index < cutsceneData.lines.Length)
            {
                var t = cutsceneData.lines[index].backgroundTrack;
                if (t != null) return t.trackAudioClip;
            }

            // If no per-line track and we haven't set activeBackgroundTrackClip yet, use global
            if (activeBackgroundTrackClip == null && cutsceneData.backgroundTrack != null)
            {
                return cutsceneData.backgroundTrack.trackAudioClip;
            }
        }

        // No track available
        return null;
    }

    /// <summary>
    /// Updates the Next button label and state.
    /// - If the current line is the last line, label becomes "Finish" and clicking finishes the cutscene.
    /// - Otherwise label is "Next".
    /// - If choices are visible, Next is hidden so this function does not change active state.
    /// - While the typewriter is running the button still allows the user to finish typing.
    /// </summary>
    private void UpdateNextButtonState()
    {
        if (nextButton == null) return;

        // If the button is hidden (choices showing), don't change it.
        if (!nextButton.gameObject.activeSelf) return;

        int total = GetTotalLines();
        TMP_Text btnText = nextButton.GetComponentInChildren<TMP_Text>();

        if (total == 0)
        {
            if (btnText != null) btnText.SetText("Next");
            nextButton.interactable = false;
            return;
        }

        bool isLastLine = (currentLine >= total - 1);

        if (btnText != null) btnText.SetText(isLastLine ? "Finish" : "Next");

        // Allow clicking to finish typing; otherwise keep it interactable so user can advance or finish.
        nextButton.interactable = true;
    }
    private IEnumerator FadeToBlack(float duration)
    {
        if (fadeImage == null)
            yield break;

        Color c = fadeImage.color;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            c.a = Mathf.Lerp(0f, 1f, t / duration);
            fadeImage.color = c;

            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }

    private IEnumerator FadeFromBlack(float duration)
    {
        if (fadeImage == null)
            yield break;

        Color c = fadeImage.color;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            c.a = Mathf.Lerp(1f, 0f, t / duration);
            fadeImage.color = c;

            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
    }
}

