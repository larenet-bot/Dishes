// GameManager.cs
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public bool startPlaying;
    public BeatScroller theBS;
    public AudioSource theMusic;

    // ADD THIS
    public NoteSpawner noteSpawner;

    private void Start()
    {
        instance = this;
    }

    private void Update()
    {
        if (!startPlaying)
        {
            // SPACE to start
            if (Input.GetKeyDown(KeyCode.Space))
            {
                startPlaying = true;

                if (theBS != null)
                    theBS.hasStarted = true;

                if (theMusic != null)
                    theMusic.Play();

                if (noteSpawner != null)
                    noteSpawner.StartSpawning();
            }
        }
    }
    public void NoteMissed()
    {
        // Whatever needs to happen on a miss
        // Example:
        // scoreManager.Miss();
        // combo = 0;

        Debug.Log("Note missed");
    }

}
