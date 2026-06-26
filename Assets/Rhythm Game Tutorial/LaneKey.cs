using UnityEngine;

public class LaneKey : MonoBehaviour
{
    public int laneIndex;
    public KeyCode hitKey;

    public AudioSource musicSource;

    [Header("Anti Mash")]
    [Tooltip("Minimum time between accepted key presses on this lane.")]
    public float mashCooldown = 0.075f;

    private float nextAllowedPressTime = 0f;

    // Prevent multiple wrong penalties if several keys are pressed in one frame.
    private static int s_lastNoteHitFrame = -1;
    private static int s_lastWrongFrame = -1;

    [Header("Timing Window")]
    public HitWindow hitWindow;

    void Update()
    {
        if (Time.time < nextAllowedPressTime)
            return;

        if (Input.GetKeyDown(hitKey))
        {
            nextAllowedPressTime = Time.time + mashCooldown;
            TryHit();
        }
    }

    private void TryHit()
    {
        Note[] notes = Object.FindObjectsByType<Note>(FindObjectsSortMode.None);

        Note best = null;
        float bestDiff = float.MaxValue;

        foreach (var n in notes)
        {
            if (n == null)
                continue;

            if (n.lane != laneIndex)
                continue;

            if (!n.canBeHit || n.wasHit)
                continue;

            float songTime;

            if (NoteSpawner.globalSongStartDspTime > 0.0)
            {
                songTime = (float)(AudioSettings.dspTime - NoteSpawner.globalSongStartDspTime);
            }
            else if (n.musicSource != null && n.musicSource.isPlaying)
            {
                songTime = n.musicSource.time;
            }
            else if (musicSource != null && musicSource.isPlaying)
            {
                songTime = musicSource.time;
            }
            else
            {
                songTime = Time.time;
            }

            songTime += NoteSpawner.globalTimingOffset;

            float diff = Mathf.Abs(songTime - n.targetTime);

            // Ignore notes outside the largest timing window.
            if (hitWindow != null && diff > hitWindow.badRange)
                continue;

            if (diff < bestDiff)
            {
                best = n;
                bestDiff = diff;
            }
        }

        // Successful hit
        if (best != null)
        {
            float songTime;

            if (NoteSpawner.globalSongStartDspTime > 0.0)
            {
                songTime = (float)(AudioSettings.dspTime - NoteSpawner.globalSongStartDspTime);
            }
            else if (best.musicSource != null && best.musicSource.isPlaying)
            {
                songTime = best.musicSource.time;
            }
            else
            {
                songTime = musicSource != null ? musicSource.time : Time.time;
            }

            songTime += NoteSpawner.globalTimingOffset;

            float diff = songTime - best.targetTime;

            best.Hit(diff);

            s_lastNoteHitFrame = Time.frameCount;

            return;
        }

        // Ignore if another lane already scored this frame.
        if (s_lastNoteHitFrame == Time.frameCount)
            return;

        // Only one wrong penalty per frame.
        if (s_lastWrongFrame == Time.frameCount)
            return;

        s_lastWrongFrame = Time.frameCount;

        MiniScoreManager.AddWrongPress();

        if (UI_RhythmHUD.Instance != null)
            UI_RhythmHUD.Instance.ShowFeedback("WRONG");
    }
}