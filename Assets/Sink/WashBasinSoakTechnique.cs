using UnityEngine;

/// <summary>
/// Overnight Soak technique state.
/// Every N dishes cleaned (manual completions), the next manual wash awards double dishes.
/// 
/// This is separated from SinkManager so renaming node IDs doesn't break logic.
/// </summary>
public class WashBasinSoakTechnique : MonoBehaviour
{
    [Header("Refs")]
    public SinkManager sinkManager;

    [Header("Node ID")]
    [Tooltip("The SinkManager node ID for Overnight Soak.")]
    public string techniqueNodeId = "wb_technique";

    [Header("Tuning")]
    public long dishesPerSoak = 100;

    private long progress;
    private bool ready;

    public bool IsReady => ready;

    /// <summary>0..1 progress toward the next soak.</summary>
    public float Progress01
    {
        get
        {
            if (dishesPerSoak <= 0) return 0f;
            return Mathf.Clamp01((float)progress / dishesPerSoak);
        }
    }

    private void Awake()
    {
        if (sinkManager == null) sinkManager = SinkManager.Instance;

        if (sinkManager != null)
        {
            sinkManager.OnSinkReset += ResetState;
            sinkManager.OnSinkTypeChanged += OnSinkTypeChanged;
        }
    }

    private void OnDestroy()
    {
        if (sinkManager != null)
        {
            sinkManager.OnSinkReset -= ResetState;
            sinkManager.OnSinkTypeChanged -= OnSinkTypeChanged;
        }
    }

    private void OnSinkTypeChanged(SinkManager.SinkType newType)
    {
        // If the player leaves the wash basin, clear the technique state.
        if (newType != SinkManager.SinkType.WashBasin)
            ResetState();
    }

    public void ResetState()
    {
        progress = 0;
        ready = false;
    }

    public bool HasTechnique()
    {
        if (sinkManager == null) return false;
        if (sinkManager.CurrentSinkType != SinkManager.SinkType.WashBasin) return false;
        if (string.IsNullOrWhiteSpace(techniqueNodeId)) return false;
        return sinkManager.IsPurchased(techniqueNodeId);
    }

    /// <summary>
    /// Preview-only: returns what the award would be if a manual wash completed right now.
    /// This does NOT consume the ready state or advance progress.
    /// </summary>
    public long PreviewApplySoak(long dishesAwarded)
    {
        if (!HasTechnique()) return dishesAwarded;
        if (!ready) return dishesAwarded;
        return dishesAwarded * 2;
    }

    /// <summary>
    /// Call this once per manual completion after awarding dishes.
    /// This consumes the ready state (if present), then advances progress.
    /// </summary>
    public void OnManualWashCompleted(long dishesAwarded)
    {
        if (!HasTechnique()) return;
        if (dishesPerSoak <= 0) return;
        if (dishesAwarded <= 0) return;

        // If we were ready, this wash is considered the doubled wash.
        if (ready)
            ready = false;

        progress += dishesAwarded;

        while (progress >= dishesPerSoak)
        {
            progress -= dishesPerSoak;
            ready = true;
        }
    }
}
