using UnityEngine;

public class Rythmstart : MonoBehaviour
{
    public NoteSpawner spawner;  // assign in inspector
    private bool hasStarted = false;

    void Update()
    {
        if (!hasStarted && Input.GetKeyDown(KeyCode.Space))
        {
            hasStarted = true;

            if (spawner != null)
            {
                spawner.StartSpawning();
                Debug.Log("Spawning started!");
            }
            else
            {
                Debug.LogWarning("No spawner assigned to Rythmstart!");
            }
        }
    }
}
