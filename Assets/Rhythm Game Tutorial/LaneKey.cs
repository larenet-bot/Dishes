using UnityEngine;

public class LaneKey : MonoBehaviour
{
    public int laneIndex;
    public KeyCode hitKey;

    public HitWindow hitWindow;      // assign in inspector
    public AudioSource musicSource;  // assign the same AudioSource used for timing

    void Update()
    {
        if (Input.GetKeyDown(hitKey))
        {
            TryHit();
        }
    }

    private void TryHit()
    {
        Debug.Log("KEY PRESSED on lane " + laneIndex);

        // Find the nearest hittable note in this lane BEFORE exit can trigger
        Note[] notes = Object.FindObjectsByType<Note>(FindObjectsSortMode.None);
        Note best = null;

        foreach (var n in notes)
        {
            if (n.lane == laneIndex && n.canBeHit && !n.wasHit)
            {
                best = n;
                break; // stop at the first hittable note
            }
        }

        if (best != null)
        {
            Debug.Log("Found note with targetTime = " + best.targetTime + " | canBeHit = " + best.canBeHit);

            // Mark as hit immediately so OnTriggerExit2D does not count it as a miss
            best.wasHit = true;
            best.Hit();
        }
    }
}
