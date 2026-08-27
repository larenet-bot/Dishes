using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class EmployeePurchaseSpin : MonoBehaviour
{
    [Tooltip("Duration of the spin in seconds.")]
    public float duration = 0.6f;

    [Tooltip("Number of full Y-axis spins to perform.")]
    public int spins = 3;

    [Tooltip("Use unscaled time (ignore timeScale).")]
    public bool useUnscaledTime = false;

    private Coroutine spinCoroutine;
    private Animator cachedAnimator;
    private bool animatorWasEnabled;
    private Quaternion originalLocalRotation;
    private Vector3 originalLocalScale;
    private bool originalCaptured;

    private void Awake()
    {
        CacheOriginalTransform();
        cachedAnimator = GetComponent<Animator>();
    }

    private void OnValidate()
    {
        if (spins < 0) spins = 0;
        if (duration < 0f) duration = 0f;
    }

    private void CacheOriginalTransform()
    {
        originalLocalRotation = transform.localRotation;
        originalLocalScale = transform.localScale;
        originalCaptured = true;
    }

    public void PlaySpin()
    {
        if (!isActiveAndEnabled)
            return;

        if (!originalCaptured)
            CacheOriginalTransform();

        // If a spin is already running, stop it and restore the transform so the new spin always starts from the same baseline.
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            spinCoroutine = null;

            // restore transform and animator to original state
            transform.localRotation = originalLocalRotation;
            transform.localScale = originalLocalScale;

            if (cachedAnimator != null)
                cachedAnimator.enabled = animatorWasEnabled;
        }

        spinCoroutine = StartCoroutine(SpinCoroutine());
    }

    private IEnumerator SpinCoroutine()
    {
        // save baseline for this run
        Quaternion startRotation = originalLocalRotation;
        Vector3 startScale = originalLocalScale;

        // If animator exists, disable it for the duration of the spin to avoid transform conflicts.
        if (cachedAnimator != null)
        {
            animatorWasEnabled = cachedAnimator.enabled;
            if (animatorWasEnabled)
                cachedAnimator.enabled = false;
        }

        float elapsed = 0f;
        float totalAngle = 360f * Mathf.Max(0, spins);

        while (elapsed < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float t = (duration > 0f) ? Mathf.Clamp01(elapsed / duration) : 1f;

            // Interpolate angle and apply relative to the original rotation so we always return to the same baseline.
            float angle = Mathf.Lerp(0f, totalAngle, t);
            transform.localRotation = startRotation * Quaternion.Euler(0f, angle, 0f);

            // Ensure the scale remains unchanged during the spin.
            transform.localScale = startScale;

            yield return null;
        }

        // Ensure final state exactly matches the original baseline (no residual rotation).
        transform.localRotation = startRotation;
        transform.localScale = startScale;

        // Re-enable animator if it was enabled before the spin and rebind to avoid visual snapping later.
        if (cachedAnimator != null && animatorWasEnabled)
        {
            cachedAnimator.enabled = true;
            cachedAnimator.Rebind();
            cachedAnimator.Update(0f);
        }

        spinCoroutine = null;
    }
}