using UnityEngine;

public class Note : MonoBehaviour
{
    public int lane;
    public bool canBeHit;

    // Add this method to fix CS1061
    public void Hit()
    {
        // Implement note hit logic here (e.g., destroy note, play sound, etc.)
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("HitBox"))
            canBeHit = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("HitBox"))
            canBeHit = false;
    }
}
