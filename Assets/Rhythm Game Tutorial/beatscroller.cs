using UnityEngine;

public class beatscroller : MonoBehaviour
{

    public float beatTempo; // beats per minute
    public bool hasStarted;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        beatTempo = beatTempo / 60f; // convert to beats per second
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasStarted)
        {
            if (Input.anyKeyDown)
                hasStarted = true;
        }
        else
        {
            transform.position -= new Vector3(0f, beatTempo * Time.deltaTime, 0f);
        }
    }
}
