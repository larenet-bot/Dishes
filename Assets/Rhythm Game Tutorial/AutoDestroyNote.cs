using UnityEngine;

public class NoteAutoDestroy : MonoBehaviour
{
    [Header("Y value where note despawns")]
    public float destroyY = -6f;

    void Update()
    {
        if (transform.position.y <= destroyY)
        {
            Destroy(gameObject);
        }
    }
}
