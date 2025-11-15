using UnityEngine;

public class BeatScroller : MonoBehaviour
{
    [Header("Movement")]
    public float scrollSpeed = 5f;
    public bool hasStarted = false;
    public Vector3 moveDirection = Vector3.down;

    [Header("Destruction")]
    public float destroyY = -5f;

    [Header("Hit Settings")]
    public float hitY = 0f;
    public float hitTolerance = 0.5f;
    public int lane;
    [HideInInspector] public bool wasHit = false;

    void Update()
    {
        if (!hasStarted) return;

        transform.position += moveDirection * scrollSpeed * Time.deltaTime;

        if (transform.position.y <= destroyY)
        {
            Destroy(gameObject);
        }
    }

    public void OnHit()
    {
        if (wasHit) return;
        wasHit = true;

        Debug.Log($"Note HIT in lane {lane}");
        Destroy(gameObject);
    }
}
