using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(AudioSource))]
public class DuckClick : MonoBehaviour
{
    private Animator animator;
    private AudioSource audioSource;

    // New: lists for default and alternate duck sounds (assign in inspector)
    public AudioClip[] defaultClips;
    public AudioClip[] altClips;

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    void OnMouseDown()
    {
        StartCoroutine(PressRoutine());
    }

    IEnumerator PressRoutine()
    {
        animator.SetBool("Pressed", true);

        // Read the preference at click-time so toggling in settings takes effect immediately
        bool useAlt = PlayerPrefs.GetInt("DuckAlternate", 0) == 1;

        AudioClip clipToPlay = null;

        AudioClip[] sourceArray = useAlt ? altClips : defaultClips;

        if (sourceArray != null && sourceArray.Length > 0)
        {
            clipToPlay = sourceArray[Random.Range(0, sourceArray.Length)];
        }
        else
        {
            // fallback to the AudioSource's default clip if lists are empty
            clipToPlay = audioSource.clip;
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }

        yield return new WaitForSeconds(0.2f); // match animation length

        animator.SetBool("Pressed", false);
    }

    // AnimationEvent receiver required by the Animator's animation clip.
    // The animation has an AnimationEvent named "Pressed", so this method must exist on a component attached to the animated GameObject.
    // The event likely fires at the end of the press animation — clear the 'Pressed' flag here to be safe.
    public void Pressed()
    {
        ResetPressed();
    }

    public void ResetPressed()
    {
        animator.SetBool("Pressed", false);
    }
}