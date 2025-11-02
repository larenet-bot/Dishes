using UnityEngine;

public class Note : MonoBehaviour
{
    // lane index this note belongs to (0..n)
    public int lane = 0;

    // flagged when note is hit to avoid double-hit
    [HideInInspector] public bool hit = false;

    // optional: timestamp (seconds) when this note should arrive at the hit zone
    public float targetTime = 0f;
}
