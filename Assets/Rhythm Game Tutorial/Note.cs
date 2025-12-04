using UnityEngine;

public class Note : MonoBehaviour
{
    [HideInInspector] public int lane;       // assigned by spawner
    public float targetTime;                 // when this note SHOULD be hit
    public bool canBeHit = false;

    private HitWindow hitWindow;
    private AudioSource musicSource;

    void Start()
    {
        hitWindow = Object.FindFirstObjectByType<HitWindow>();
        musicSource = Object.FindFirstObjectByType<AudioSource>();

        Debug.Log($"Note spawned in lane {lane} (targetTime={targetTime})");
    }

    // Called when the player presses a key
    public void Hit()
    {
        if (!canBeHit) return;

        float diff = musicSource.time - targetTime;
        hitWindow.JudgeNoteHit(diff);

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("HitBox"))
            canBeHit = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("HitBox"))
        {
            canBeHit = false;

            // Only judge miss here
            MiniScoreManager.AddMiss();
            UI_RhythmHUD.Instance.ShowFeedback("MISS");

            Destroy(gameObject);
        }
    }

}
