using UnityEngine;

[System.Serializable]
public class BeatmapOption
{
    public string label;
    public TextAsset chartFile;
    public AudioClip audioClip;
    public Sprite icon; // optional for button visuals
}