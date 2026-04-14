using System.Collections;
using UnityEngine;

public class DuckClick : MonoBehaviour
{
    private Animator animator;
    private AudioSource audioSource;

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
        audioSource.PlayOneShot(audioSource.clip);

        yield return new WaitForSeconds(0.2f); // match animation length

        animator.SetBool("Pressed", false);
    }
    public void ResetPressed()
    {
        animator.SetBool("Pressed", false);
    }
}