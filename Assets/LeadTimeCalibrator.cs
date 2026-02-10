//csharp Assets\Rhythm Game Tutorial\LeadTimeCalibrator.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeadTimeCalibrator : MonoBehaviour
{
    [Tooltip("Optional. If set, uses this spawner's notePrefab and spawn points.")]
    public NoteSpawner noteSpawner;

    [Tooltip("Fallback note prefab if no NoteSpawner assigned.")]
    public GameObject notePrefab;

    [Tooltip("Fallback spawn point if no NoteSpawner assigned.")]
    public Transform spawnPoint;

    [Tooltip("Lane index to spawn into (if using NoteSpawner lanes).")]
    public int lane = 0;

    [Tooltip("Number of test notes to spawn for averaging.")]
    public int iterations = 5;

    [Tooltip("Delay between test spawns (seconds).")]
    public float delayBetween = 0.5f;

    [Tooltip("Maximum wait (seconds) for a spawned test note to reach the hitline.")]
    public float perTestTimeout = 5f;

    // Run from inspector or code. Returns IEnumerator for convenience.
    [ContextMenu("Run Calibration")]
    public void RunCalibrationContextMenu()
    {
        StartCoroutine(RunCalibrationCoroutine());
    }

    public void StartCalibration()
    {
        StartCoroutine(RunCalibrationCoroutine());
    }

    private IEnumerator RunCalibrationCoroutine()
    {
        // resolve prefab and spawn transform
        GameObject prefab = notePrefab;
        Transform spawnT = spawnPoint;

        if (noteSpawner != null)
        {
            if (prefab == null) prefab = noteSpawner.notePrefab;
            if (spawnT == null && noteSpawner.laneSpawnPoints != null && noteSpawner.laneSpawnPoints.Length > 0)
                spawnT = noteSpawner.laneSpawnPoints[Mathf.Clamp(lane, 0, noteSpawner.laneSpawnPoints.Length - 1)];
        }

        if (prefab == null || spawnT == null)
        {
            Debug.LogWarning("[LeadTimeCalibrator] Missing prefab or spawn point. Assign noteSpawner or notePrefab + spawnPoint.");
            yield break;
        }

        var travelTimes = new List<double>();
        for (int i = 0; i < Mathf.Max(1, iterations); i++)
        {
            double spawnDsp = AudioSettings.dspTime;

            // instantiate test note
            GameObject inst = Instantiate(prefab, spawnT.position, Quaternion.identity);
            inst.transform.SetParent(this.transform, true);

            // add CalibrationMarker to detect hitline timestamps
            var marker = inst.AddComponent<CalibrationMarker>();
            marker.SpawnDsp = spawnDsp;

            bool gotHit = false;
            double travel = 0.0;
            marker.OnHit = (t) =>
            {
                gotHit = true;
                travel = t;
            };

            // ensure scroller starts (if prefab uses BeatScroller)
            var sc = inst.GetComponent<BeatScroller>();
            if (sc != null)
            {
                sc.hasStarted = true;
            }

            // wait until hit or timeout
            float startWait = Time.realtimeSinceStartup;
            while (!gotHit && Time.realtimeSinceStartup - startWait < perTestTimeout)
            {
                yield return null;
            }

            if (gotHit)
            {
                travelTimes.Add(travel);
                Debug.Log($"[LeadTimeCalibrator] Test {i + 1}: travelTime={travel:0.000}s");
            }
            else
            {
                Debug.LogWarning($"[LeadTimeCalibrator] Test {i + 1}: timed out (no hit detected within {perTestTimeout}s).");
            }

            // small delay to avoid overlap
            yield return new WaitForSeconds(delayBetween);
        }

        if (travelTimes.Count == 0)
        {
            Debug.LogWarning("[LeadTimeCalibrator] No successful tests. Check hitline tag and colliders.");
            yield break;
        }

        double avg = 0;
        foreach (var t in travelTimes) avg += t;
        avg /= travelTimes.Count;

        // statistic: standard deviation
        double sumSq = 0;
        foreach (var t in travelTimes) sumSq += (t - avg) * (t - avg);
        double std = Math.Sqrt(sumSq / travelTimes.Count);

        Debug.Log($"[LeadTimeCalibrator] Completed {travelTimes.Count}/{iterations} tests. Average travel time = {avg:0.000}s (std={std:0.000}s).");
        Debug.Log($"[LeadTimeCalibrator] Recommended spawnLeadTime = {avg:0.000}s");

        // If using NoteSpawner, optionally set it
        if (noteSpawner != null)
        {
            noteSpawner.spawnLeadTime = (float)avg;
            Debug.Log("[LeadTimeCalibrator] Applied recommended spawnLeadTime to NoteSpawner.");
        }
    }

    // Small helper attached to spawned test note to detect hitline time precisely
    private class CalibrationMarker : MonoBehaviour
    {
        public double SpawnDsp;
        public Action<double> OnHit;

        private bool reported = false;

        void Start()
        {
            // Ensure SpawnDsp is set; fallback to current DSP
            if (SpawnDsp == 0) SpawnDsp = AudioSettings.dspTime;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (reported) return;
            if (collision.CompareTag("HitBox"))
            {
                double hitDsp = AudioSettings.dspTime;
                double travel = hitDsp - SpawnDsp;
                reported = true;
                try { OnHit?.Invoke(travel); } catch { }
                // do not destroy here; let Note/other logic handle it
                Destroy(this); // remove marker component only
            }
        }
    }
}