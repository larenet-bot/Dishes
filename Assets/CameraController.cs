using UnityEngine;

/// <summary>
/// Simple camera controles:
/// - press 1 to move to the full view
/// - press 2 to move to the sink view 
/// - hold right mouse button and drag to pan the camera around
/// - Use mouse scrollwheel to adjust distance 
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Presets")]
    [Tooltip("Transform representing the whole-kitchen view (position & rotation).")]
    public Transform kitchenTransform;

    [Tooltip("Transform representing the sink-focused view (position & rotation).")]
    public Transform sinkTransform;

    [Header("Pan & Zoom")]
    [Tooltip("Pan speed (when dragging with right mouse).")]
    public float panSpeed = 0.5f;

    [Tooltip("Scroll wheel zoom speed.")]
    public float scrollSpeed = 5f;

    [Tooltip("How fast the camera moves to a preset.")]
    public float moveSmoothTime = 0.15f;

    [Tooltip("Limits for scroll-based distance changes.")]
    public float minDistance = 1f;
    public float maxDistance = 20f;

    // Internal smoothing state
    private Vector3 velocity = Vector3.zero;
    private Quaternion rotVelocity = Quaternion.identity;
    private Transform targetTransform; // current target preset transform (if any)
    private float targetDistance;
    private bool isMovingToPreset;

    void Start()
    {
        // initialize distance from camera forward to a point in front of camera
        targetDistance = Vector3.Distance(transform.position, transform.position + transform.forward);
        targetDistance = Mathf.Clamp(Vector3.Magnitude(transform.localPosition), minDistance, maxDistance);
    }

    void Update()
    {
        HandlePresetInput();
        HandlePanInput();
        HandleScrollZoom();
        ApplyMovementSmoothing();
    }

    void HandlePresetInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && kitchenTransform != null)
        {
            SetTargetPreset(kitchenTransform);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && sinkTransform != null)
        {
            SetTargetPreset(sinkTransform);
        }
    }

    void SetTargetPreset(Transform preset)
    {
        targetTransform = preset;
        isMovingToPreset = true;
    }

    void HandlePanInput()
    {
        // Pan when right mouse button is held
        if (Input.GetMouseButton(1))
        {
            isMovingToPreset = false; // cancel preset movement while panning

            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");

            // Move in camera's local plane: right and up
            Vector3 right = transform.right;
            Vector3 up = transform.up;

            Vector3 pan = (-right * mx + -up * my) * panSpeed;
            // Use world translation so panning feels natural regardless of rotation
            transform.position += pan;
        }
    }

    void HandleScrollZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            isMovingToPreset = false; // cancel preset movement when user manually zooms

            // Move camera forward/back along its forward axis
            float amount = scroll * scrollSpeed;
            Vector3 newPos = transform.position + transform.forward * amount;

            // Clamp by distance from a simple pivot: use camera's parent or world origin as pivot.
            float dist = Vector3.Distance(newPos, newPos - transform.forward * 1f);
            // Here apply simple clamping on the camera's local position magnitude to avoid runaway zoom.
            float localMag = newPos.magnitude;
            localMag = Mathf.Clamp(localMag, minDistance, maxDistance);
            // Apply new position (basic clamp)
            transform.position = newPos;
        }
    }

    void ApplyMovementSmoothing()
    {
        if (isMovingToPreset && targetTransform != null)
        {
            // Smoothly interpolate position and rotation to the preset
            transform.position = Vector3.SmoothDamp(transform.position, targetTransform.position, ref velocity, moveSmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetTransform.rotation, Time.deltaTime / Mathf.Max(moveSmoothTime, 0.0001f));

            // If close enough, stop moving
            if (Vector3.Distance(transform.position, targetTransform.position) < 0.01f &&
                Quaternion.Angle(transform.rotation, targetTransform.rotation) < 0.5f)
            {
                isMovingToPreset = false;
            }
        }
    }
}
