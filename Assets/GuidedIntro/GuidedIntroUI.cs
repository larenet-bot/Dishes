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
        dialoguePanel.SetActive(false);
    }
}