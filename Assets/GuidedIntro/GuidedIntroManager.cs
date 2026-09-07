using UnityEngine;

public class GuidedIntroManager : MonoBehaviour
{
    [Header("Intro Steps")]
    [Tooltip("The steps of the guided introduction, in the order they should play.")]
    public GuidedIntroStep[] introSteps;
    [Header("UI")]
    public GuidedIntroUI introUI;

    [Header("Current Step")]
    [SerializeField]
    private int currentStepIndex = 0;

    private int currentActionCount = 0;

    private bool introActive = false;
    private void Start()
    {
        if (introSteps == null || introSteps.Length == 0)
        {
            Debug.LogWarning("GuidedIntroManager has no intro steps assigned.");
            return;
        }

        StartIntro();
    }

    public void StartIntro()
    {
        currentStepIndex = 0;
        currentActionCount = 0;
        introActive = true;

        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (!introActive)
            return;

        if (currentStepIndex >= introSteps.Length)
        {
            CompleteIntro();
            return;
        }

        GuidedIntroStep currentStep = introSteps[currentStepIndex];

        if (currentStep == null)
        {
            Debug.LogWarning(
                $"Guided Intro Step {currentStepIndex} is empty."
            );

            AdvanceToNextStep();
            return;
        }

        introUI.ShowStep(currentStep);
    }

    public void CompleteCurrentStep()
    {
        if (!introActive)
            return;

        GuidedIntroStep currentStep = introSteps[currentStepIndex];

        Debug.Log(
            $"GUIDED INTRO: Completed step {currentStepIndex + 1}"
        );

        AdvanceToNextStep();
    }

    private void AdvanceToNextStep()
    {
        currentStepIndex++;
        currentActionCount = 0;

        if (currentStepIndex >= introSteps.Length)
        {
            CompleteIntro();
            return;
        }

        ShowCurrentStep();
    }

    private void CompleteIntro()
    {
        introActive = false;

        if (introUI != null)
        {
            introUI.Hide();
        }

        Debug.Log("GUIDED INTRO: Introduction complete!");
    }

    public GuidedIntroStep GetCurrentStep()
    {
        if (!introActive)
            return null;

        if (currentStepIndex < 0 ||
            currentStepIndex >= introSteps.Length)
        {
            return null;
        }

        return introSteps[currentStepIndex];
    }

    public int GetCurrentStepIndex()
    {
        return currentStepIndex;
    }

    public bool IsIntroActive()
    {
        return introActive;
    }
    public void ReportMovementPerformed()
    {
        ReportCompletion(CompletionType.MovementPerformed);
    }



    public void ReportCompletion(CompletionType completionType)
    {
        if (!introActive)
            return;

        GuidedIntroStep currentStep = GetCurrentStep();

        if (currentStep == null)
            return;

        if (currentStep.completionType != completionType)
            return;

        currentActionCount++;

        Debug.Log(
            $"GUIDED INTRO: Correct action completed - " +
            $"{completionType} " +
            $"({currentActionCount}/{currentStep.requiredActions})"
        );

        if (currentActionCount < currentStep.requiredActions)
            return;

        CompleteCurrentStep();
    }

    public void ReportButtonClicked()
    {
        ReportCompletion(CompletionType.ButtonClicked);
    }
}