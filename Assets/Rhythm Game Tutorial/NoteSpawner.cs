using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("Prefab / Spawn")]
    public GameObject notePrefab;
    public Transform[] laneSpawnPoints; // assign transforms for each lane (x,y spawn positions)
    public float spawnYOffset = 0f; // additional offset if needed

    [Header("Timing")]
    public AudioSource musicSource;
    public AudioClip songClip;
    public float songBPM = 120f;
    public bool generateByBPM = true;
    public List<float> customNoteTimes = new List<float>(); // seconds

    [Header("Spawn Settings")]
    public float timeAhead = 2.0f; // how many seconds before targetTime to spawn the note
    public float noteScrollSpeed = 5f; // passed to BeatScroller on spawn

    private List<float> noteTimes = new List<float>();
    private int nextIndex = 0;
    private bool spawning = false;

    void Start()
    {
        if (musicSource == null && songClip != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = songClip;
        }

        if (generateByBPM)
            GenerateByBPM();

        // if using custom list, copy
        if (!generateByBPM)
            noteTimes = new List<float>(customNoteTimes);
        
    }

    void Update()
    {
        if (spawning && musicSource != null)
        {
            Debug.Log($"Spawner running... time={musicSource.time:F2}");
        }
        if (!spawning || musicSource == null || nextIndex >= noteTimes.Count) return;

        float songTime = musicSource.time;
        // spawn when current time reaches (target - timeAhead)
        if (songTime >= noteTimes[nextIndex] - timeAhead)
        {
            SpawnAtTime(noteTimes[nextIndex]);
            nextIndex++;
        }
    }

    public void StartSpawning()
    {
        nextIndex = 0;
        spawning = true;
        Debug.Log($"Spawner started! noteTimes count = {noteTimes.Count}");
    }


    public void StopSpawning()
    {
        spawning = false;
    }

    private void GenerateByBPM()
    {
        noteTimes.Clear();
        if (songClip == null || songBPM <= 0f) return;

        float beatInterval = 60f / songBPM;
        float songLength = songClip.length;

        for (float t = 0f; t < songLength; t += beatInterval)
        {
            if (t > timeAhead) // only spawn beats far enough into song
                noteTimes.Add(t);
        }
    }


    private void SpawnAtTime(float targetTime)
    {
        int laneIndex = Random.Range(0, laneSpawnPoints.Length);
        Transform spawnPoint = laneSpawnPoints[laneIndex];
        Vector3 spawnPos = spawnPoint.position + new Vector3(0, spawnYOffset, 0);
        Debug.Log($"Spawning note at lane {laneIndex}, pos={spawnPos}");
        spawnPos.z = 0; 
        GameObject g = Instantiate(notePrefab, spawnPos, Quaternion.identity, transform);
        Note note = g.GetComponent<Note>();
        BeatScroller scroller = g.GetComponent<BeatScroller>();
        if (note != null) note.lane = laneIndex;
        if (note != null) note.targetTime = targetTime;
        if (scroller != null)
        {
            scroller.scrollSpeed = noteScrollSpeed;
            scroller.hasStarted = true;
        }
    }


}
