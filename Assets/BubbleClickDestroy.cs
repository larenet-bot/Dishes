using UnityEngine;
using UnityEngine.EventSystems;

public class BubbleClickDestroy : MonoBehaviour
{
    private BubblePopSound popSound;
    float averageProfit = 0f;
    float reward = 0f;
    [SerializeField] private float baseReward = 20f;
    [SerializeField] private float multiplier = 10f;

    void Start()
    {
        popSound = GetComponent<BubblePopSound>();
    }

    void OnMouseDown()
    {
        //Block while game is paused
        if (Time.timeScale == 0f) return;

        //Block if clicking through the console/UI
        //if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;


        averageProfit = ProfitRate.Instance.AverageProfit;
        reward = baseReward + (averageProfit * multiplier);
        ScoreManager.Instance.AddBubbleReward(reward);

        if (popSound != null && popSound.popSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, popSound.popSounds.Length);
            AudioSource.PlayClipAtPoint(popSound.popSounds[randomIndex], transform.position);
        }

        Destroy(gameObject, 0.1f);
    }
}