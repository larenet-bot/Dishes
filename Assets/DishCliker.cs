using UnityEngine;

public class DishClicker : MonoBehaviour
{
    [Header("Cleaning Settings")]
    public int clicksRequired = 3;

    [Header("References")]
    public DishVisual dishVisual;
    public SudsOnClick sudsOnClick; // bubbles
    public Upgrades upgrades; // assign in inspector or find at runtime

    [Header("Reward Text")]
    [SerializeField] private GameObject rewardTextPrefab;      // same prefab you used for bubbles
    [SerializeField] private Vector3 rewardTextOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Sounds")]
    public AudioClip[] squeakClips;
    private AudioSource audioSource;
    private int lastSqueakIndex = -1;

    private int currentClicks = 0;
    private DishData currentDish;

    public void Init(DishData data)
    {
        currentDish = data;
        currentClicks = 0;
        dishVisual.SetDish(currentDish);
        dishVisual.SetStage(0);
    }

    public void OnDishClicked()
    {
        if (currentDish == null) return;

        int stagesPerClick = upgrades != null ? upgrades.GetCurrentStagesPerClick() : 1;
        int finalStageIndex = currentDish.stageSprites.Length - 1; // final stage index

        // If we're not yet at the final stage, advance up to stagesPerClick but DO NOT complete on the same click
        if (currentClicks < finalStageIndex)
        {
            int nextStage = Mathf.Min(currentClicks + stagesPerClick, finalStageIndex);
            currentClicks = nextStage;
            dishVisual?.SetStage(currentClicks);

            // Bubble burst visuals
            if (sudsOnClick != null)
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = Camera.main.nearClipPlane;
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                sudsOnClick.BurstBubbles(worldPos);
            }

            // If we just moved to the final stage, do not finish the dish this click.
            // The next click (while currentClicks == finalStageIndex) will complete the dish.
            return;
        }

        // If we're already on the final stage, complete the dish on click
        if (currentClicks >= finalStageIndex)
        {
            // complete
            currentClicks = 0;
            dishVisual?.SetStage(0);

            // Let ScoreManager handle profit and dishes, and get the reward amount back
            float reward = ScoreManager.Instance.OnDishCleaned(currentDish);

            // Get world position under cursor once
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.nearClipPlane;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

            PlayRandomSqueak();

            // Bubble burst visuals
            if (sudsOnClick != null)
            {
                sudsOnClick.BurstBubbles(worldPos);
            }
            // Floating reward text
            if (rewardTextPrefab != null && Camera.main != null && reward > 0f)
            {
                Vector3 textPos = worldPos;
                textPos.z = 0f; // keep on gameplay plane (adjust if needed)
                textPos += rewardTextOffset;

                GameObject go = Instantiate(rewardTextPrefab, textPos, Quaternion.identity);

                var floating = go.GetComponent<BubbleRewardText>();
                if (floating != null)
                {
                    string formatted = BigNumberFormatter.FormatMoney(reward);
                    floating.Initialize("+ " + formatted);
                }
            }
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void PlayRandomSqueak()
    {
        if (squeakClips == null || squeakClips.Length == 0) return;

        int index;
        do { index = Random.Range(0, squeakClips.Length); }
        while (index == lastSqueakIndex && squeakClips.Length > 3);

        lastSqueakIndex = index;
        audioSource.PlayOneShot(squeakClips[index]);
        Debug.Log($" Played: {lastSqueakIndex}");
    }
}
