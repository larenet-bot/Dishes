using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Attach to the hit line GameObject (with a trigger collider)
public class HitZone : MonoBehaviour
{
    public KeyCode[] keys; // e.g. Left, Down, Up, Right
    public Transform[] hitZones; // 1 per lane

    void Update()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKeyDown(keys[i]))
            {
                CheckHit(i);
            }
        }
    }

    void CheckHit(int laneIndex)
    {
        Collider2D note = Physics2D.OverlapBox(
            hitZones[laneIndex].position,
            new Vector2(0.8f, 0.4f),  // width, height of detection
            0f,
            LayerMask.GetMask("Notes")
        );

        if (note != null)
        {
            Destroy(note.gameObject);
            Debug.Log($"Hit note in lane {laneIndex}!");
        }
    }
}
