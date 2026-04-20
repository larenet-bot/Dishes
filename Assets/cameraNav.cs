using UnityEngine;

public class CameraNavigator : MonoBehaviour
{
    public Transform sinkView;
    public Transform leftView;
    public Transform rightView;
    public Transform overviewView;

    public float moveSpeed = 5f;

    private Transform target;

    void Start()
    {
        target = sinkView; // default focus
    }

    void Update()
    {
        if (target != null)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                target.position,
                Time.deltaTime * moveSpeed
            );

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                target.rotation,
                Time.deltaTime * moveSpeed
            );
        }
    }

    public void GoSink() => target = sinkView;
    public void GoLeft() => target = leftView;
    public void GoRight() => target = rightView;
    public void GoOverview() => target = overviewView;
}