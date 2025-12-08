using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public GameObject notePrefab;
    public Transform[] laneSpawnPoints;

    public AudioSource musicSource;    // assign your rhythm track AudioSource
    public HitWindow hitWindow;        // assign in inspector

    public TextAsset chartFile;
    public float spawnYOffset = 0f;

    [Tooltip("Seconds that a note will be spawned before its target hit time.")]
    public float spawnLeadTime = 2f;

    [Tooltip("Seconds from now to schedule the audio start (DSP). Small positive value recommended: 0.05-0.2")]
    public float scheduleStartDelay = 0.1f;

    [Header("Timing tuning")]
    [Tooltip("Tweak this if your visuals/audio are consistently offset. Positive moves judgement later (use ~0.4s if logs show -0.4s).")]
    public float timingOffset = 0f;

    // public global DSP start time (used by LaneKey for judgement)
    public static double globalSongStartDspTime = -1.0;

    // global timing offset applied to judgement (seconds)
    public static float globalTimingOffset = 0f;

    private float songStartTime;

    // Do NOT spawn notes automatically — wait for SPACE press
    void Start()
    {
        // intentionally empty
    }

    // Called by Rythmstart.cs when SPACE is pressed
    public void StartSpawning()
    {
        // basic validation to avoid silent failures
        if (notePrefab == null)
        {
            Debug.LogWarning("[NoteSpawner] notePrefab not assigned. No notes will be spawned.");
            return;
        }
        if (laneSpawnPoints == null || laneSpawnPoints.Length == 0)
        {
            Debug.LogWarning("[NoteSpawner] laneSpawnPoints not configured or empty.");
            return;
        }
        if (chartFile == null || string.IsNullOrWhiteSpace(chartFile.text))
        {
            Debug.LogWarning("[NoteSpawner] chartFile missing or empty.");
            return;
        }

        // schedule audio using DSP for accurate timing
        double dspStart = AudioSettings.dspTime + Math.Max(0.0, scheduleStartDelay);
        if (musicSource != null)
        {
            // Stop any previous playback then schedule
            musicSource.Stop();
            musicSource.PlayScheduled(dspStart);
        }

        // expose global DSP start time for judgement code
        globalSongStartDspTime = dspStart;

        // set global timing offset from inspector value
        globalTimingOffset = timingOffset;

        // still store Time.time-based start for any fallback diagnostics
        songStartTime = Time.time;

        StartCoroutine(SpawnNotesFromChartCoroutine());

        Debug.Log($"[NoteSpawner] Audio scheduled at DSP {dspStart:0.000}. spawnLeadTime={spawnLeadTime:0.00}s timingOffset={timingOffset:0.000}s");
    }

    private class NoteDef
    {
        public float timeMs;
        public float targetTime; // seconds
        public int lane;
    }

    private IEnumerator SpawnNotesFromChartCoroutine()
    {
        var noteDefs = new List<NoteDef>();

        // split lines robustly (handle CRLF)
        string[] lines = chartFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

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
                Debug.LogWarning("[NoteSpawner] Invalid line (expected 5 values): " + line);
                continue;
            }

            // Parse time (milliseconds in chart)
            if (!float.TryParse(parts[0], out float timeMs))
            {
                Debug.LogWarning("[NoteSpawner] Invalid TIME value: " + line);
                continue;
            }

            // Parse lane and auto-wrap
            if (!int.TryParse(parts[1], out int lane))
            {
                Debug.LogWarning("[NoteSpawner] Invalid LANE value: " + line);
                continue;
            }

            // AUTO-WRAP lane to valid range
            if (laneSpawnPoints != null && laneSpawnPoints.Length > 0)
            {
                lane = lane % laneSpawnPoints.Length;
                if (lane < 0) lane += laneSpawnPoints.Length;
            }
            else
            {
                Debug.LogWarning("[NoteSpawner] laneSpawnPoints not configured, skipping note.");
                continue;
            }

            // store note def
            noteDefs.Add(new NoteDef
            {
                timeMs = timeMs,
                targetTime = timeMs / 1000f,
                lane = lane
            });
        }

        if (noteDefs.Count == 0)
        {
            Debug.LogWarning("[NoteSpawner] No valid notes were parsed from chart.");
            yield break;
        }

        // sort by targetTime ascending
        noteDefs.Sort((a, b) => a.targetTime.CompareTo(b.targetTime));
        Debug.Log($"[NoteSpawner] Scheduled {noteDefs.Count} notes (lead {spawnLeadTime:0.00}s)");

        // iterate and spawn at appropriate DSP times relative to globalSongStartDspTime
        foreach (var nd in noteDefs)
        {
            // desired moment (DSP) to instantiate this note so it has spawnLeadTime to travel to hit zone
            double desiredSpawnDsp = globalSongStartDspTime + nd.targetTime - spawnLeadTime;

            // if desired spawn time is in future, wait using DSP-based loop for accuracy
            while (AudioSettings.dspTime < desiredSpawnDsp)
            {
                yield return null;
            }

            // spawn on next frame
            SpawnSingleNote(nd.timeMs, nd.lane);
        }
    }

    private void SpawnSingleNote(float timeMs, int lane)
    {
        if (notePrefab == null)
        {
            Debug.LogWarning("[NoteSpawner] notePrefab null when trying to spawn a note.");
            return;
        }

        if (laneSpawnPoints == null || laneSpawnPoints.Length == 0)
        {
            Debug.LogWarning("[NoteSpawner] laneSpawnPoints not configured.");
            return;
        }

        Transform spawnPoint = laneSpawnPoints[Mathf.Clamp(lane, 0, laneSpawnPoints.Length - 1)];
        Vector3 spawnPosition = spawnPoint.position + new Vector3(0, spawnYOffset, 0);

        // normalize z for common 2D camera setups so sprite is in front of camera
        if (Camera.main != null)
            spawnPosition.z = 0f;

        GameObject noteObj = Instantiate(notePrefab, spawnPosition, Quaternion.identity);

        // parent for scene tidiness (keeps world position)
        noteObj.transform.SetParent(this.transform, true);

        // Debug: log world position and viewport coordinates (helps identify off-screen spawns)
        if (Camera.main != null)
        {
            Vector3 vp = Camera.main.WorldToViewportPoint(spawnPosition);
            Debug.Log($"[NoteSpawner] Spawned note '{notePrefab.name}' at world {spawnPosition} viewport {vp} (lane {lane})");
        }
        else
        {
            Debug.Log($"[NoteSpawner] Spawned note '{notePrefab.name}' at world {spawnPosition} (no Camera.main)");
        }

        // Common visibility fixes for debugging:
        // - ensure scale isn't zero
        if (noteObj.transform.localScale == Vector3.zero)
            noteObj.transform.localScale = Vector3.one;

        // - find any SpriteRenderer (child or root) and force alpha = 1 and high sorting order for visibility
        var sr = noteObj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true;
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, 1f);
            try
            {
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 1000;
            }
            catch { /* ignore if sorting layer not found */ }
        }
        else
        {
            // If prefab is a UI element it will have a RectTransform instead of a SpriteRenderer
            if (noteObj.GetComponent<RectTransform>() != null)
            {
                Debug.LogWarning("[NoteSpawner] Spawned prefab uses a RectTransform (UI). If you intended a world-space sprite, convert the prefab or use a World Space Canvas.");
            }
            else
            {
                Debug.LogWarning("[NoteSpawner] No SpriteRenderer found on spawned note prefab. The prefab may not be visible.");
            }
        }

        Note n = noteObj.GetComponent<Note>();
        if (n != null)
        {
            n.lane = lane;
            n.targetTime = timeMs / 1000f; // convert ms to seconds relative to song start
            n.musicSource = musicSource;
            n.hitWindow = hitWindow;
        }

        // ensure movement starts
        var scroller = noteObj.GetComponent<BeatScroller>();
        if (scroller != null)
        {
            scroller.hasStarted = true;
        }
    }
}
