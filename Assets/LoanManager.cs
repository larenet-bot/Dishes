using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoanManager : MonoBehaviour
{
    [Serializable]
    public class LoanTier
    {
        [Header("Loan")]
        public string loanName = "Starter Loan";
        [Tooltip("Amount due to clear this loan tier (single payment).")]
        public float amount = 100f;

        [Header("Optional Scene Jump")]
        [Tooltip("If true, paying this loan will load the scene below.")]
        public bool loadSceneOnPay = false;

        [Tooltip("Scene to load after paying this loan (e.g., a cutscene or shop).")]
        public string sceneName = "";
    }

    [Header("References")]
    [Tooltip("Reference to ScoreManager (auto-finds if left empty).")]
    [SerializeField] private ScoreManager scoreManager;

    [Tooltip("Button the player clicks to pay off the current loan.")]
    [SerializeField] private Button payLoanButton;

    [Tooltip("Text component shown on the pay button.")]
    [SerializeField] private TMP_Text payButtonText;

    [Header("Loan Tiers (like Upgrades)")]
    [Tooltip("Add as many loan tiers as you want, in order.")]
    public List<LoanTier> loanTiers = new List<LoanTier>();

    [Header("State / Persistence (optional)")]
    [Tooltip("Save and load current loan index in PlayerPrefs.")]
    public bool usePersistence = false;

    [Tooltip("PlayerPrefs key used if persistence is enabled.")]
    public string prefsKey_CurrentLoanIndex = "LOAN_CURRENT_INDEX";

    private int currentLoanIndex = 0;

    private void Reset()
    {
        scoreManager = FindFirstObjectByType<ScoreManager>();
    }

    private void Awake()
    {
        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManager>();

        // Seed a couple tiers so the component works out-of-the-box
        if (loanTiers.Count == 0)
        {
            loanTiers.Add(new LoanTier { loanName = "Starter Loan", amount = 100f, loadSceneOnPay = false });
            loanTiers.Add(new LoanTier { loanName = "Expansion Loan", amount = 1000f, loadSceneOnPay = false });
            loanTiers.Add(new LoanTier { loanName = "Mega Loan", amount = 10000f, loadSceneOnPay = false });
        }

        if (usePersistence)
            currentLoanIndex = PlayerPrefs.GetInt(prefsKey_CurrentLoanIndex, 0);

        if (payLoanButton != null)
        {
            payLoanButton.onClick.RemoveAllListeners();
            payLoanButton.onClick.AddListener(OnPayLoanClicked);
        }
    }

    private void OnEnable()
    {
        // Keep button text/interactable synced with player funds
        ScoreManager.OnProfitChanged += RefreshUI; // uses the same event pattern as EmployeeManager. 
        RefreshUI();
    }

    private void OnDisable()
    {
        ScoreManager.OnProfitChanged -= RefreshUI;
    }

    private bool HasActiveLoan()
    {
        return loanTiers != null && currentLoanIndex >= 0 && currentLoanIndex < loanTiers.Count;
    }

    private void RefreshUI()
    {
        if (!HasActiveLoan())
        {
            // Hide or disable UI once all loans are done
            if (payLoanButton) payLoanButton.interactable = false;
            if (payButtonText) payButtonText.text = "All loans paid";
            return;
        }

        var tier = loanTiers[currentLoanIndex];
        float wallet = scoreManager ? scoreManager.GetTotalProfit() : 0f;
        bool canAfford = wallet >= tier.amount;

        if (payLoanButton) payLoanButton.interactable = canAfford; // greys out automatically when false

        // Always use BigNumberFormatter (money) for the amount
        if (payButtonText)
        {
            string amountStr = BigNumberFormatter.FormatMoney(tier.amount);
            // Example: "Pay off loan: $100" and scales as amounts grow
            payButtonText.text = $"Pay off loan: {amountStr}";
        }
    }

    public void OnPayLoanClicked()
    {
        if (!HasActiveLoan() || scoreManager == null) return;

        var tier = loanTiers[currentLoanIndex];
        float wallet = scoreManager.GetTotalProfit();

        if (wallet < tier.amount)
            return; // should already be greyed out, but double-guard

        // Subtract from wallet as a purchase so your profit rate calc stays accurate
        scoreManager.SubtractProfit(tier.amount, isPurchase: true); // mirrors your other purchases

        // Advance to the next loan tier
        currentLoanIndex++;
        if (usePersistence)
        {
            PlayerPrefs.SetInt(prefsKey_CurrentLoanIndex, currentLoanIndex);
            PlayerPrefs.Save();
        }

        // Optional scene load for this tier
        if (tier.loadSceneOnPay && !string.IsNullOrEmpty(tier.sceneName))
        {
            SceneManager.LoadScene(tier.sceneName);
            return; // scene is changing; no need to refresh here
        }

        RefreshUI();
    }

    // Optional helpers if you want to manipulate loans from other scripts
    public bool AllLoansPaid() => !HasActiveLoan();
    public int GetCurrentLoanIndex() => Mathf.Clamp(currentLoanIndex, 0, loanTiers.Count - 1);
}
