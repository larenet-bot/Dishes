using UnityEngine;

public class LaneKey : MonoBehaviour
{
    public int laneIndex;
    public KeyCode hitKey;

    public AudioSource musicSource;  // assigned in inspector or by manager (optional)

    void Update()
    {
        if (Input.GetKeyDown(hitKey))
        {
            TryHit();
        }
    }

    private void TryHit()
    {
        Note[] notes = Object.FindObjectsByType<Note>(FindObjectsSortMode.None);
        Note best = null;
        float bestDiff = float.MaxValue;

        // choose the closest valid note (smallest absolute timing difference)
        foreach (var n in notes)
        {
            if (n == null) continue;
            if (n.lane == laneIndex && n.canBeHit && !n.wasHit)
            {
                // prefer DSP-based song time (most accurate)
                float songTimeForNote;
                if (NoteSpawner.globalSongStartDspTime > 0.0)
                {
                    songTimeForNote = (float)(AudioSettings.dspTime - NoteSpawner.globalSongStartDspTime);
                }
                else if (n.musicSource != null && n.musicSource.isPlaying)
                {
                    songTimeForNote = n.musicSource.time;
                }
                else if (musicSource != null && musicSource.isPlaying)
                {
                    songTimeForNote = musicSource.time;
                }
                else
                {
                    // last-resort fallback (not recommended for accuracy)
                    songTimeForNote = Time.time;
                }

                float diff = Mathf.Abs(songTimeForNote - n.targetTime);
                if (diff < bestDiff)
                {
                    best = n;
                    bestDiff = diff;
                }
            }
        }

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

            // Apply global timing offset (tunable in NoteSpawner)
            songTime += NoteSpawner.globalTimingOffset;

            float diff = songTime - best.targetTime;

            // Let Note.Hit mark wasHit and handle scoring
            best.Hit(diff);

            Debug.Log($"[LaneKey] Lane {laneIndex} hit; diff={diff:0.000}s (bestDiff={bestDiff:0.000}s) songTime={songTime:0.000}s target={best.targetTime:0.000}s offset={NoteSpawner.globalTimingOffset:0.000}s");
        }
    }
}