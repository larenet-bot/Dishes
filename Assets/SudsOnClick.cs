using UnityEngine;

public class SudsOnClick : MonoBehaviour
{
    public GameObject bubblePrefab; // Assign your bubble sprite prefab in the Inspector
    public int bubbleCount = 20;    // Number of bubbles to burst out
    public float burstForce = 10f;   // Force applied to each bubble (reduced for less velocity)
    public float bubbleLifetime = 0.2f; // Time before bubble despawns

    // Call this method to burst bubbles at a specific world position
    public void BurstBubbles(Vector3 worldPos)
    {
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

            // Destroy bubble after bubbleLifetime seconds
            Destroy(bubble, bubbleLifetime);
        }
    }
}
