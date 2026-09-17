using UnityEngine;

public class GuidedIntroManager : MonoBehaviour
{
    private const string IntroCompletedKey = "GuidedIntroCompleted";
    [Header("Intro Steps")]
    [Tooltip("The steps of the guided introduction, in the order they should play.")]
    public GuidedIntroStep[] introSteps;
    [Header("UI")]
    public GuidedIntroUI introUI;

    [Header("Current Step")]
    [SerializeField]
    private int currentStepIndex = 0;

    private int currentActionCount = 0;

    private bool leftMovementCompleted = false;
    private bool rightMovementCompleted = false;

    private bool introActive = false;
   private void Start()
    {
        if (introSteps == null || introSteps.Length == 0)
        {
            Debug.LogWarning("GuidedIntroManager has no intro steps assigned.");
            return;
        }

        // Don't start the intro if the player has already completed it.
        if (PlayerPrefs.GetInt(IntroCompletedKey, 0) == 1)
        {
            introActive = false;

            if (introUI != null)
            {
                introUI.Hide();
            }

            Debug.Log("GUIDED INTRO: Already completed. Skipping intro.");
            return;
        }

        StartIntro();
    }

    public void StartIntro()
    {
        currentStepIndex = 0;
        currentActionCount = 0;

        leftMovementCompleted = false;
        rightMovementCompleted = false;

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

        leftMovementCompleted = false;
        rightMovementCompleted = false;

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

        // Remember that the player has completed the guided intro.
        PlayerPrefs.SetInt(IntroCompletedKey, 1);
        PlayerPrefs.Save();

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


   

    public void ReportCompletion(CompletionType completionType)
    {
        if (!introActive)
            return;

        GuidedIntroStep currentStep = GetCurrentStep();

        if (currentStep == null)
            return;

        // Special handling for the movement tutorial.
        if (completionType == CompletionType.LeftMovementPerformed)
        {
            if (currentStep.completionType != CompletionType.LeftMovementPerformed &&
                currentStep.completionType != CompletionType.RightMovementPerformed)
            {
                return;
            }

            leftMovementCompleted = true;

            Debug.Log("GUIDED INTRO: Left movement completed.");

            CheckMovementTutorialComplete();
            return;
        }

        if (completionType == CompletionType.RightMovementPerformed)
        {
            if (currentStep.completionType != CompletionType.LeftMovementPerformed &&
                currentStep.completionType != CompletionType.RightMovementPerformed)
            {
                return;
            }

            rightMovementCompleted = true;

            Debug.Log("GUIDED INTRO: Right movement completed.");

            CheckMovementTutorialComplete();
            return;
        }

        // Normal completion handling.
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



    public void ReportMovementLeft()
    {
        if (!introActive)
            return;

        GuidedIntroStep currentStep = GetCurrentStep();

        if (currentStep == null)
            return;

        if (currentStep.completionType != CompletionType.MovementBothDirections)
            return;

        leftMovementCompleted = true;

        Debug.Log("GUIDED INTRO: Left movement completed.");

        CheckMovementTutorialComplete();
    }

    public void ReportMovementRight()
    {
        if (!introActive)
            return;

        GuidedIntroStep currentStep = GetCurrentStep();

        if (currentStep == null)
            return;

        if (currentStep.completionType != CompletionType.MovementBothDirections)
            return;

        rightMovementCompleted = true;

        Debug.Log("GUIDED INTRO: Right movement completed.");

        CheckMovementTutorialComplete();
    }
    private void CheckMovementTutorialComplete()
    {
        if (leftMovementCompleted && rightMovementCompleted)
        {
            Debug.Log(
                "GUIDED INTRO: Both left and right movement completed."
            );

            CompleteCurrentStep();
        }
    }
    public void ReportSettingsOpened()
    {
        ReportCompletion(CompletionType.SettingsOpened);
    }


}