using UnityEngine;

public class LaneKey : MonoBehaviour
{
    public int laneIndex;
    public KeyCode hitKey;
    public Transform hitZone;
    public float hitWindow = 0.2f;

    void Update()
    {
        if (Input.GetKeyDown(hitKey))
        {
            Debug.Log($"Lane {laneIndex} key pressed ({hitKey})");
            TryHit();
        }
    }


    private void TryHit()
    {
        Note nextNote = NoteRegistry.GetNextNote(laneIndex);

        if (nextNote == null)
        {
            Debug.Log($"Lane {laneIndex}: HIT PRESSED but NO notes available.");
            return;
        }

        float dist = Mathf.Abs(nextNote.transform.position.y - hitZone.position.y);

        Debug.Log($"Lane {laneIndex}: Attempting to hit note {nextNote.GetInstanceID()} | dist = {dist}");

        if (dist <= hitWindow)
        {
            Debug.Log($"Lane {laneIndex}: SUCCESS hit note {nextNote.GetInstanceID()}");
            nextNote.Hit();
            NoteRegistry.PopNote(laneIndex);
        }
        else
        {
            Debug.Log($"Lane {laneIndex}: MISS — note {nextNote.GetInstanceID()} out of timing window.");
        }
    }

}
