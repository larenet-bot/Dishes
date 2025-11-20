using System.Collections.Generic;
using UnityEngine;

public static class NoteRegistry
{
    // 1 list per lane
    private static Dictionary<int, Queue<Note>> lanes = new Dictionary<int, Queue<Note>>();

    public static void RegisterNote(int lane, Note note)
    {
        if (!lanes.ContainsKey(lane))
            lanes[lane] = new Queue<Note>();

        lanes[lane].Enqueue(note);
    }

    public static Note GetNextNote(int lane)
    {
        if (!lanes.ContainsKey(lane)) return null;
        if (lanes[lane].Count == 0) return null;

        return lanes[lane].Peek();
    }

    public static void PopNote(int lane)
    {
        if (!lanes.ContainsKey(lane)) return;
        if (lanes[lane].Count == 0) return;

        lanes[lane].Dequeue();
    }

    public static void ClearAll()
    {
        lanes.Clear();
    }
}
