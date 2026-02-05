using UnityEngine;

public static class MiniScoreManager
{
    public static int Score { get; private set; }
    public static int Perfects { get; private set; }
    public static int Goods { get; private set; }
    public static int Bads { get; private set; }
    public static int Misses { get; private set; }
    public static int WrongPresses { get; private set; }

    public static System.Action OnScoreUpdated;

    // ----- adjustable values -----
    public static int perfectValue = 300;
    public static int goodValue = 150;
    public static int badValue = 50;
    public static int missValue = 0;

    // Penalty for pressing a wrong key when there is a hittable note in another lane.
    // Set to a negative value to reduce score.
    public static int wrongPressValue = -300;

    public static void ResetScore()
    {
        Score = 0;
        Perfects = 0;
        Goods = 0;
        Bads = 0;
        Misses = 0;
        WrongPresses = 0;

        OnScoreUpdated?.Invoke();
    }

    public static void AddPerfect()
    {
        Score += perfectValue;
        Perfects++;
        OnScoreUpdated?.Invoke();
    }

    public static void AddGood()
    {
        Score += goodValue;
        Goods++;
        OnScoreUpdated?.Invoke();
    }

    public static void AddBad()
    {
        Score += badValue;
        Bads++;
        OnScoreUpdated?.Invoke();
    }

    public static void AddMiss()
    {
        Misses++;
        Score += missValue;
        OnScoreUpdated?.Invoke();
    }

    public static void AddWrongPress()
    {
        WrongPresses++;
        Score += wrongPressValue;
        OnScoreUpdated?.Invoke();
    }
}
