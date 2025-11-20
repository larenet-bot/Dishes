using UnityEngine;

public class Rythmstart : MonoBehaviour
{
    public NoteSpawner spawner;

    void Start()
    {
        spawner.StartSpawning();
        spawner.musicSource.Play();
    }
}
