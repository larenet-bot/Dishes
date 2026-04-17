
using UnityEngine;

/// <summary>
/// Centralizes audio playback and bubble burst spawning so DishClicker is not responsible
/// for low-level audio/effect management.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioEffects : MonoBehaviour
{
    [Tooltip("Optional bubble burst effect played during washing.")]
    public SudsOnClick sudsOnClick;

    [Tooltip("Random squeak clips played when a dish is completed.")]
    public AudioClip[] squeakClips;

    private AudioSource audioSource;
    private int lastSqueakIndex = -1;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    /// <summary>
    /// Plays the configured bubble burst at the current pointer position.
    /// </summary>
    public void BurstBubblesAtMouse()
    {
        if (sudsOnClick == null || Camera.main == null)
        {
            return;
        }

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Camera.main.nearClipPlane;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        sudsOnClick.BurstBubbles(worldPosition);
    }

    /// <summary>
    /// Plays a completion squeak clip, avoiding immediate repetition when possible.
    /// </summary>
    public void PlayRandomSqueak()
    {
        if (audioSource == null || squeakClips == null || squeakClips.Length == 0)
        {
            return;
        }

        int index;
        do
        {
            index = Random.Range(0, squeakClips.Length);
        }
        while (index == lastSqueakIndex && squeakClips.Length > 3);

        lastSqueakIndex = index;
        audioSource.PlayOneShot(squeakClips[index]);
    }
}