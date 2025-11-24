using UnityEngine;

public class LaneEditor : MonoBehaviour
{
    public int laneIndex;

    public Transform noteParent;
    public GameObject notePrefab;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    private void OnMouseDown()
    {
        Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;

        // Snap note vertically to timeline:
        GameObject note = Instantiate(notePrefab, worldPos, Quaternion.identity, noteParent);
        note.GetComponent<Note>().lane = laneIndex;

        Debug.Log($"Placed note in lane {laneIndex} at Y={worldPos.y}");
    }
}
