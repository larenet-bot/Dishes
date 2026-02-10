using UnityEngine;

public class Note : MonoBehaviour
{
    [HideInInspector] public int lane;
    public float targetTime;

    public bool canBeHit = false;
    public bool wasHit = false;

    public HitWindow hitWindow;
    public AudioSource musicSource;

    public SpriteRenderer noteSprite;
    public Color highlightColor = Color.yellow;
    private Color originalColor;

    // Optional DSP-aware fields (set by spawner.Initialize)
    private double songStartDspTime = -1.0;

    void Start()
    {
        if (noteSprite != null)
            originalColor = noteSprite.color;
    }

    /// <summary>
    /// Lightweight initializer called by NoteSpawner so the note has access
    /// to the song DSP start time. Non-destructive: existing behavior unchanged.
    /// </summary>
    public void Initialize(float noteTime, double songStartDsp)
    {
        this.targetTime = noteTime;
        this.songStartDspTime = songStartDsp;
    }

    /// <summary>
    /// Returns seconds until the target hit time according to DSP clock.
    /// If Initialize wasn't called, falls back to Time.time-based estimate.
    /// </summary>
    public float GetTimeUntilHit()
    {
        if (songStartDspTime > 0.0)
        {
            double songTime = AudioSettings.dspTime - songStartDspTime;
            return targetTime - (float)songTime;
        }
        else
        {
            // best-effort fallback (not sample-accurate)
            return targetTime - (Time.time - 0f);
        }
    }

    // Called by LaneKey, receives correct time difference
    public void Hit(float timeDifference)
    {
        // Only accept hit if hittable and not already hit
        if (!canBeHit || wasHit)
            return;

        // mark as hit to prevent duplicates
        wasHit = true;

        // Judge the hit (updates MiniScoreManager and HUD)
        if (hitWindow != null)
            hitWindow.JudgeNoteHit(timeDifference);

        // Destroy immediately to avoid MISS on trigger exit
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("HitBox"))
        {
            canBeHit = true;

            if (noteSprite != null)
                noteSprite.color = highlightColor;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("HitBox"))
        {
            canBeHit = false;

            if (!wasHit)
            {
                MiniScoreManager.AddMiss();
                UI_RhythmHUD.Instance.ShowFeedback("MISS");
            }

            if (noteSprite != null)
                noteSprite.color = originalColor;

            Destroy(gameObject);
        }
    }
}