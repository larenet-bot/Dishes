using UnityEngine;

public class NoteHitManager : MonoBehaviour
{
    public RectTransform hitZoneRect;
    public float hitTolerance = 50f; // pixels

    public static NoteHitManager Instance;

    [Header("Lane Hit Keys (match BeatScroller lanes)")]
    public KeyCode[] laneKeys = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        for (int i = 0; i < laneKeys.Length; i++)
        {
            if (Input.GetKeyDown(laneKeys[i]))
            {
                CheckHitInLane(i);
            }
        }
    }

    void CheckHitInLane(int lane)
    {
        // Find all notes in this lane
        BeatScroller[] notes = Object.FindObjectsByType<BeatScroller>(FindObjectsSortMode.None);

        BeatScroller closest = null;
        float closestDist = Mathf.Infinity;

        foreach (BeatScroller n in notes)
        {
            if (n.lane != lane || n.wasHit) continue;

            float dist = Mathf.Abs(n.transform.position.y - n.hitY);
            if (dist < n.hitTolerance && dist < closestDist)
            {
                closest = n;
                closestDist = dist;
            }
        }

        // Hit the closest valid note
        if (closest != null)
        {
            closest.OnHit();
        }
    }
}
