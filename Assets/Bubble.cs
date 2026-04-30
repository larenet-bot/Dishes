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

    [Header("Spawn Bounds (camera-based)")]
    [SerializeField] float horizontalPadding = 0.3f; // world units inside edges
    [SerializeField] float spawnBelowScreen = 0.5f;   // world units below bottom

    [SerializeField] float minX;
    [SerializeField] float maxX;
    [SerializeField] float spawnY;
    [SerializeField] float spawnZ;

    [SerializeField] public float bubbleSpeed = 0f; // Upward movement speed
    [SerializeField] public float bubbleXAmplitude = 0f; // Distance bubble travels in x
    [SerializeField] public float bubbleXFrequency = 0f; // How often bubble completes its swing

    Camera cam;

    // Keep a reference to the spawn coroutine so we can restart/stop it safely
    private Coroutine spawnCoroutine;

    void Start()
    {
        cam = Camera.main;
        StartSpawning();
    }

    void OnEnable()
    {
        // Ensure spawning resumes when component/gameobject is enabled
        if (spawnCoroutine == null)
            StartSpawning();
    }

    void OnDisable()
    {
        // Stop spawn coroutine when disabled to avoid orphaned coroutines
        StopSpawning();
    }

    IEnumerator SpawnBubble()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawn, maxSpawn));
            if (cam == null) cam = Camera.main;
            if (cam == null) { yield return null; continue; }

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float minX = cam.transform.position.x - halfW + horizontalPadding;
            float maxX = cam.transform.position.x + halfW - horizontalPadding;
            float bottomY = cam.transform.position.y - halfH;

            float x = Random.Range(minX, maxX);
            float y = bottomY - spawnBelowScreen;
            float z = spawnZ;

            Vector3 spawnPosition = new Vector3(x, y, z);

            // Spawn a random bubble prefab at the position
            GameObject spawnedBubble = Instantiate(
                bubblePrefab[Random.Range(0, bubblePrefab.Length)],
                spawnPosition,
                Quaternion.identity
            );

            // Add upward movement behavior
            BubbleMover mover = spawnedBubble.AddComponent<BubbleMover>();
            mover.SetSpeed(bubbleSpeed);

            mover.SetXAmplitude(bubbleXAmplitude);

            mover.SetXFrequency(bubbleXFrequency);

            // Destroy automatically after 5 seconds for cleanup
            Destroy(spawnedBubble, 5f);
        }
    }

    // Public API to start spawning bubbles (idempotent)
    public void StartSpawning()
    {
        if (spawnCoroutine == null && this.isActiveAndEnabled)
            spawnCoroutine = StartCoroutine(SpawnBubble());
    }

    // Public API to stop spawning bubbles (idempotent)
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }
}
public class BubbleMover : MonoBehaviour
{
    private float speed = 0f;
    private float xAmplitude = 0f;
    private float xFrequency = 0f;
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