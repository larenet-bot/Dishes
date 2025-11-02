using UnityEngine;

public class RhythmGameManager : MonoBehaviour
{
    public AudioSource musicSource;
    public NoteSpawner noteSpawner;
    public HitZone hitZone;

    private bool running = false;

    void Start()
    {
        // ensure musicSource.clip is set in inspector or via NoteSpawner
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();
        if (noteSpawner == null)
            noteSpawner = Object.FindFirstObjectByType<NoteSpawner>();
    }

    public void StartGame()
    {
        if (running) return;
        // Optionally pause/disable player input in main game:
        var clicker = Object.FindFirstObjectByType<DishClicker>();
        if (clicker != null) clicker.enabled = false;

        // start music and spawning
        if (musicSource != null) musicSource.Play();
        if (noteSpawner != null) noteSpawner.StartSpawning();
        running = true;
    }

    public void StopGame()
    {
        if (!running) return;
        if (musicSource != null) musicSource.Stop();
        if (noteSpawner != null) noteSpawner.StopSpawning();

        var clicker = Object.FindFirstObjectByType<DishClicker>();
        if (clicker != null) clicker.enabled = true;

        running = false;
    }

    // used by HitZone to get current song time
    public float GetSongTime()
    {
        return (musicSource != null && musicSource.isPlaying) ? musicSource.time : 0f;
    }

    public void NoteHit(Note note, float delta)
    {
        // reward player — example: give small profit bonus
        // ScoreManager.Instance.AddPoints(...);
        // optionally call ScoreManager or Score system
        Debug.Log($"Hit! lane {note.lane}, delta {delta:F3}s");
    }

    public void NoteMissed()
    {
        Debug.Log("Missed");
    }
    void Update()
    {
        // Press Space to start or stop the rhythm game
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!running)
            {
                StartGame();
            }
            else
            {
                StopGame();
            }
        }
    }

}
