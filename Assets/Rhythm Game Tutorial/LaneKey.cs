using UnityEngine;

public class LaneKey : MonoBehaviour
{
    public int laneIndex;
    public KeyCode hitKey;

    public AudioSource musicSource;  // assigned in inspector or by manager (optional)

    // Tracks if any note was successfully hit this frame so other keys pressed the same frame
    // won't register as "wrong" and trigger immediate conflicting feedback.
    private static int s_lastNoteHitFrame = -1;

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
        bool anyHittable = false; // any note in any lane currently hittable

        // choose the closest valid note (smallest absolute timing difference)
        foreach (var n in notes)
        {
            if (n == null) continue;

            // track whether any note is in the hit zone (regardless of lane)
            if (n.canBeHit && !n.wasHit)
                anyHittable = true;

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

            // remember that a successful hit occurred this frame
            s_lastNoteHitFrame = Time.frameCount;

            Debug.Log($"[LaneKey] Lane {laneIndex} hit; diff={diff:0.000}s (bestDiff={bestDiff:0.000}s) songTime={songTime:0.000}s target={best.targetTime:0.000}s offset={NoteSpawner.globalTimingOffset:0.000}s");
        }
        else
        {
            // No hittable note in this lane.
            // If there are notes in the hit zone, treat this as a wrong key press and penalize.
            if (anyHittable)
            {
                // If another key already hit a note this same frame, skip marking this as a wrong press.
                // This prevents "Perfect" feedback followed immediately by a "Wrong"/"Miss" from other keys processed later that frame.
                if (s_lastNoteHitFrame == Time.frameCount)
                {
                    Debug.Log($"[LaneKey] Lane {laneIndex} pressed same frame as a successful hit; skipping wrong-press penalty.");
                    return;
                }

                MiniScoreManager.AddWrongPress();
                if (UI_RhythmHUD.Instance != null)
                    UI_RhythmHUD.Instance.ShowFeedback("WRONG");

                // Mark currently hittable notes as consumed so subsequent presses won't score them.
                foreach (var n in notes)
                {
                    if (n == null) continue;
                    if (n.canBeHit && !n.wasHit)
                    {
                        n.wasHit = true; // prevent later scoring
                    }
                }

                Debug.Log($"[LaneKey] Lane {laneIndex} wrong key press while other notes are in hit zone. Hittable notes invalidated.");
            }
            else
            {
                // No notes in any hit zone — do nothing (optional: feedback or soft penalty can be added)
                Debug.Log($"[LaneKey] Lane {laneIndex} pressed but no notes in any hit zone.");
            }
        }
    }
}