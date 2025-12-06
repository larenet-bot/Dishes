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

    void Start()
    {
        if (noteSprite != null)
            originalColor = noteSprite.color;
    }

    // Called by LaneKey, receives correct time difference
    public void Hit(float timeDifference)
    {
        if (!canBeHit || wasHit == false)
            return;

        // Judge the hit
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
