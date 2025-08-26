using UnityEngine;

public class SudsOnClick : MonoBehaviour
{
    public GameObject bubblePrefab; // Assign your bubble sprite prefab in the Inspector
    public int bubbleCount = 20;    // Number of bubbles to burst out
    public float burstForce = 1f;   // Force applied to each bubble (reduced for less velocity)
    public float bubbleLifetime = 0.2f; // Minimum time before bubble despawns

    public AudioClip burstSound; // Assign your .wav file in the Inspector
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // Call this method to burst bubbles at a specific world position
    public void BurstBubbles(Vector3 worldPos)
    {
        // Play burst sound effect
        if (burstSound != null)
        {
            audioSource.PlayOneShot(burstSound);
        }

        for (int i = 0; i < bubbleCount; i++)
        {
            GameObject bubble = Instantiate(bubblePrefab, worldPos, Quaternion.identity);

            // Add random burst direction
            Rigidbody2D rb = bubble.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float angle = Random.Range(0f, 2f * Mathf.PI);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                rb.AddForce(direction * burstForce, ForceMode2D.Impulse);
            }

            // Destroy bubble after a random time between bubbleLifetime and bubbleLifetime + 0.2 seconds
            float randomLifetime = Random.Range(bubbleLifetime, bubbleLifetime + 0.2f);
            Destroy(bubble, randomLifetime);
        }
    }
}
