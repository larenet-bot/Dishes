using UnityEngine;

public class DishClicker : MonoBehaviour
{
    [Header("Cleaning Settings")]
    public int clicksRequired = 3;

    [Header("References")]
    public DishVisual dishVisual;
    public SudsOnClick sudsOnClick; // Assign in Inspector or via code

    [Header("Auto Clicker")]
    public bool autoClickEnabled = false;
    public float autoClickInterval = 1f;

    [Header("Squeak Sounds")]
    public AudioClip[] squeakClips; // Assign 3 .wav files from Sounds folder in Inspector
    private AudioSource audioSource;
    private int lastSqueakIndex = -1; // Track last played sound

    private int currentClicks = 0;
    private float autoClickTimer = 0f;

    private void Start()
    {
        dishVisual?.SetStage(0);
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (autoClickEnabled)
        {
            autoClickTimer += Time.deltaTime;
            if (autoClickTimer >= autoClickInterval)
            {
                autoClickTimer = 0f;
                ProcessClick();
            }
        }
    }

    public void OnDishClicked()
    {
        ProcessClick();

        // Burst bubbles at mouse position
        if (sudsOnClick != null)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.nearClipPlane;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            sudsOnClick.BurstBubbles(worldPos);
        }
    }

    private void PlayRandomSqueak()
    {
        if (squeakClips != null && squeakClips.Length > 1)
        {
            int index;
            do
            {
                index = Random.Range(0, squeakClips.Length);
            } while (index == lastSqueakIndex);

            lastSqueakIndex = index;
            audioSource.PlayOneShot(squeakClips[index]);
        }
        else if (squeakClips != null && squeakClips.Length == 1)
        {
            audioSource.PlayOneShot(squeakClips[0]);
            lastSqueakIndex = 0;
        }
    }

    private void ProcessClick()
    {
        currentClicks++;
        dishVisual?.SetStage(currentClicks);

        if (currentClicks >= clicksRequired)
        {
            currentClicks = 0;
            dishVisual?.SetStage(0);

            ScoreManager.Instance.AddScore(); // Use upgraded profitPerDish from ScoreManager

            // Play random squeak sound only when clicksRequired is reached
            PlayRandomSqueak();
        }
    }

    public void EnableAutoClick() => autoClickEnabled = true;
}
