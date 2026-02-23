using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

public class BubbleClickDestroy : MonoBehaviour
{
    private BubblePopSound popSound;

    public enum BubbleAwardType
    {
        InstantCash,
        InstantWash,
        HappyEmployees,
        WellMotivated
    }

    [Header("Award Type")]
    [SerializeField] private BubbleAwardType awardType = BubbleAwardType.InstantCash;

    [Header("Randomize On Spawn")]
    [Tooltip("If enabled, this bubble picks an award at random when it spawns, using the weights below.")]
    [SerializeField] private bool randomizeAwardOnSpawn = true;

    [Serializable]
    private class AwardRoll
    {
        public BubbleAwardType type;
        public string title;
        [Min(0f)] public float weight = 1f;
    }

    [Tooltip("Optional weighted roll list. Set weight to 0 to exclude a type. If empty, all types are equally likely.")]
    [SerializeField] private AwardRoll[] awardRolls;

    [Header("Award Title")]
    [SerializeField] private string awardTitle = "Instant Cash";

    [Header("Award Text")]
    [SerializeField] private GameObject awardTextPrefab; // BubbleRewardText prefab
    [SerializeField] private Vector3 awardTextOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private float awardTextPlaneZ = 0f;

    [Header("Instant Cash")]
    [Tooltip("Cash reward = (ProfitRate $/sec) * secondsWorth + flatBonus")]
    [SerializeField] private float secondsWorth = 25f;
    [SerializeField] private float flatBonus = 0f;

    [Header("Instant Wash")]
    [Tooltip("How many wash ticks per second.")]
    [SerializeField] private float washTicksPerSecond = 5f;
    [Tooltip("How many seconds the wash runs per dish stage.")]
    [SerializeField] private float secondsPerStage = 4f;
    [Tooltip("Optional override for how many stages the wash should run for. 0 = use current dish stage count.")]
    [SerializeField] private int instantWashStageCountOverride = 0;

    [Header("Happy Employees")]
    [SerializeField] private float happyEmployeesMultiplier = 10f;
    [SerializeField] private float happyEmployeesSeconds = 20f;

    [Header("Well Motivated")]
    [SerializeField] private float wellMotivatedMultiplier = 20f;
    [SerializeField] private float wellMotivatedSeconds = 10f;

    private void Awake()
    {
        PickRandomAwardIfNeeded();
    }

    private void Start()
    {
        popSound = GetComponent<BubblePopSound>();
    }

    private void OnMouseDown()
    {
        // Block while game is paused
        if (Time.timeScale == 0f) return;

        // Block if clicking through the console/UI
        // if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (ScoreManager.Instance == null)
            return;

        switch (awardType)
        {
            case BubbleAwardType.InstantCash:
                AwardInstantCash();
                break;
            case BubbleAwardType.InstantWash:
                AwardInstantWash();
                break;
            case BubbleAwardType.HappyEmployees:
                AwardHappyEmployees();
                break;
            case BubbleAwardType.WellMotivated:
                AwardWellMotivated();
                break;
        }

        if (popSound != null && popSound.popSounds.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, popSound.popSounds.Length);
            AudioSource.PlayClipAtPoint(popSound.popSounds[randomIndex], transform.position);
        }

        Destroy(gameObject, 0.1f);
    }

    private void AwardInstantCash()
    {
        float avg = 0f;
        if (ProfitRate.Instance != null)
            avg = Mathf.Max(0f, ProfitRate.Instance.AverageProfit);

        float reward = flatBonus + (avg * Mathf.Max(0f, secondsWorth));
        if (reward > 0f)
            ScoreManager.Instance.AddBubbleReward(reward);

        string money = BigNumberFormatter.FormatMoney((double)reward);
        SpawnAwardText($"{awardTitle} + {money}");
    }

    private void AwardInstantWash()
    {
        SpawnAwardText(awardTitle);

        if (ScoreManager.Instance.activeDish == null)
            return;

        float rate = Mathf.Max(0.1f, washTicksPerSecond);
        float perStage = Mathf.Max(0.1f, secondsPerStage);
        ScoreManager.Instance.activeDish.StartInstantWash(rate, perStage, awardTitle, instantWashStageCountOverride);
    }

    private void AwardHappyEmployees()
    {
        float mult = Mathf.Max(1f, happyEmployeesMultiplier);
        float dur = Mathf.Max(0.1f, happyEmployeesSeconds);
        ScoreManager.Instance.ApplyHappyEmployeesBoost(mult, dur);
        SpawnAwardText(awardTitle);
    }

    private void AwardWellMotivated()
    {
        float mult = Mathf.Max(1f, wellMotivatedMultiplier);
        float dur = Mathf.Max(0.1f, wellMotivatedSeconds);
        ScoreManager.Instance.ApplyWellMotivatedBoost(mult, dur);
        SpawnAwardText(awardTitle);
    }

    private void SpawnAwardText(string message)
    {
        if (awardTextPrefab == null) return;
        if (string.IsNullOrWhiteSpace(message)) return;

        Vector3 worldPos = transform.position;
        worldPos.z = awardTextPlaneZ;
        worldPos += awardTextOffset;

        GameObject go = Instantiate(awardTextPrefab, worldPos, Quaternion.identity);
        var floating = go.GetComponent<BubbleRewardText>();
        if (floating != null)
            floating.Initialize(message);
    }

    private void PickRandomAwardIfNeeded()
    {
        if (!randomizeAwardOnSpawn)
            return;

        // No list => uniform roll across all award types.
        if (awardRolls == null || awardRolls.Length == 0)
        {
            int count = Enum.GetValues(typeof(BubbleAwardType)).Length;
            awardType = (BubbleAwardType)UnityEngine.Random.Range(0, count);
            awardTitle = ToPrettyTitle(awardType.ToString());
            return;
        }

        float totalWeight = 0f;
        for (int i = 0; i < awardRolls.Length; i++)
        {
            var entry = awardRolls[i];
            if (entry == null) continue;
            if (entry.weight <= 0f) continue;
            totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
        {
            int count = Enum.GetValues(typeof(BubbleAwardType)).Length;
            awardType = (BubbleAwardType)UnityEngine.Random.Range(0, count);
            awardTitle = ToPrettyTitle(awardType.ToString());
            return;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        for (int i = 0; i < awardRolls.Length; i++)
        {
            var entry = awardRolls[i];
            if (entry == null) continue;
            if (entry.weight <= 0f) continue;

            roll -= entry.weight;
            if (roll <= 0f)
            {
                awardType = entry.type;
                awardTitle = string.IsNullOrWhiteSpace(entry.title)
                    ? ToPrettyTitle(entry.type.ToString())
                    : entry.title;
                return;
            }
        }

        // Fallback
        var last = awardRolls[awardRolls.Length - 1];
        awardType = last != null ? last.type : BubbleAwardType.InstantCash;
        awardTitle = (last != null && !string.IsNullOrWhiteSpace(last.title))
            ? last.title
            : ToPrettyTitle(awardType.ToString());
    }

    private static string ToPrettyTitle(string camel)
    {
        if (string.IsNullOrWhiteSpace(camel))
            return string.Empty;

        StringBuilder sb = new StringBuilder(camel.Length + 8);
        sb.Append(camel[0]);

        for (int i = 1; i < camel.Length; i++)
        {
            char c = camel[i];
            char prev = camel[i - 1];

            if (char.IsUpper(c) && !char.IsUpper(prev))
                sb.Append(' ');

            sb.Append(c);
        }

        return sb.ToString();
    }
}
