using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Attach to the hit line GameObject (with a trigger collider)
public class HitZone : MonoBehaviour
{
    public KeyCode[] laneKeys = new KeyCode[] { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F };
    public float hitWindow = 0.3f; // seconds tolerance around perfect
    public TextMeshProUGUI scoreText;
    public RhythmGameManager manager; // assign

    // notes currently overlapping the zone
    private List<Note> overlapping = new List<Note>();
    private int score = 0;

    void Update()
    {
        for (int i = 0; i < laneKeys.Length; i++)
        {
            if (Input.GetKeyDown(laneKeys[i]))
            {
                TryHitLane(i);
            }
        }
    }

    private void TryHitLane(int laneIndex)
    {
        // find best candidate in lane
        Note best = null;
        float bestDelta = float.MaxValue;

        float songTime = manager.GetSongTime();

        foreach (Note n in overlapping)
        {
            if (n == null || n.lane != laneIndex || n.hit) continue;
            float delta = Mathf.Abs(n.targetTime - songTime);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                best = n;
            }
        }

        if (best != null && bestDelta <= hitWindow)
        {
            OnHit(best, bestDelta);
        }
        else
        {
            OnMiss();
        }
    }

    private void OnHit(Note n, float delta)
    {
        n.hit = true;
        score += 100; // basic scoring
        UpdateScoreText();
        manager.NoteHit(n, delta);
        Destroy(n.gameObject);
    }

    private void OnMiss()
    {
        manager.NoteMissed();
        // optional - show feedback
    }

    private void UpdateScoreText()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        Note n = col.GetComponent<Note>();
        if (n != null) overlapping.Add(n);
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        Note n = col.GetComponent<Note>();
        if (n != null) overlapping.Remove(n);
    }
}
