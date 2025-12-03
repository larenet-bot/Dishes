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
    public TextAsset chartFile; // <-- assign your .txt file here

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
        

        LoadChart();

        if (parsedNotes.Count > 0)
            Debug.Log("First note time: " + parsedNotes[0].time);
    }

    // ---------------------------------------------------------
    // Load .txt chart file
    // ---------------------------------------------------------
    private class BPMChange
    {
        public float beat;
        public float bpm;
    }

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
            if (parts.Length < 5)
            {
                Debug.LogWarning("Invalid line: " + line);
                continue;
            }

            // ---- trim the time string first ----
            string timeString = parts[0].Trim();

            // ---- explicitly skip NaN ----
            if (timeString.Equals("NaN", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Skipping NaN timestamp line: " + line);
                continue;
            }

            // ---- TryParse AFTER trimming ----
            if (!float.TryParse(timeString, out float timeMs))
            {
                Debug.LogWarning("Skipping invalid time: '" + timeString + "'");
                continue;
            }

            float timeSec = timeMs / 1000f;

            // ---- LANE ----
            if (!int.TryParse(parts[3].Trim(), out int lane))
            {
                Debug.LogWarning("Invalid lane: " + parts[3]);
                continue;
            }

            if (lane < 0 || lane >= laneSpawnPoints.Length)
            {
                Debug.LogWarning("Lane out of range: " + lane);
                continue;
            }

            parsedNotes.Add(new ParsedNote { time = timeSec, lane = lane });
        }

        // Remove any accidental NaN entries (safety)
        parsedNotes.RemoveAll(n => float.IsNaN(n.time));

        // Sort only valid notes
        parsedNotes.Sort((a, b) => a.time.CompareTo(b.time));

        Debug.Log($"Loaded {parsedNotes.Count} notes (after removing NaN lines).");
    }

    private float ConvertBeatToSeconds(float beat, List<BPMChange> bpmChanges)
    {
        float seconds = 0f;

        for (int i = 0; i < bpmChanges.Count; i++)
        {
            BPMChange curr = bpmChanges[i];
            BPMChange next = (i + 1 < bpmChanges.Count) ? bpmChanges[i + 1] : null;

            float bpm = curr.bpm;

            if (next != null && beat >= curr.beat && beat < next.beat)
            {
                // Inside this BPM range
                return seconds + (beat - curr.beat) * (60f / bpm);
            }

            if (next != null)
            {
                // Add full section
                float sectionBeats = next.beat - curr.beat;
                seconds += sectionBeats * (60f / bpm);
            }
            else
            {
                // Last section
                return seconds + (beat - curr.beat) * (60f / bpm);
            }
        }

        return seconds;
    }

    // ---------------------------------------------------------
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

    // ---------------------------------------------------------
    public void StartSpawning()
    {
        nextIndex = 0;
        spawning = true;
        if (musicSource != null) musicSource.Play();
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
        {
            note.lane = lane;
            NoteRegistry.RegisterNote(lane, note);
        }

        Debug.Log("Spawning note at lane " + lane + " time=" + musicSource.time);
    }
}
