using UnityEngine;

public class beatscroller : MonoBehaviour
{
    // speed in units per second toward negative Y (or change to match your layout)
    public float scrollSpeed = 5f;
    public bool hasStarted = false;
    public float destroyY = -5f; // position at which the note gets destroyed

    void Update()
    {
        if (!hasStarted) return;

        transform.position += Vector3.down * scrollSpeed * Time.deltaTime;

        // destroy if offscreen
        if (transform.position.y <= destroyY)
        {
            Destroy(gameObject);
        }
    }
}
