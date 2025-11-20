using UnityEngine;

public class Note : MonoBehaviour
{
    [HideInInspector] public int lane;   // Assigned by spawner
    public bool canBeHit = false;

    // Optional: events / feedback hooks
    public System.Action<Note> OnHit;

    public void Hit()
    {
        // Trigger callback (e.g., score, effects)
        OnHit?.Invoke(this);

        // Destroy the note
        Destroy(gameObject);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("HitBox"))
        {
            canBeHit = true;
            Debug.Log($"NOTE {gameObject.GetInstanceID()} lane {lane} ENTERED HIT ZONE");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("HitBox"))
        {
            canBeHit = false;
            Debug.Log($"NOTE {gameObject.GetInstanceID()} lane {lane} EXITED HIT ZONE");
        }
    }

    void Start()
    {
        Debug.Log($"Note spawned in lane {lane}");
    }

}
