using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotGameManager : MonoBehaviour
{
    [Header("Reels")]
    public Reel[] reels;

    [Header("UI")]
    public Button SpinButton;
    public Button AddButton; //increase bet amount
    public Button SubtractButton;// reduce bet amount
    public TextMeshProUGUI BalanceText;
    public TextMeshProUGUI BetText;
    public TextMeshProUGUI StatusText;

    [Header("Game Values")]
    public int Balance = 1000;
    public int CurrentBet = 10;

    private void Start()
    {
        UpdateUI();
        SpinButton.onClick.AddListener(StartSpin);
        AddButton.onClick.AddListener(IncreaseBet);
        SubtractButton.onClick.AddListener(DecreaseBet);
    }

    void UpdateUI()
    {
        BalanceText.text = $"Balance: {Balance}";
        BetText.text = $"Bet: {CurrentBet}";
    }

    public void IncreaseBet()
    {
        if (CurrentBet + 10 <= Balance)
        {
            CurrentBet += 10;
            UpdateUI();
        }
    }

    public void DecreaseBet()
    {
        if (CurrentBet - 10 >= 10)
        {
            CurrentBet -= 10;
            UpdateUI();
        }
    }

    public void StartSpin()
    {
        if (Balance < CurrentBet)
        {
            StatusText.text = "Insufficient Balance!";
            return;

        }
        Balance -= CurrentBet;
        UpdateUI();
        StartCoroutine(SpinRoutine());
    }
    private IEnumerator SpinRoutine()
    {
        ToggleButtons(false);
        StatusText.text = "Spinning...";
        for (int i = 0; i < reels.Length; i++)
        {
            StartCoroutine(reels[i].SpinReel(1.0f + (i * 0.5f)));
        }
        foreach (var reel in reels)
        {
            yield return new WaitUntil(() => !reel.IsSpinning);
        }
        CheckWin();
        ToggleButtons(true);
    }
    private void CheckWin()
    {
        int r1 = reels[0].ResultIndex;
        int r2 = reels[1].ResultIndex;
        int r3 = reels[2].ResultIndex;
        if (r1 == r2 && r2 == r3)
        {
            int winAmount = CurrentBet * 10;
            Balance += winAmount;
            StatusText.text = $"JACKPOT! You Win! +{winAmount}";
        }
        else if (r1 == r2 || r2 == r3 || r1 == r3)
        {
            int winAmount = CurrentBet * 2;
            Balance += winAmount;
            StatusText.text = $"Pair! Won! +{winAmount}";
        }
        else
        {
            StatusText.text = "You Lose! Try Again";
        }
        UpdateUI();
    }
    void ToggleButtons(bool state)
    {
        SpinButton.interactable = state;
        AddButton.interactable = state;
        SubtractButton.interactable = state;
    }

}