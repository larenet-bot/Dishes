using UnityEngine;

public class Note : MonoBehaviour
{
    public int lane;            // Which lane this note belongs to
    public float speed = 5f;

    void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        // Destroy if offscreen
        if (transform.position.y < -6f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HitZone"))
        {
            // Ready to be hit
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("HitZone"))
        {
            // Missed the note
            Destroy(gameObject);
        }
    }
}

