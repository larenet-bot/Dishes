using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public bool startPlaying;
    public BeatScroller theBS;      // matches your beat scroller script
    public AudioSource theMusic;    // uncomment this if you want the main music here

    private void Start()
    {
        instance = this;
    }

    private void Update()
    {
        if (!startPlaying)
        {
            if (Input.anyKeyDown)
            {
                startPlaying = true;
                theBS.hasStarted = true;
                theMusic.Play();
            }
        }
    }

    public void NoteHit()
    {
        Debug.Log("Hit");
    }

    public void NoteMissed()
    {
        Debug.Log("Missed");
    }
}
