using UnityEngine;

public enum HitType
{
    Perfect,
    Good,
    Bad,
    Miss
}

public class HitWindow : MonoBehaviour
{
    [Header("Timing windows in seconds")]
    public float perfectRange = 0.25f;
    public float goodRange = 0.35f;
    public float badRange = 0.50f;

    /// <summary>
    /// Judge a note hit based on timing difference (absolute seconds)
    /// </summary>
    /// <param name="timeDifference">Difference between note target time and actual hit time (seconds)</param>
    /// <returns>The HitType of the note</returns>
    public HitType JudgeNoteHit(float timeDifference)
    {
        float absDiff = Mathf.Abs(timeDifference);
        HitType result;

        if (absDiff <= perfectRange)
        {
            MiniScoreManager.AddPerfect();
            result = HitType.Perfect;
        }
        else if (absDiff <= goodRange)
        {
            MiniScoreManager.AddGood();
            result = HitType.Good;
        }
        else if (absDiff <= badRange)
        {
            MiniScoreManager.AddBad();
            result = HitType.Bad;
        }
        else
        {
            MiniScoreManager.AddMiss();
            result = HitType.Miss;
        }

        // Show feedback if the HUD exists
        if (UI_RhythmHUD.Instance != null)
        {
            UI_RhythmHUD.Instance.ShowFeedback(result.ToString().ToUpper());
        }
        //Debug.Log($"JudgeNoteHit called | diff={absDiff} | HUD={(UI_RhythmHUD.Instance != null ? "OK" : "NULL")}");

        return result;

    }
}
