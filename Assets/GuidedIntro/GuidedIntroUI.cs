using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuidedIntroUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public Image duckPortrait;
    public TMP_Text speakerNameText;
    public TMP_Text dialogueText;
    public TMP_Text instructionText;

    private void Awake()
    {
        // The guided intro should not be visible when the scene starts.
        Hide();
    }

    public void ShowStep(GuidedIntroStep step)
    {
        if (step == null)
        {
            Hide();
            return;
        }

        dialoguePanel.SetActive(true);

        speakerNameText.text = step.speakerName;
        dialogueText.text = step.dialogueText;
        instructionText.text = step.instructionText;
    }

    public void Hide()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }
}