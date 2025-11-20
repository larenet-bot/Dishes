using UnityEngine;

public class Note : MonoBehaviour
{
    public int lane;
    public bool canBeHit;

    //private LaneInput laneInput;

    void Start()
    {
        // Find the correct lane and register this note with it
       // laneInput = Object.FindFirstObjectByType<LaneInputManager>().GetLane(lane);
       // laneInput.RegisterNote(this);
    }

    public void Hit()
    {
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
