
using UnityEngine;

/// <summary>
/// Centralized wash-basin rules and helpers.
/// Delegates purchase/state checks to SinkManager but keeps the basin-specific calculation here.
/// </summary>
public class BasinManager : MonoBehaviour
{
    public static BasinManager Instance { get; private set; }

    private SinkManager sinkManager;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        sinkManager = SinkManager.Instance;
    }

    private void EnsureSinkManager()
    {
        if (sinkManager == null) sinkManager = SinkManager.Instance;
    }

    public int GetManualMultiplier()
    {
        EnsureSinkManager();
        if (sinkManager == null) return 1;
        return sinkManager.GetWashBasinManualMultiplier();
    }

    public int GetFlatBonusDishes()
    {
        EnsureSinkManager();
        if (sinkManager == null) return 0;
        return sinkManager.GetWashBasinFlatBonusDishes();
    }

    public bool TryRollExtraDishes(out int extra)
    {
        EnsureSinkManager();
        if (sinkManager == null)
        {
            extra = 0;
            return false;
        }
        return sinkManager.TryRollWashBasinExtraDishes(out extra);
    }

    /// <summary>
    /// Calculates manual dishes awarded, applying wash-basin multipliers, flat bonuses,
    /// extra-rolls, and optionally the soak technique preview.
    /// </summary>
    public long CalculateManualDishesAwarded(long baseIncrement, WashBasinSoakTechnique soakTechnique = null)
    {
        long awarded = baseIncrement;

        EnsureSinkManager();
        if (sinkManager != null && sinkManager.CurrentSinkType == SinkManager.SinkType.WashBasin)
        {
            int multiplier = Mathf.Max(1, GetManualMultiplier());
            awarded = (long)baseIncrement * multiplier;

            awarded += GetFlatBonusDishes();

            if (TryRollExtraDishes(out int extra))
            {
                awarded += Mathf.Max(0, extra);
            }

            if (soakTechnique != null)
            {
                awarded = soakTechnique.PreviewApplySoak(awarded);
            }
        }

        if (awarded < 1) awarded = 1;
        return awarded;
    }
}