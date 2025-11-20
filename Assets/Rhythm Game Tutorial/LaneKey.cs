using UnityEngine;

public class LaneKey : MonoBehaviour
{
    public KeyCode key;
    public int laneId;

    void Update()
    {
        if (Input.GetKeyDown(key))
        {
            TryHitLane();
        }
    }

    void TryHitLane()
    {
        // Use FindObjectsByType instead of FindObjectsOfType to fix CS0618
        BeatScroller[] notes = Object.FindObjectsByType<BeatScroller>(FindObjectsSortMode.None);

        BeatScroller best = null;
        float bestDist = float.MaxValue;

        foreach (var note in notes)
        {
            if (note.lane != laneId) continue;

            float dist = Mathf.Abs(note.transform.position.y - note.hitY);

            if (dist < bestDist)
            {
                bestDist = dist;
                best = note;
            }
        }

        if (best != null && best.TryHit())
        {
            Debug.Log($"HIT lane {laneId}");
        }
    }
}
