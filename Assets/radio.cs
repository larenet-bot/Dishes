using UnityEngine;
using System.Collections;

public class Radio : MonoBehaviour
{
    [Header("Radio Songs")]
    public AudioClip[] RadioSongs;
    public AudioClip StaticSound;

    private AudioClip lastSong;

    void OnMouseDown()
    {
        SwitchSong();
    }

    public void SwitchSong()
    {
        if (RadioSongs.Length == 0) return;

        int index = Random.Range(0, RadioSongs.Length);

        while (RadioSongs.Length > 1 && RadioSongs[index] == lastSong)
        {
            index = Random.Range(0, RadioSongs.Length);
        }

        AudioClip chosen = RadioSongs[index];
        lastSong = chosen;

        StartCoroutine(PlayStaticThenSong(chosen));
    }

    private IEnumerator PlayStaticThenSong(AudioClip newSong)
    {
        // 1. Fade out current music
        yield return StartCoroutine(AudioManager.instance.FadeOutMusic(0.4f));

        // 2. Play static SFX immediately
        float staticLength = 0f;
        if (StaticSound != null)
        {
            AudioManager.instance.PlaySFX(StaticSound);
            staticLength = StaticSound.length;
        }

        // 3. Wait EXACTLY for static length — no extra time
        yield return new WaitForSeconds(staticLength);

        // 4. Instantly start the new song
        AudioManager.instance.PlayMusic(newSong);
    }

}
