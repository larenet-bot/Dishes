using System.Collections;
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

    [SerializeField] float bubbleSpeed = 0.001f; // Upward movement speed

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
    private float speed = 0.001f;

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    void Update()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }
}

public class BubbleClickDestroy : MonoBehaviour
{
    void OnMouseDown()
    {
        Destroy(gameObject);
    }
}
