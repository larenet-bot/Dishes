using UnityEngine;

public class BeatScroller : MonoBehaviour
{
    public float scrollSpeed = 5f;
    public bool hasStarted = false;

    public float hitY = 0f;
    public float hitTolerance = 0.5f;
    public int lane;

    private bool wasHit = false;

    void Update()
    {
        if (!hasStarted) return;

        transform.position += Vector3.down * scrollSpeed * Time.deltaTime;

        if (transform.position.y <= hitY + hitTolerance)
        {
            // Optional: Broadcast "miss" here 
        }

        if (transform.position.y <= -5f)
        {
            Destroy(gameObject);
        }
    }

    public bool TryHit()
    {
        if (wasHit) return false;

        float dist = Mathf.Abs(transform.position.y - hitY);

        if (dist <= hitTolerance)
        {
            wasHit = true;
            Destroy(gameObject);
            return true;
        }

        return false;
    }
}
