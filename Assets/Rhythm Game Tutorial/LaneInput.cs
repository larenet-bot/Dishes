using UnityEngine;

public class LaneInput : MonoBehaviour
{
    public KeyCode hitKey; // assign different keys per lane
    public Transform hitZone; // assign the lane's hit zone
    public float hitWindow = 0.2f; // timing tolerance

    void Update()
    {
        if (Input.GetKeyDown(hitKey))
        {
            Note closestNote = FindClosestNote();
            if (closestNote != null)
            {
                float offset = Mathf.Abs(closestNote.transform.position.y - hitZone.position.y);
                if (offset <= hitWindow)
                {
                    Debug.Log($"Hit note at lane {closestNote.lane}!");
                    closestNote.Hit();
                }
            }
        }
    }

    Note FindClosestNote()
    {
        // FIX: Use FindObjectsByType with FindObjectsSortMode.None instead of deprecated FindObjectsOfType
        Note[] notes = Object.FindObjectsByType<Note>(FindObjectsSortMode.None);
        Note closest = null;
        float minDist = float.MaxValue;

        foreach (Note n in notes)
        {
            if (n.lane != int.Parse(gameObject.name.Substring(4))) continue; // Lane1 => 1
            float dist = Mathf.Abs(n.transform.position.y - hitZone.position.y);
            if (dist < minDist)
            {
                minDist = dist;
                closest = n;
            }
        }

        return closest;
    }
}
