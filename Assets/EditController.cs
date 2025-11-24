using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class BeatmapEditor : MonoBehaviour
{
    public AudioSource music;
    public string saveFileName = "chart.txt";

    private List<string> notes = new List<string>();

    void Update()
    {
        // Press lane keys to add notes
        if (Input.GetKeyDown(KeyCode.A)) AddNote(0);
        if (Input.GetKeyDown(KeyCode.S)) AddNote(1);
        if (Input.GetKeyDown(KeyCode.J)) AddNote(2);
        if (Input.GetKeyDown(KeyCode.K)) AddNote(3);
    }

    void AddNote(int lane)
    {
        float t = music.time;
        notes.Add($"{t:F3} {lane}");
        Debug.Log($"Added note lane {lane} at {t:F3}");
    }

    public void SaveChart()
    {
        File.WriteAllLines(Application.dataPath + "/" + saveFileName, notes);
        Debug.Log("Chart saved!");
    }
}
