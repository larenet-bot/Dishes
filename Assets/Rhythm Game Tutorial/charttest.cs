using UnityEngine;

public class ChartTest : MonoBehaviour
{
    public TextAsset chartFile;

    void Start()
    {
        if (chartFile == null)
        {
            Debug.LogError("No chart file assigned!");
            return;
        }

        string[] lines = chartFile.text.Split('\n');

        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.StartsWith("#") || string.IsNullOrEmpty(line)) continue;

            string[] parts = line.Split(' ');
            float time = float.Parse(parts[0]);
            int lane = int.Parse(parts[1]);

            Debug.Log($"Parsed note: time={time}, lane={lane}");
        }
    }
}
