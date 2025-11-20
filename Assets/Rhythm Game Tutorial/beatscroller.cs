using UnityEngine;

public class BeatScroller : MonoBehaviour
{
    public float scrollSpeed = 5f;
    public bool hasStarted = false;

    void Update()
    {
        if (!hasStarted) return;

        // Move down
        transform.position += Vector3.down * scrollSpeed * Time.deltaTime;

        // Clean up if offscreen
        if (transform.position.y <= -5f)
        {
            Destroy(gameObject);
        }
    }
}
