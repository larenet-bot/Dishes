using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class NoteSpawner : MonoBehaviour
{
    [Header("Prefab / Spawn")]
    public GameObject notePrefab;
    public Transform[] laneSpawnPoints;
    public float spawnYOffset = 0f;

    [Header("Song / Audio")]
    public AudioMixerGroup musicGroup;
    public AudioSource musicSource;
    public AudioClip songClip;

    [Header("Chart File (.txt)")]
    public TextAsset chartFile;

    [Header("Spawn Settings")]
    public float timeAhead = 2.0f;
    public float noteScrollSpeed = 5f;

    // ----------------------
    private class ParsedNote
    {
        public float time;
        public int lane;
    }

    private List<ParsedNote> parsedNotes = new List<ParsedNote>();
    private int nextIndex = 0;
    private bool spawning = false;
    // ----------------------

    void Start()
    {
        if (musicSource == null && songClip != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = songClip;
            musicSource.outputAudioMixerGroup = musicGroup;
        }

        LoadChart();
        Debug.Log("Loaded " + parsedNotes.Count + " notes.");
    }

    // ----------------------
    // Load .txt chart file
    // ----------------------
    private void LoadChart()
    {
        parsedNotes.Clear();
        nextIndex = 0;

        if (!chartFile)
        {
            Debug.LogError("No chart file assigned!");
            return;
        }

        string[] lines = chartFile.text.Split('\n');

        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("[")) continue;

            string[] parts = line.Split(':');
            if (parts.Length < 5) continue;

            string timeString = parts[0].Trim();
            if (timeString.Equals("NaN", System.StringComparison.OrdinalIgnoreCase)) continue;

            if (!float.TryParse(timeString, out float timeMs))
                continue;

            float timeSec = timeMs / 1000f;

            if (!int.TryParse(parts[3].Trim(), out int lane))
                continue;

            if (lane < 0 || lane >= laneSpawnPoints.Length)
                continue;

            parsedNotes.Add(new ParsedNote
            {
                time = timeSec,
                lane = lane
            });
        }

        parsedNotes.RemoveAll(n => float.IsNaN(n.time));
        parsedNotes.Sort((a, b) => a.time.CompareTo(b.time));
    }

    // ----------------------
    void Update()
    {
        if (!spawning || musicSource == null) return;
        if (nextIndex >= parsedNotes.Count) return;

        float songTime = musicSource.time;
        ParsedNote note = parsedNotes[nextIndex];

        if (songTime >= note.time - timeAhead)
        {
            SpawnSpecific(note.lane);
            nextIndex++;
        }
    }

    // ----------------------
    public void StartSpawning()
    {
        nextIndex = 0;
        spawning = true;

        if (musicSource != null)
            musicSource.Play();

        Debug.Log("Spawner started!");
    }

    public void StopSpawning()
    {
        spawning = false;
        if (musicSource != null)
            musicSource.Stop();
    }

    // ----------------------
    private void SpawnSpecific(int lane)
    {
        Transform spawnPoint = laneSpawnPoints[lane];
        Vector3 spawnPos = spawnPoint.position + new Vector3(0, spawnYOffset, 0);
        spawnPos.z = 0;

        GameObject g = Instantiate(notePrefab, spawnPos, Quaternion.identity, transform);

        BeatScroller scroller = g.GetComponent<BeatScroller>();
        if (scroller != null)
        {
            scroller.scrollSpeed = noteScrollSpeed;
            scroller.hasStarted = true;
        }

        Note note = g.GetComponent<Note>();
        if (note != null)
        {
            note.lane = lane;
            NoteRegistry.RegisterNote(lane, note);
        }
    }
}
