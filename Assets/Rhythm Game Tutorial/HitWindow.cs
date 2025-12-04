using UnityEngine;

public class HitWindow : MonoBehaviour
{
    public float perfectRange = 0.05f;
    public float goodRange = 0.10f;
    public float badRange = 0.20f;

    public void JudgeNoteHit(float timeDifference)
    {
        timeDifference = Mathf.Abs(timeDifference);

        if (timeDifference <= perfectRange)
        {
            MiniScoreManager.AddPerfect();
            UI_RhythmHUD.Instance.ShowFeedback("PERFECT");
        }
        else if (timeDifference <= goodRange)
        {
            MiniScoreManager.AddGood();
            UI_RhythmHUD.Instance.ShowFeedback("GOOD");
        }
        else if (timeDifference <= badRange)
        {
            MiniScoreManager.AddBad();
            UI_RhythmHUD.Instance.ShowFeedback("BAD");
        }
        else
        {
            MiniScoreManager.AddMiss();
            UI_RhythmHUD.Instance.ShowFeedback("MISS");
        }
    }
}
