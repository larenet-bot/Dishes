using UnityEngine;

public class LaneKey : MonoBehaviour
{
    public int laneIndex;
    public KeyCode hitKey;

    public HitWindow hitWindow;  // assign in inspector
    public AudioSource musicSource; // assign the same AudioSource used for timing

    void Update()
    {
        if (Input.GetKeyDown(hitKey))
        {
            TryHit();
        }
    }

    private void TryHit()
    {
        // 1. Get the next note for this lane
        Note nextNote = NoteRegistry.GetNextNote(laneIndex);
        if (nextNote == null) return;

        // 2. Only judge if note is inside the hitbox
        if (!nextNote.canBeHit) return;

        // --- FIX 1: Remove BEFORE judging ---
        NoteRegistry.PopNote(laneIndex);
        Destroy(nextNote.gameObject);

        // 3. Calculate timing difference
        float diff = Mathf.Abs(musicSource.time - nextNote.targetTime);

        // --- FIX 2: Feed absolute timing difference ---
        hitWindow.JudgeNoteHit(diff);
    }
}
