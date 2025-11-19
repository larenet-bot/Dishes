using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("Prefab / Spawn")]
    public GameObject notePrefab;
    public Transform[] laneSpawnPoints;
    public float spawnYOffset = 0f;

    [Header("Song / Audio")]
    public AudioSource musicSource;
    public AudioClip songClip;

    [Header("Chart File (.txt)")]
    public TextAsset chartFile;   // <-- assign your .txt file here

    [Header("Spawn Settings")]
    public float timeAhead = 2.0f;     // spawn notes early so they scroll perfectly
    public float noteScrollSpeed = 5f;

    // ---------------------------------------------------------

    private class ParsedNote
    {
        public float time;
        public int lane;
    }

    private List<ParsedNote> parsedNotes = new List<ParsedNote>();
    private int nextIndex = 0;
    private bool spawning = false;

    // ---------------------------------------------------------

    void Start()
    {
        if (musicSource == null && songClip != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = songClip;
        }

        LoadChart();
    }

    // ---------------------------------------------------------
    // Load .txt chart file
    // ---------------------------------------------------------
    private void LoadChart()
    {
        parsedNotes.Clear();
        nextIndex = 0;

        if (chartFile == null)
        {
            Debug.LogError("No chart file assigned to NoteSpawner!");
            return;
        }

        string[] lines = chartFile.text.Split('\n');

        foreach (string raw in lines)
        {
            string line = raw.Trim();

            // Skip comments and empty lines
            if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                continue;

            string[] parts = line.Split(' ');

            if (parts.Length < 2)
                continue;

            float time;
            int lane;

            if (!float.TryParse(parts[0], out time))
            {
                Debug.LogWarning($"Invalid time value in chart: '{parts[0]}'");
                continue;
            }

            if (!int.TryParse(parts[1], out lane))
            {
                Debug.LogWarning($"Invalid lane value in chart: '{parts[1]}'");
                continue;
            }

            if (lane < 0 || lane >= laneSpawnPoints.Length)
            {
                Debug.LogWarning($"Lane {lane} out of range! Skipping.");
                continue;
            }

            parsedNotes.Add(new ParsedNote { time = time, lane = lane });
        }

        Debug.Log($"Loaded {parsedNotes.Count} notes from '{chartFile.name}'.");
    }

    // ---------------------------------------------------------

    void Update()
    {
        if (!spawning || musicSource == null) return;
        if (nextIndex >= parsedNotes.Count) return;

        float songTime = musicSource.time;

        ParsedNote note = parsedNotes[nextIndex];

        // Time to spawn?
        if (songTime >= note.time - timeAhead)
        {
            SpawnSpecific(note.lane);
            nextIndex++;
        }
    }

    // ---------------------------------------------------------

    public void StartSpawning()
    {
        nextIndex = 0;
        spawning = true;
        Debug.Log("Spawner started!");
    }

    public void StopSpawning()
    {
        spawning = false;
    }

    // ---------------------------------------------------------
    // Spawn a note at a specific lane
    // ---------------------------------------------------------
    private void SpawnSpecific(int lane)
    {
        Transform spawnPoint = laneSpawnPoints[lane];
        Vector3 spawnPos = spawnPoint.position + new Vector3(0, spawnYOffset, 0);
        spawnPos.z = 0;

        GameObject g = Instantiate(notePrefab, spawnPos, Quaternion.identity, transform);

        // Set scroll speed
        BeatScroller scroller = g.GetComponent<BeatScroller>();
        if (scroller != null)
        {
            scroller.scrollSpeed = noteScrollSpeed;
            scroller.hasStarted = true;
        }

        // Optional: set lane on Note component
        Note note = g.GetComponent<Note>();
        if (note != null)
            note.lane = lane;
    }
}
