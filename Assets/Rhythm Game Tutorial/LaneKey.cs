using UnityEngine;

public class LaneKey : MonoBehaviour
{
    public int laneIndex;
    public KeyCode hitKey;


    public HitWindow hitWindow; // assign in inspector
    public AudioSource musicSource; // assign your NoteSpawner's AudioSource


    void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyDown(hitKey))
        {
            TryHit();
        }
    }

    private void TryHit()
    {
        Note nextNote = NoteRegistry.GetNextNote(laneIndex);
        if (nextNote == null) return;
        if (!nextNote.canBeHit) return;

        float diff = musicSource.time - nextNote.targetTime;
        hitWindow.JudgeNoteHit(diff);

        NoteRegistry.PopNote(laneIndex);
        Destroy(nextNote.gameObject);
    }

}
