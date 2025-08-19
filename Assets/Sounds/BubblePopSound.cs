using UnityEngine;

public class BubblePopSound : MonoBehaviour
{
    public AudioClip[] popSounds; // assign sounds in inspector
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayRandomPopSound()
    {
        if (popSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, popSounds.Length);
            audioSource.PlayOneShot(popSounds[randomIndex]);
        }
    }
}
