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
            startColor = rewardText.color;
    }

    public void Initialize(string text)
    {
        if (rewardText != null)
            rewardText.text = text;
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
