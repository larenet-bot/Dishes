using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public GameObject notePrefab;
    public Transform[] laneSpawnPoints;

    public AudioSource musicSource;    // assign your rhythm track AudioSource
    public HitWindow hitWindow;        // assign in inspector

    public TextAsset chartFile;
    public float spawnYOffset = 0f;

    private float songStartTime;

    // Do NOT spawn notes automatically — wait for SPACE press
    void Start()
    {
        // intentionally empty
    }

    // Called by Rythmstart.cs when SPACE is pressed
    public void StartSpawning()
    {
        songStartTime = Time.time;
        SpawnNotesFromChart();

        if (musicSource != null)
            musicSource.Play();
    }

    void SpawnNotesFromChart()
    {
        string[] lines = chartFile.text.Split('\n');

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            // Skip header or empty lines
            if (string.IsNullOrEmpty(line) || line.StartsWith("["))
                continue;

            string[] parts = line.Split(':');

            // Must have exactly 5 values
            if (parts.Length != 5)
            {
                Debug.LogWarning("Invalid line (expected 5 values): " + line);
                continue;
            }

            // Parse time
            if (!float.TryParse(parts[0], out float timeMs))
            {
                Debug.LogWarning("Invalid TIME value: " + line);
                continue;
            }

            // Parse lane and auto-wrap
            if (!int.TryParse(parts[1], out int lane))
            {
                Debug.LogWarning("Invalid LANE value: " + line);
                continue;
            }

            // AUTO-WRAP lane to valid range
            lane = lane % laneSpawnPoints.Length;
            if (lane < 0) lane += laneSpawnPoints.Length;

            // Parse the remaining fields (can be ignored if not used)
            if (!int.TryParse(parts[2], out int sustain)) sustain = 0;
            if (!int.TryParse(parts[3], out int type)) type = 0;
            if (!int.TryParse(parts[4], out int extra)) extra = 0;

            // safe spawn
            SpawnSingleNote(timeMs, lane);
        }
    }

    private void SpawnSingleNote(float timeMs, int lane)
    {
        Vector3 spawnPosition = laneSpawnPoints[lane].position + new Vector3(0, spawnYOffset, 0);
        GameObject note = Instantiate(notePrefab, spawnPosition, Quaternion.identity);

        Note n = note.GetComponent<Note>();
        if (n != null)
        {
            n.lane = lane;
            n.targetTime = timeMs / 1000f; // convert ms to seconds
            n.musicSource = musicSource;
            n.hitWindow = hitWindow;
        }
    }
}
