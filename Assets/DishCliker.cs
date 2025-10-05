using UnityEngine;

public class DishClicker : MonoBehaviour
{
    [Header("Cleaning Settings")]
    public int clicksRequired = 3;

    [Header("References")]
    public DishVisual dishVisual;
    public SudsOnClick sudsOnClick; // bubbles

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
        currentClicks++;
        dishVisual?.SetStage(currentClicks);

        if (currentClicks >= clicksRequired)
        {
            currentClicks = 0;
            dishVisual?.SetStage(0);

            //  Tell ScoreManager this dish is complete
            ScoreManager.Instance.OnDishCleaned(currentDish);

            PlayRandomSqueak();
        }

        // Bubble burst visuals
        if (sudsOnClick != null)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.nearClipPlane;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            sudsOnClick.BurstBubbles(worldPos);
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
        while (index == lastSqueakIndex && squeakClips.Length > 1);

        lastSqueakIndex = index;
        audioSource.PlayOneShot(squeakClips[index]);
    }
}
