using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JazzShowFlowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JazzBandBookingManager bookingManager;

    [Header("Panels")]
    [Tooltip("The main booking screen with performer cards, band slots, budget, and start button.")]
    [SerializeField] private GameObject bookingPanel;

    [Tooltip("The stage background panel shown after the show starts.")]
    [SerializeField] private GameObject stagePanel;

    [Tooltip("The result screen panel shown after the stage interlude.")]
    [SerializeField] private GameObject resultPanel;

    [Header("Stage Timing")]
    [SerializeField] private float stageDurationSeconds = 5f;

    [Header("Simple Payout")]
    [Tooltip("Flat amount added to every completed show.")]
    [SerializeField] private float basePayout = 0f;

    [Tooltip("Final performance score is multiplied by this to create the profit reward.")]
    [SerializeField] private float payoutPerPerformancePoint = 5f;

    [Tooltip("If true, the payout is only granted once per completed show sequence.")]
    [SerializeField] private bool preventDuplicatePayout = true;

    [Header("Future Stage Audio Hook")]
    [Tooltip("Optional AudioSource for the short stage performance clip.")]
    [SerializeField] private AudioSource stageAudioSource;

    [Tooltip("Temporary default clip. Later this can be chosen based on instrument talent and score.")]
    [SerializeField] private AudioClip defaultStageClip;

    [Header("Future Stage Visual Hook")]
    [Tooltip("Optional animated instrument objects. For now, these can simply turn on during the stage interlude.")]
    [SerializeField] private GameObject[] stageVisualObjects;

    [Header("Result Text")]
    [Tooltip("Optional title text on the result screen.")]
    [SerializeField] private TMP_Text resultTitleText;

    [Tooltip("Optional body text on the result screen.")]
    [SerializeField] private TMP_Text resultBodyText;

    [Header("Buttons")]
    [Tooltip("Optional button on the result screen that returns to booking.")]
    [SerializeField] private Button backToBookingButton;

    private Coroutine showSequenceCoroutine;
    private JazzPerformanceScoreResult latestResult;

    private float latestPayout;
    private bool payoutGrantedForCurrentShow;

    private void Awake()
    {
        if (bookingManager == null)
            bookingManager = FindFirstObjectByType<JazzBandBookingManager>();

        if (backToBookingButton != null)
        {
            backToBookingButton.onClick.RemoveAllListeners();
            backToBookingButton.onClick.AddListener(ReturnToBookingScreen);
        }
    }

    private void OnEnable()
    {
        if (bookingManager == null)
            bookingManager = JazzBandBookingManager.Instance;

        if (bookingManager != null)
        {
            bookingManager.onBookingFinished.RemoveListener(BeginShowSequence);
            bookingManager.onBookingFinished.AddListener(BeginShowSequence);
        }
    }

    private void OnDisable()
    {
        if (bookingManager != null)
            bookingManager.onBookingFinished.RemoveListener(BeginShowSequence);
    }

    private void Start()
    {
        ShowBookingPanelOnly();
    }

    public void BeginShowSequence()
    {
        if (bookingManager == null)
            bookingManager = JazzBandBookingManager.Instance;

        if (bookingManager == null)
            return;

        latestResult = bookingManager.GetLatestScoreResult();

        if (latestResult == null)
            latestResult = bookingManager.CalculateCurrentScore(false);

        latestPayout = CalculatePayout(latestResult);
        payoutGrantedForCurrentShow = false;

        if (showSequenceCoroutine != null)
            StopCoroutine(showSequenceCoroutine);

        showSequenceCoroutine = StartCoroutine(ShowSequenceCoroutine());
    }

    private IEnumerator ShowSequenceCoroutine()
    {
        ShowStagePanelOnly();

        PlayStageAudioPlaceholder();
        ShowStageVisualsPlaceholder(true);

        yield return new WaitForSeconds(stageDurationSeconds);

        ShowStageVisualsPlaceholder(false);
        StopStageAudioPlaceholder();

        GrantPayout();

        ShowResultPanelOnly();

        showSequenceCoroutine = null;
    }

    private float CalculatePayout(JazzPerformanceScoreResult result)
    {
        if (result == null)
            return 0f;

        float payout = basePayout + result.finalPerformanceScore * payoutPerPerformancePoint;

        return Mathf.Max(0f, payout);
    }

    private void GrantPayout()
    {
        if (latestPayout <= 0f)
            return;

        if (preventDuplicatePayout && payoutGrantedForCurrentShow)
            return;

        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[JazzShowFlowController] Cannot grant payout. ScoreManager.Instance is missing.");
            return;
        }

        ScoreManager.Instance.AddProfit(latestPayout);
        payoutGrantedForCurrentShow = true;

        Debug.Log($"[JazzShowFlowController] Jazz show payout granted: {BigNumberFormatter.FormatMoney(latestPayout)}");
    }

    private void ShowBookingPanelOnly()
    {
        if (bookingPanel != null)
            bookingPanel.SetActive(true);

        if (stagePanel != null)
            stagePanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        ShowStageVisualsPlaceholder(false);
    }

    private void ShowStagePanelOnly()
    {
        if (bookingPanel != null)
            bookingPanel.SetActive(false);

        if (stagePanel != null)
            stagePanel.SetActive(true);

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void ShowResultPanelOnly()
    {
        if (bookingPanel != null)
            bookingPanel.SetActive(false);

        if (stagePanel != null)
            stagePanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        RefreshResultScreen();
    }

    public void ReturnToBookingScreen()
    {
        if (showSequenceCoroutine != null)
        {
            StopCoroutine(showSequenceCoroutine);
            showSequenceCoroutine = null;
        }

        StopStageAudioPlaceholder();
        ShowStageVisualsPlaceholder(false);
        ShowBookingPanelOnly();
    }

    private void PlayStageAudioPlaceholder()
    {
        if (stageAudioSource == null || defaultStageClip == null)
            return;

        stageAudioSource.Stop();
        stageAudioSource.clip = defaultStageClip;
        stageAudioSource.Play();
    }

    private void StopStageAudioPlaceholder()
    {
        if (stageAudioSource == null)
            return;

        stageAudioSource.Stop();
    }

    private void ShowStageVisualsPlaceholder(bool show)
    {
        if (stageVisualObjects == null)
            return;

        for (int i = 0; i < stageVisualObjects.Length; i++)
        {
            if (stageVisualObjects[i] != null)
                stageVisualObjects[i].SetActive(show);
        }
    }

    private void RefreshResultScreen()
    {
        if (latestResult == null)
            return;

        if (resultTitleText != null)
            resultTitleText.text = "Tonight's Performance";

        if (resultBodyText != null)
        {
            int netChemistry = latestResult.GetNetChemistry();

            resultBodyText.text =
                $"Raw Talent: {latestResult.rawTalentScore}\n" +
                $"Chemistry: {FormatSignedNumber(netChemistry)}\n" +
                $"Final Performance: {latestResult.finalPerformanceScore}\n" +
                $"Profit Gained: {BigNumberFormatter.FormatMoney(latestPayout)}";
        }
    }

    private string FormatSignedNumber(int value)
    {
        if (value > 0)
            return $"+{value}";

        return value.ToString();
    }
}