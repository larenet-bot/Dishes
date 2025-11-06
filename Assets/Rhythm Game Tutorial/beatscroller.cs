using UnityEngine;

public class BeatScroller : MonoBehaviour
{
    [Header("Movement")]
    public float scrollSpeed = 5f;      // How fast the note moves
    public bool hasStarted = false;     // Controlled by NoteSpawner
    public Vector3 moveDirection = Vector3.down; // Adjust if game moves upward

    [Header("Destruction")]
    public float destroyY = -5f;        // Y threshold for auto destroy 

    [Header("Hit Detection")]
    public float hitY = 0f;             // The Y position of the hit line
    public float hitTolerance = 0.5f;   // Timing window tolerance

    [Header("Lane Settings")]
    public int lane = 0;                // Set by NoteSpawner
    public KeyCode[] laneKeys = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };

    private bool wasHit = false;

    void Update()
    {
        if (!hasStarted) return;

        // Move note
        transform.position += moveDirection * scrollSpeed * Time.deltaTime;

        // Destroy when it goes offscreen
        if (transform.position.y <= destroyY)
        {
            Destroy(gameObject);
            return;
        }

        // Multi-lane key detection
        if (!wasHit && lane < laneKeys.Length && Input.GetKeyDown(laneKeys[lane]))
        {
            if (Mathf.Abs(transform.position.y - hitY) <= hitTolerance)
            {
                OnHit();
            }
        }
    }

  

    private void OnHit()
    {
        if (wasHit) return;
        wasHit = true;

        Debug.Log($"Note hit! Lane {lane}, Timing diff = {Mathf.Abs(transform.position.y - hitY):F3}");

        // TODO: Add  scoring / combo / effects here
        

        Destroy(gameObject);
    }
}
