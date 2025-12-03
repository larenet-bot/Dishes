using UnityEngine;

public class Rythmstart : MonoBehaviour
{
    public NoteSpawner spawner;
    private bool hasStarted = false;

    void Update()
    {
        if (!hasStarted && Input.GetKeyDown(KeyCode.Space))
        {
            hasStarted = true;
            spawner.StartSpawning();
        }
    }
}
