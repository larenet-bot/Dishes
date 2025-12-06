using UnityEngine;

public class Note : MonoBehaviour
{
    [HideInInspector] public int lane;
    public float targetTime;
    public bool canBeHit = false;

    public bool wasHit = false;

    private HitWindow hitWindow;
    private AudioSource musicSource;

    public SpriteRenderer noteSprite;
    public Color highlightColor = Color.yellow;
    private Color originalColor;

    void Start()
    {
        hitWindow = Object.FindFirstObjectByType<HitWindow>();
        musicSource = Object.FindFirstObjectByType<AudioSource>();

        if (noteSprite != null)
            originalColor = noteSprite.color;
    }

    public void Hit()
    {
        if (!canBeHit || wasHit)
            return;

        wasHit = true;

        float diff = musicSource.time - targetTime;
        hitWindow.JudgeNoteHit(diff);

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

            if (wasHit == false)
            {
                // TRUE miss
                MiniScoreManager.AddMiss();
                UI_RhythmHUD.Instance.ShowFeedback("MISS");
            }

            if (noteSprite != null)
                noteSprite.color = originalColor;

            Destroy(gameObject);
        }
    }
}
