using UnityEngine;
using UnityEngine.EventSystems;

public class CameraPanClamp2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform backgroundBounds; // long background object

    [Header("Input")]
    [SerializeField] private bool panWithKeyboard = true;
    [SerializeField] private float keyboardSpeed = 12f;

    [SerializeField] private bool panWithMouseDrag = true;
    [SerializeField] private int dragMouseButton = 2; // 2 = Middle Mouse (won't conflict with LMB gameplay)
    [SerializeField] private float dragSpeed = 0.02f; // higher = faster drag

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.08f;

    private Bounds worldBounds;
    private float targetX;
    private float velX;

    private void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
        CacheBounds();

        targetX = transform.position.x;
    }

    private void LateUpdate()
    {
        CacheBoundsIfNeeded();

        float deltaX = 0f;

        // Keyboard pan (A/D, Left/Right arrows)
        if (panWithKeyboard)
        {
            float h = Input.GetAxisRaw("Horizontal");
            deltaX += h * keyboardSpeed * Time.unscaledDeltaTime;
        }

        // Mouse drag pan (MMB by default)
        if (panWithMouseDrag && Input.GetMouseButton(dragMouseButton))
        {
            // Optional: avoid panning while over UI
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                float mx = Input.GetAxis("Mouse X");
                deltaX += -mx * dragSpeed * cam.orthographicSize;
            }
        }

        targetX += deltaX;

        // Clamp to background edges
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float minX = worldBounds.min.x + halfW;
        float maxX = worldBounds.max.x - halfW;

        // If camera is wider than the bounds, lock to center
        if (minX > maxX)
        {
            targetX = worldBounds.center.x;
        }
        else
        {
            targetX = Mathf.Clamp(targetX, minX, maxX);
        }

        float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref velX, smoothTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    private void CacheBoundsIfNeeded()
    {
        // In case you resize/scale the background at runtime in-editor
        // (cheap enough to do every frame if you want, but this is fine)
        if (backgroundBounds == null) return;
    }

    private void CacheBounds()
    {
        if (backgroundBounds == null)
        {
            Debug.LogWarning("[CameraPanClamp2D] No backgroundBounds assigned.");
            worldBounds = new Bounds(Vector3.zero, Vector3.one);
            return;
        }

        // Prefer Collider2D bounds (explicit boundary), fall back to SpriteRenderer bounds
        var col2D = backgroundBounds.GetComponent<Collider2D>();
        if (col2D != null)
        {
            worldBounds = col2D.bounds;
            return;
        }

        var sr = backgroundBounds.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            worldBounds = sr.bounds;
            return;
        }

        Debug.LogWarning("[CameraPanClamp2D] backgroundBounds needs a Collider2D or SpriteRenderer.");
        worldBounds = new Bounds(backgroundBounds.position, Vector3.one);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
    }
#endif
}