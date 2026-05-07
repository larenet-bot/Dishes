using System.Collections;
using UnityEngine;

public class Note : MonoBehaviour
{
    [HideInInspector] public int lane;
    public float targetTime;

    public bool canBeHit = false;
    public bool wasHit = false;

    public HitWindow hitWindow;
    public AudioSource musicSource;

    public SpriteRenderer noteSprite;
    public Color highlightColor = Color.yellow;
    private Color originalColor;

    // Optional sprite to use while the note is hittable
    [Tooltip("Optional: sprite to display while the note is inside the HitBox (hittable).")]
    public Sprite hitSprite;

    // store the original sprite so we can restore it on exit
    private Sprite originalSprite;

    // Glow settings
    [Header("Glow")]
    [Tooltip("Enable a simple pulsing glow when the note is in the hit area.")]
    public bool enableGlow = true;
    [Tooltip("Glow color (alpha controls max glow intensity).")]
    public Color glowColor = new Color(1f, 0.9f, 0.4f, 0.6f);
    [Tooltip("How much larger the glow sprite is compared to the note.")]
    public float glowScaleMultiplier = 1.4f;
    [Tooltip("Pulse speed of the glow.")]
    public float glowPulseSpeed = 2f;

    private GameObject glowObj;
    private SpriteRenderer glowSpriteRenderer;
    private Coroutine glowPulseCoroutine;

    // Optional DSP-aware fields (set by spawner.Initialize)
    private double songStartDspTime = -1.0;

    void Start()
    {
        if (noteSprite != null)
        {
            originalColor = noteSprite.color;

            // capture original sprite here if available.
            // NoteSpawner may set the sprite immediately after Instantiate,
            // so capturing in Start is usually correct. We also guard
            // and capture lazily in OnTriggerEnter if needed.
            originalSprite = noteSprite.sprite;
        }
    }

    /// <summary>
    /// Lightweight initializer called by NoteSpawner so the note has access
    /// to the song DSP start time. Non-destructive: existing behavior unchanged.
    /// </summary>
    public void Initialize(float noteTime, double songStartDsp)
    {
        this.targetTime = noteTime;
        this.songStartDspTime = songStartDsp;
    }

    /// <summary>
    /// Returns seconds until the target hit time according to DSP clock.
    /// If Initialize wasn't called, falls back to Time.time-based estimate.
    /// </summary>
    public float GetTimeUntilHit()
    {
        if (songStartDspTime > 0.0)
        {
            double songTime = AudioSettings.dspTime - songStartDspTime;
            return targetTime - (float)songTime;
        }
        else
        {
            // best-effort fallback (not sample-accurate)
            return targetTime - (Time.time - 0f);
        }
    }

    // Called by LaneKey, receives correct time difference
    public void Hit(float timeDifference)
    {
        // Only accept hit if hittable and not already hit
        if (!canBeHit || wasHit)
            return;

        // mark as hit to prevent duplicates
        wasHit = true;

        // Judge the hit (updates MiniScoreManager and HUD)
        if (hitWindow != null)
            hitWindow.JudgeNoteHit(timeDifference);

        // cleanup any glow and then destroy immediately to avoid MISS on trigger exit
        DestroyGlow();
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("HitBox"))
        {
            canBeHit = true;

            if (noteSprite != null)
            {
                // Ensure we captured the original sprite (in case Start ran before spawner set it)
                if (originalSprite == null)
                    originalSprite = noteSprite.sprite;

                // swap to hitSprite if provided
                if (hitSprite != null)
                    noteSprite.sprite = hitSprite;

                noteSprite.color = highlightColor;

                // create glow halo
                CreateGlow();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("HitBox"))
        {
            canBeHit = false;

            if (!wasHit)
            {
                MiniScoreManager.AddMiss();
                UI_RhythmHUD.Instance.ShowFeedback("MISS");
            }

            if (noteSprite != null)
            {
                // restore original color and sprite
                noteSprite.color = originalColor;
                if (originalSprite != null)
                    noteSprite.sprite = originalSprite;
            }

            // remove glow and destroy
            DestroyGlow();
            Destroy(gameObject);
        }
    }

    private void CreateGlow()
    {
        if (!enableGlow) return;
        if (noteSprite == null) return;
        if (noteSprite.sprite == null) return;
        if (glowObj != null) return;

        glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(noteSprite.transform, false);
        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localRotation = Quaternion.identity;
        glowObj.transform.localScale = Vector3.one * glowScaleMultiplier;

        glowSpriteRenderer = glowObj.AddComponent<SpriteRenderer>();
        glowSpriteRenderer.sprite = noteSprite.sprite;

        // try to match sorting layer and put glow behind the note
        try
        {
            glowSpriteRenderer.sortingLayerName = noteSprite.sortingLayerName;
            glowSpriteRenderer.sortingOrder = noteSprite.sortingOrder - 1;
        }
        catch { /* ignore if sorting layer not found */ }

        Color c = glowColor;
        // ensure base alpha is preserved as max intensity
        glowSpriteRenderer.color = c;

        // use same material if available, otherwise default sprite shader
        glowSpriteRenderer.sharedMaterial = noteSprite.sharedMaterial != null
            ? noteSprite.sharedMaterial
            : new Material(Shader.Find("Sprites/Default"));

        glowPulseCoroutine = StartCoroutine(GlowPulse());
    }

    private IEnumerator GlowPulse()
    {
        if (glowSpriteRenderer == null) yield break;

        float t = 0f;
        Color baseColor = glowSpriteRenderer.color;
        float baseAlpha = baseColor.a;
        while (true)
        {
            t += Time.deltaTime * glowPulseSpeed;
            float pulse = (Mathf.Sin(t) * 0.5f + 0.5f); // 0..1
            baseColor.a = Mathf.Clamp01(pulse * baseAlpha);
            if (glowSpriteRenderer != null)
                glowSpriteRenderer.color = baseColor;
            yield return null;
        }
    }

    private void DestroyGlow()
    {
        if (glowPulseCoroutine != null)
        {
            try { StopCoroutine(glowPulseCoroutine); } catch { }
            glowPulseCoroutine = null;
        }

        if (glowObj != null)
        {
            try { Destroy(glowObj); } catch { }
            glowObj = null;
            glowSpriteRenderer = null;
        }
    }
}