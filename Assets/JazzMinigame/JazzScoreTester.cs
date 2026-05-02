using System.Collections.Generic;
using UnityEngine;

public class JazzScoreTester : MonoBehaviour
{
    [Header("Test Band")]
    public PerformerData drummer;
    public PerformerData bassist;
    public PerformerData singer;
    public PerformerData saxPlayer;

    [Header("Debug")]
    public bool logOnStart = true;

    private void Start()
    {
        if (logOnStart)
        {
            TestScore();
        }
    }

    [ContextMenu("Test Jazz Score")]
    public void TestScore()
    {
        List<PerformerData> selectedPerformers = new List<PerformerData>
        {
            drummer,
            bassist,
            singer,
            saxPlayer
        };

        JazzPerformanceScoreResult result =
            JazzPerformanceScorer.CalculateScore(selectedPerformers, true);

        Debug.Log($"[JazzScoreTester] Final Score: {result.finalPerformanceScore}");
    }
}