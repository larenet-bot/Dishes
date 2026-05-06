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
        [Tooltip("Amount due to clear this loan tier as a single payment.")]
        public float amount = 100f;

        [Header("Optional Scene Jump")]
        [Tooltip("If true, paying this loan will load the scene below.")]
        public bool loadSceneOnPay = false;

        [Tooltip("Scene to load after paying this loan, such as a cutscene.")]
        public string sceneName = "";
    }

    public static event Action<string, bool> OnKitchenLoanStateChanged;
    public static event Action<string> OnKitchenAllLoansPaid;

    [Header("References")]
    [Tooltip("Reference to ScoreManager. Auto-finds if left empty.")]
    [SerializeField] private ScoreManager scoreManager;

    [Tooltip("Button the player clicks to pay off the current loan.")]
    [SerializeField] private Button payLoanButton;

    [Tooltip("Text component shown on the pay button.")]
    [SerializeField] private TMP_Text payButtonText;

    [Header("Loan Tiers")]
    [Tooltip("Add as many loan tiers as you want, in order.")]
    public List<LoanTier> loanTiers = new List<LoanTier>();

    [Header("Kitchen Scope")]
    [Tooltip("Use a different loan progress key for each kitchen scene.")]
    [SerializeField] private bool useKitchenScopedLoanProgress = true;

    [Tooltip("Unique id for this kitchen. Use the same id in KitchenBusinessMenu.")]
    [SerializeField] private string kitchenId = "kitchen_1";

    [Tooltip("Only turn this on in Kitchen 1 if you need to import the old global loan save once.")]
    [SerializeField] private bool allowGlobalSaveMigrationForThisKitchen = false;

    [Tooltip("Turn this on for Kitchen 1. When all its loans are paid, the Other Businesses button becomes available.")]
    [SerializeField] private bool unlockOtherBusinessesWhenAllLoansPaid = false;

    [Header("Legacy / Optional Persistence")]
    [Tooltip("Older fallback. Kitchen-scoped progress is preferred for multiple kitchens.")]
    public bool usePersistence = false;

    [Tooltip("Base PlayerPrefs key used if persistence is enabled.")]
    public string prefsKey_CurrentLoanIndex = "LOAN_CURRENT_INDEX";

    private int currentLoanIndex = 0;
    private bool lastAllLoansPaidState = false;

    private string KitchenScopedLoanPrefsKey
    {
        get { return $"{prefsKey_CurrentLoanIndex}_{kitchenId}"; }
    }

    private void Reset()
    {
        scoreManager = FindFirstObjectByType<ScoreManager>();
    }

    private void Awake()
    {
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }

        if (loanTiers.Count == 0)
        {
            loanTiers.Add(new LoanTier { loanName = "Starter Loan", amount = 100f, loadSceneOnPay = false });
            loanTiers.Add(new LoanTier { loanName = "Expansion Loan", amount = 1000f, loadSceneOnPay = false });
            loanTiers.Add(new LoanTier { loanName = "Mega Loan", amount = 10000f, loadSceneOnPay = false });
        }

        LoadLoanProgressFromPrefs();

        if (payLoanButton != null)
        {
            payLoanButton.onClick.RemoveAllListeners();
            payLoanButton.onClick.AddListener(OnPayLoanClicked);
        }

        ApplyProgressionFlags();
    }

    private void OnEnable()
    {
        ScoreManager.OnProfitChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        ScoreManager.OnProfitChanged -= RefreshUI;
    }

    private void LoadLoanProgressFromPrefs()
    {
        if (useKitchenScopedLoanProgress)
        {
            if (PlayerPrefs.HasKey(KitchenScopedLoanPrefsKey))
            {
                currentLoanIndex = PlayerPrefs.GetInt(KitchenScopedLoanPrefsKey, 0);
            }
            else
            {
                currentLoanIndex = 0;
            }

            currentLoanIndex = ClampLoanIndex(currentLoanIndex);
            return;
        }

        if (usePersistence)
        {
            currentLoanIndex = ClampLoanIndex(PlayerPrefs.GetInt(prefsKey_CurrentLoanIndex, 0));
        }
    }

    private void SaveLoanProgressToPrefs()
    {
        if (useKitchenScopedLoanProgress)
        {
            PlayerPrefs.SetInt(KitchenScopedLoanPrefsKey, currentLoanIndex);
        }
        else if (usePersistence)
        {
            PlayerPrefs.SetInt(prefsKey_CurrentLoanIndex, currentLoanIndex);
        }

        PlayerPrefs.Save();
    }

    private int ClampLoanIndex(int index)
    {
        int max = loanTiers != null ? loanTiers.Count : 0;
        return Mathf.Clamp(index, 0, max);
    }

    private bool HasActiveLoan()
    {
        return loanTiers != null && currentLoanIndex >= 0 && currentLoanIndex < loanTiers.Count;
    }

    private void RefreshUI()
    {
        ApplyProgressionFlags();

        if (!HasActiveLoan())
        {
            if (payLoanButton != null) payLoanButton.interactable = false;
            if (payButtonText != null) payButtonText.text = "All loans paid";
            return;
        }

        LoanTier tier = loanTiers[currentLoanIndex];
        float wallet = scoreManager != null ? scoreManager.GetTotalProfit() : 0f;
        bool canAfford = wallet >= tier.amount;

        if (payLoanButton != null)
        {
            payLoanButton.interactable = canAfford;
        }

        if (payButtonText != null)
        {
            string amountText = BigNumberFormatter.FormatMoney((double)tier.amount);
            payButtonText.text = $"Pay off loan: {amountText}";
        }
    }

    public void OnPayLoanClicked()
    {
        if (!HasActiveLoan() || scoreManager == null)
        {
            return;
        }

        LoanTier tier = loanTiers[currentLoanIndex];
        float wallet = scoreManager.GetTotalProfit();

        if (wallet < tier.amount)
        {
            return;
        }

        // Loans are purchases. This keeps ProfitRate from treating the payment as negative income.
        scoreManager.SubtractProfit(tier.amount, isPurchase: true);

        currentLoanIndex = ClampLoanIndex(currentLoanIndex + 1);
        SaveLoanProgressToPrefs();
        ApplyProgressionFlags();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
        }

        if (tier.loadSceneOnPay && !string.IsNullOrEmpty(tier.sceneName))
        {
            SceneManager.LoadScene(tier.sceneName);
            return;
        }

        RefreshUI();
    }

    private void ApplyProgressionFlags()
    {
        bool allLoansPaid = AllLoansPaid();

        KitchenBusinessProgress.SetKitchenLoansPaid(kitchenId, allLoansPaid);

        if (allLoansPaid && unlockOtherBusinessesWhenAllLoansPaid)
        {
            KitchenBusinessProgress.SetOtherBusinessesUnlocked(true);
        }

        if (allLoansPaid != lastAllLoansPaidState)
        {
            OnKitchenLoanStateChanged?.Invoke(kitchenId, allLoansPaid);

            if (allLoansPaid)
            {
                OnKitchenAllLoansPaid?.Invoke(kitchenId);
            }

            lastAllLoansPaidState = allLoansPaid;
        }
    }

    public bool AllLoansPaid()
    {
        return !HasActiveLoan();
    }

    public int GetCurrentLoanIndex()
    {
        if (loanTiers == null || loanTiers.Count == 0)
        {
            return 0;
        }

        return Mathf.Clamp(currentLoanIndex, 0, loanTiers.Count - 1);
    }

    public int GetLoanIndexForSave()
    {
        return currentLoanIndex;
    }

    public void ApplyLoanIndexFromSave(int index)
    {
        if (useKitchenScopedLoanProgress)
        {
            if (PlayerPrefs.HasKey(KitchenScopedLoanPrefsKey))
            {
                currentLoanIndex = ClampLoanIndex(PlayerPrefs.GetInt(KitchenScopedLoanPrefsKey, 0));
            }
            else if (allowGlobalSaveMigrationForThisKitchen)
            {
                currentLoanIndex = ClampLoanIndex(index);
                SaveLoanProgressToPrefs();
            }
            else
            {
                currentLoanIndex = 0;
            }
        }
        else
        {
            currentLoanIndex = ClampLoanIndex(index);
        }

        ApplyProgressionFlags();
        RefreshUI();
    }

    public string GetKitchenId()
    {
        return kitchenId;
    }
}
