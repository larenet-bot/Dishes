using UnityEngine;

public class LaneKey : MonoBehaviour
{
    public int laneIndex;
    public KeyCode hitKey;

    public AudioSource musicSource;  // assigned in inspector or by manager

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

        foreach (var n in notes)
        {
            if (n.lane == laneIndex && n.canBeHit && !n.wasHit)
            {
                best = n;
                break;
            }
        }

        if (best != null)
        {
            best.wasHit = true;

            float songTime = musicSource.time;
            float diff = songTime - best.targetTime;

            best.Hit(diff);
        }
    }
}
