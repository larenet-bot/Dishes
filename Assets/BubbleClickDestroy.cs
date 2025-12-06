using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BubbleClickDestroy : MonoBehaviour
{
    private BubblePopSound popSound;
    float averageProfit = 0f;
    float reward = 0f;
    [SerializeField] private float baseReward = 20f;
    [SerializeField] private float multiplier = 10f;

    [Header("Reward Text")]
    [SerializeField] private GameObject rewardTextPrefab;   // assign the prefab here
    [SerializeField] private Vector3 rewardTextOffset = new Vector3(0f, 0.5f, 0f);


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

        if (rewardTextPrefab != null && Camera.main != null)
        {
            Vector3 screenPos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            worldPos.z = 0f; // keep on game plane
            worldPos += rewardTextOffset;

            GameObject go = Instantiate(rewardTextPrefab, worldPos, Quaternion.identity);

            var floating = go.GetComponent<BubbleRewardText>();
            if (floating != null)
            {
                // Use your global formatter, so big rewards scale nicely
                string formatted = BigNumberFormatter.FormatMoney(reward);
                floating.Initialize("+ " + formatted);
            }
        }

        if (popSound != null && popSound.popSounds.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, popSound.popSounds.Length);
            AudioSource.PlayClipAtPoint(popSound.popSounds[randomIndex], transform.position);
        }

        Destroy(gameObject, 0.1f);
    }
}