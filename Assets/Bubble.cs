using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    [SerializeField] GameObject[] bubblePrefab;

    // Controls how fast bubbles spawn:
    // Set lower minSpawn and maxSpawn for faster spawns,
    // higher values for slower spawns.
    [SerializeField] float minSpawn = 10.3f; // Minimum seconds between spawns
    [SerializeField] float maxSpawn = 60.0f; // Maximum seconds between spawns

    [SerializeField] float minX;
    [SerializeField] float maxX;
    [SerializeField] float spawnY;

    [SerializeField] public float bubbleSpeed = 0f; // Upward movement speed
    [SerializeField] public float bubbleXAmplitude = 0f; // Distance bubble travels in x
    [SerializeField] public float bubbleXFrequency = 0f; // How often bubble completes its swing

    void Start()
    {
        StartCoroutine(SpawnBubble());
    }

    IEnumerator SpawnBubble()
    {
        while (true)
        {
            // Choose a random X position within the defined range
            float randomX = Random.Range(minX, maxX);
            Vector3 spawnPosition = new Vector3(randomX, spawnY, 0);

            // Spawn a random bubble prefab at the position
            GameObject spawnedBubble = Instantiate(
                bubblePrefab[Random.Range(0, bubblePrefab.Length)],
                spawnPosition,
                Quaternion.identity
            );

            // Add upward movement behavior
            BubbleMover mover = spawnedBubble.AddComponent<BubbleMover>();
            mover.SetSpeed(bubbleSpeed);

            // Define x Amplitude
            BubbleMover xAmp = spawnedBubble.AddComponent<BubbleMover>();
            xAmp.SetXAmplitude(bubbleXAmplitude);

            // Define x Frequency
            BubbleMover xFreq = spawnedBubble.AddComponent<BubbleMover>();
            xFreq.SetXFrequency(bubbleXFrequency);

            // Add click-to-pop behavior
            spawnedBubble.AddComponent<BubbleClickDestroy>();

            // Destroy automatically after 5 seconds for cleanup
            Destroy(spawnedBubble, 5f);

            // Wait for a random time between minSpawn and maxSpawn before spawning next bubble
            // ------------------------------------------------------
            // HOW TO CHANGE SPAWN SPEED:
            // - Lower minSpawn and maxSpawn => faster spawn rate
            //   e.g., minSpawn = 0.1, maxSpawn = 0.5
            // - Higher minSpawn and maxSpawn => slower spawn rate
            //   e.g., minSpawn = 1.0, maxSpawn = 2.0
            // Change these in the Inspector or directly above.
            // ------------------------------------------------------
            yield return new WaitForSeconds(Random.Range(minSpawn, maxSpawn));
        }
    }
}

public class BubbleMover : MonoBehaviour
{
    private float speed = 0f;
    private float xAmplitude = 0f;
    private float xFrequency = 2f;

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
    public void SetXAmplitude(float newXAmplitude)
    {
        xAmplitude = newXAmplitude;
    }
    public void SetXFrequency(float newXFrequency)
    {
        xFrequency = newXFrequency;
    }


    void Update()
    {

        float xMovement = Mathf.Sin(Time.time * xFrequency) * xAmplitude;
        float yMovement = speed;
        Vector3 bubbleDirection = new Vector3(xMovement, yMovement, 0f) * Time.deltaTime;
        transform.Translate(bubbleDirection);

    }

}

public class BubbleClickDestroy : MonoBehaviour
{
    void OnMouseDown()
    {
        Object.FindFirstObjectByType<AudioManager>().Play("BubblePop1");
        Destroy(gameObject);
    }
}
