using UnityEngine;

public class BeatScroller : MonoBehaviour
{
    public float scrollSpeed = 5f;

    public float targetTime;      // time when note should be hit
    public double songStartDSP;   // DSP start time

    private float spawnY;
    private float hitY;

    public Transform hitLine;

    void Start()
    {
        spawnY = transform.position.y;

        if (hitLine != null)
            hitY = hitLine.position.y;
    }

    void Update()
    {
        if (songStartDSP < 0) return;

        double songTime = AudioSettings.dspTime - songStartDSP;

        float timeUntilHit = targetTime - (float)songTime;

        float y = hitY + timeUntilHit * scrollSpeed;

        Vector3 pos = transform.position;
        pos.y = y;
        transform.position = pos;
    }

    public void Initialize(float noteTargetTime, double dspStart, Transform hitLineTransform)
    {
        targetTime = noteTargetTime;
        songStartDSP = dspStart;
        hitLine = hitLineTransform;

        spawnY = transform.position.y;

        if (hitLine != null)
            hitY = hitLine.position.y;
    }
}