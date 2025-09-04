using UnityEngine;
using System.Collections;

public class Radio : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip[] RadioSongs;      // assign songs in inspector
    public AudioClip StaticSound;       // assign static noise in inspector

    [Header("Settings")]
    public float PauseBetweenSongs = 2f; // seconds of silence before next song

    private AudioSource audioSource;
    private AudioClip lastSong;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // If nothing is playing, wait a bit, then play next song
        if (!audioSource.isPlaying && !IsInvoking(nameof(PlayNextSong)))
        {
            Invoke(nameof(PlayNextSong), PauseBetweenSongs);
        }
    }

    //  CLICK DETECTION 
    void OnMouseDown()
    {
        SwitchSong();
    }

    public void SwitchSong()
    {
        if (RadioSongs.Length > 0)
        {
            int randomIndex = Random.Range(0, RadioSongs.Length);
            AudioClip chosenSong = RadioSongs[randomIndex];
            StartCoroutine(PlayStaticThenSong(chosenSong));
        }
    }

    private IEnumerator PlayStaticThenSong(AudioClip song)
    {
        // stop current song
        audioSource.Stop();

        // play static
        if (StaticSound != null)
        {
            audioSource.PlayOneShot(StaticSound, 1f);
            yield return new WaitForSeconds(StaticSound.length);
        }

        // play new song
        PlaySong(song);
    }

    private void PlayNextSong()
    {
        if (RadioSongs.Length == 0) return;

        int randomIndex = Random.Range(0, RadioSongs.Length);

        // ensure  not the same song twice 
        while (RadioSongs.Length > 1 && RadioSongs[randomIndex] == lastSong)
        {
            randomIndex = Random.Range(0, RadioSongs.Length);
        }

        PlaySong(RadioSongs[randomIndex]);
    }

    private void PlaySong(AudioClip clip)
    {
        lastSong = clip;
        audioSource.clip = clip;
        audioSource.Play();
    }
}
