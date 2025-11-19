using System;
using System.Collections.Generic;

[Serializable]
public class NoteData
{
    public float time;
    public int lane;
}

[Serializable]
public class DifficultyChart
{
    public string difficultyName;   // "easy", "normal", "hard"
    public int level;               // 1–10 or anything you want
    public List<NoteData> notes;
}

[Serializable]
public class SongChart
{
    public string songName;
    public string artist;
    public float offset;
    public List<DifficultyChart> difficulties;
}
