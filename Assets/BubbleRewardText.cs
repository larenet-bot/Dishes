using TMPro;
using UnityEngine;

public class BubbleRewardText : MonoBehaviour
{
    [Header("References")]
    public TMP_Text rewardText;          // assign in prefab, or auto-grab in Awake

    [Header("Animation")]
    public float floatSpeed = 1.5f;      // units per second upward
    public float lifetime = 1.0f;        // seconds until destroy

    private float timer = 0f;
    private Color startColor;

    private void Awake()
    {
        if (rewardText == null)
            rewardText = GetComponentInChildren<TMP_Text>();

        if (rewardText != null)
        {
            startColor = rewardText.color;
            // If the prefab's text somehow has zero alpha, ensure a visible start alpha.
            if (startColor.a <= 0f)
                startColor.a = 1f;
        }
        else
        {
            // Defensive fallback so fade logic still produces visible color.
            startColor = Color.white;
            startColor.a = 1f;
            Debug.LogWarning("[BubbleRewardText] No TMP_Text found in children. Assign a TMP_Text in the prefab for proper behavior.", this);
        }
    }

    public void Initialize(string text)
    {
        if (rewardText != null)
        {
            rewardText.text = text;
            // Reset color to the known-good start color for each new instance.
            rewardText.color = startColor;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // Move upward
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // Fade out over lifetime
        if (rewardText != null)
        {
            float t = Mathf.Clamp01(timer / lifetime);
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            rewardText.color = c;
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
