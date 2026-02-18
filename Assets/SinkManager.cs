using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SinkManager : MonoBehaviour
{
    public static SinkManager Instance { get; private set; }

    public enum SinkType
    {
        Basic = 0,
        PowerWasher = 1,
        WashBasin = 2,
        Dishwasher = 3
    }

    [Serializable]
    public class SinkNode
    {
        [Header("Identity")]
        public string id;
        public SinkType branch = SinkType.PowerWasher;
        public int tier = 1;

        [Header("Text")]
        public string displayName;
        [TextArea] public string description;
        [TextArea] public string loreDescription;

        [Header("Purchase")]
        public float cost = 0f;
        public bool isTechnique = false;

        [Tooltip("If true, purchasing this node commits the player to this sink branch (if still Basic).")]
        public bool unlocksSinkType = false;

        [Tooltip("All of these node IDs must be purchased before this node can be bought.")]
        public List<string> requires = new List<string>();
    }

    [Header("Nodes")]
    [Tooltip("Define your whole tree here. If empty at runtime, this script will seed defaults.")]
    public List<SinkNode> nodes = new List<SinkNode>();

    [Header("Reset Settings")]
    [Range(0f, 1f)]
    public float refundEfficiency = 0.9f;

    [Header("Sink Sprites (optional)")]
    public Sprite basicSinkSprite;
    public Sprite powerWasherSinkSprite;
    public Sprite washBasinSinkSprite;
    public Sprite dishwasherSinkSprite;

    public event Action<SinkType> OnSinkTypeChanged;
    public event Action<string> OnNodePurchased;
    public event Action OnSinkReset;

    [SerializeField] private SinkType currentSinkType = SinkType.Basic;
    private readonly HashSet<string> purchased = new HashSet<string>();
    private readonly Dictionary<string, SinkNode> nodeById = new Dictionary<string, SinkNode>();

    public SinkType CurrentSinkType => currentSinkType;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        BuildLookup();

        if (nodes == null || nodes.Count == 0)
        {
            SeedDefaults();
            BuildLookup();
        }
    }

    private void BuildLookup()
    {
        nodeById.Clear();

        if (nodes == null) return;

        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n == null) continue;
            if (string.IsNullOrWhiteSpace(n.id)) continue;

            if (!nodeById.ContainsKey(n.id))
                nodeById.Add(n.id, n);
            else
                Debug.LogWarning($"[SinkManager] Duplicate node id '{n.id}' found. IDs must be unique.");
        }
    }

    // --------------------------
    // Public API (state)
    // --------------------------

    public bool IsPurchased(string nodeId) => !string.IsNullOrWhiteSpace(nodeId) && purchased.Contains(nodeId);

    public SinkNode GetNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) return null;
        nodeById.TryGetValue(nodeId, out var node);
        return node;
    }

    public List<SinkNode> GetNodesForBranch(SinkType branch)
    {
        return nodes
            .Where(n => n != null && n.branch == branch)
            .OrderBy(n => n.tier)
            .ThenBy(n => n.displayName)
            .ToList();
    }

    public bool CanPurchase(string nodeId)
    {
        var node = GetNode(nodeId);
        if (node == null) return false;
        if (IsPurchased(nodeId)) return false;

        // Lockout after commit
        if (currentSinkType != SinkType.Basic && node.branch != currentSinkType)
            return false;

        // Must buy unlock node first if still Basic
        if (currentSinkType == SinkType.Basic && !node.unlocksSinkType)
            return false;

        // Prereqs
        if (node.requires != null && node.requires.Count > 0)
        {
            for (int i = 0; i < node.requires.Count; i++)
            {
                if (!IsPurchased(node.requires[i])) return false;
            }
        }

        return true;
    }

    public bool TryPurchase(string nodeId)
    {
        var node = GetNode(nodeId);
        if (node == null) return false;
        if (!CanPurchase(nodeId)) return false;

        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[SinkManager] ScoreManager.Instance not found.");
            return false;
        }

        float wallet = ScoreManager.Instance.GetTotalProfit();
        if (wallet < node.cost) return false;

        // Pay (purchase should be ignored by profit-rate average)
        ScoreManager.Instance.SubtractProfit(node.cost, isPurchase: true);

        purchased.Add(node.id);

        // Commit if this was the first unlock
        if (node.unlocksSinkType && currentSinkType == SinkType.Basic)
        {
            currentSinkType = node.branch;
            OnSinkTypeChanged?.Invoke(currentSinkType);
        }

        OnNodePurchased?.Invoke(node.id);
        return true;
    }

    public float GetTotalSpentOnSinks()
    {
        float sum = 0f;

        foreach (var id in purchased)
        {
            var n = GetNode(id);
            if (n != null) sum += Mathf.Max(0f, n.cost);
        }

        return sum;
    }

    public float GetRefundAmount()
    {
        return GetTotalSpentOnSinks() * Mathf.Clamp01(refundEfficiency);
    }

    public bool ResetToBasicAndRefund()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[SinkManager] ScoreManager.Instance not found.");
            return false;
        }

        if (currentSinkType == SinkType.Basic && purchased.Count == 0)
            return false;

        float refund = GetRefundAmount();

        purchased.Clear();
        currentSinkType = SinkType.Basic;

        // Refund should be ignored by ProfitRate average
        if (refund > 0f)
            ScoreManager.Instance.AddBubbleReward(refund);

        OnSinkTypeChanged?.Invoke(currentSinkType);
        OnSinkReset?.Invoke();
        return true;
    }

    public Sprite GetCurrentSinkSprite()
    {
        switch (currentSinkType)
        {
            case SinkType.PowerWasher: return powerWasherSinkSprite != null ? powerWasherSinkSprite : basicSinkSprite;
            case SinkType.WashBasin: return washBasinSinkSprite != null ? washBasinSinkSprite : basicSinkSprite;
            case SinkType.Dishwasher: return dishwasherSinkSprite != null ? dishwasherSinkSprite : basicSinkSprite;
            default: return basicSinkSprite;
        }
    }

    // --------------------------
    // Public API (modifiers)
    // These are read-only helpers for DishClicker later.
    // --------------------------

    // Power Washer
    public float GetPowerWasherBaseStagesPerSecond() => 2f;

    public float GetPowerWasherNozzleMultiplier()
    {
        // Treat “upgrade” nozzle line as override.
        if (IsPurchased("pw_industrial_pump")) return 1.25f;
        if (IsPurchased("pw_fire_hose")) return 1.20f;
        if (IsPurchased("pw_pressure_nozzle")) return 1.15f;
        return 1f;
    }

    public bool HasPowerWasherMomentum() => IsPurchased("pw_reinforced_hose") || IsPurchased("pw_kevlar_lining") || IsPurchased("pw_tanks_barrel");

    public void GetPowerWasherMomentumSettings(out float startAfterSeconds, out float perSecondBonus, out float maxBonus)
    {
        startAfterSeconds = 3f;

        if (IsPurchased("pw_tanks_barrel"))
        {
            perSecondBonus = 0.10f;
            maxBonus = 0.50f;
            return;
        }
        if (IsPurchased("pw_kevlar_lining"))
        {
            perSecondBonus = 0.07f;
            maxBonus = 0.40f;
            return;
        }
        if (IsPurchased("pw_reinforced_hose"))
        {
            perSecondBonus = 0.05f;
            maxBonus = 0.25f;
            return;
        }

        perSecondBonus = 0f;
        maxBonus = 0f;
    }

    public bool HasTurboJetTechnique() => IsPurchased("pw_turbo_jet");

    // Wash Basin
    public int GetWashBasinManualMultiplier()
    {
        if (currentSinkType != SinkType.WashBasin) return 1;
        return 2;
    }

    public int GetWashBasinFlatBonusDishes()
    {
        int bonus = 0;
        if (IsPurchased("wb_deeper_hole")) bonus += 2;
        if (IsPurchased("wb_even_deeper_hole")) bonus += 2;
        if (IsPurchased("wb_quarry")) bonus += 2;
        return bonus;
    }

    public bool HasOvernightSoakTechnique() => IsPurchased("wb_overnight_soak");

    public bool TryRollWashBasinExtraDishes(out int extra)
    {
        extra = 0;

        // Perfect soaker guarantees at least +1
        if (IsPurchased("wb_perfect_soaker"))
        {
            extra = 1;
            // Then roll for more using the defined table.
            int more = RollFromChances(new (int dishes, float chance)[]
            {
                (2, 0.25f),
                (3, 0.10f),
                (4, 0.10f),
                (5, 0.05f)
            });
            extra = Mathf.Max(extra, more);
            return true;
        }

        if (IsPurchased("wb_hands_and_feet"))
        {
            extra = RollFromChances(new (int dishes, float chance)[]
            {
                (1, 0.50f),
                (2, 0.25f),
                (3, 0.10f),
                (4, 0.05f)
            });
            return extra > 0;
        }

        if (IsPurchased("wb_ambidextrous"))
        {
            extra = RollFromChances(new (int dishes, float chance)[]
            {
                (1, 0.40f),
                (2, 0.15f),
                (3, 0.05f)
            });
            return extra > 0;
        }

        return false;
    }

    // Dishwasher
    public bool HasDishwasher() => currentSinkType == SinkType.Dishwasher;

    public float GetDishwasherCycleSeconds()
    {
        float seconds = 300f;

        if (IsPurchased("dw_faster_cycle")) seconds -= 60f;
        if (IsPurchased("dw_increase_water_pressure")) seconds -= 60f;
        if (IsPurchased("dw_turbo_dishwasher")) seconds -= 60f;

        return Mathf.Max(30f, seconds);
    }

    public float GetDishwasherDishesMultiplier()
    {
        // Treat “upgrade” line as override.
        if (IsPurchased("dw_xl_dishwasher")) return 2.50f;          // +150%
        if (IsPurchased("dw_efficient_placement")) return 2.00f;    // +100%
        if (IsPurchased("dw_more_racks")) return 1.50f;             // +50%
        return 1f;
    }

    public bool HasHeatDryBoostTechnique() => IsPurchased("dw_heat_dry_boost");

    // --------------------------
    // Save hooks (optional)
    // --------------------------

    public List<string> GetPurchasedNodeIds()
    {
        return purchased.ToList();
    }

    public void LoadFromSave(SinkType sinkType, List<string> purchasedNodeIds)
    {
        purchased.Clear();

        if (purchasedNodeIds != null)
        {
            for (int i = 0; i < purchasedNodeIds.Count; i++)
            {
                string id = purchasedNodeIds[i];
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (GetNode(id) == null) continue;
                purchased.Add(id);
            }
        }

        currentSinkType = sinkType;
        OnSinkTypeChanged?.Invoke(currentSinkType);
    }

    // --------------------------
    // Helpers
    // --------------------------

    private int RollFromChances((int dishes, float chance)[] table)
    {
        // Returns the MAX dishes hit by any entry (so 4 includes hitting 1, 2, etc).
        float r = UnityEngine.Random.value;
        int best = 0;

        float cumulative = 0f;
        for (int i = 0; i < table.Length; i++)
        {
            cumulative += Mathf.Clamp01(table[i].chance);
            if (r <= cumulative)
            {
                best = table[i].dishes;
                break;
            }
        }

        return best;
    }

    private void SeedDefaults()
    {
        nodes = new List<SinkNode>();

        // -------- Power Washer --------
        nodes.Add(new SinkNode
        {
            id = "pw_unlock",
            branch = SinkType.PowerWasher,
            tier = 1,
            displayName = "The Power Washer",
            description = "Unlock the power washer sink. Hold left click to wash. While held, stages auto-complete at 2 stages per second.",
            loreDescription = "",
            cost = 250f,
            unlocksSinkType = true
        });

        nodes.Add(new SinkNode
        {
            id = "pw_pressure_nozzle",
            branch = SinkType.PowerWasher,
            tier = 2,
            displayName = "Pressure Nozzle",
            description = "Increase stage completion rate by 15% while holding click.",
            loreDescription = "",
            cost = 600f,
            requires = new List<string> { "pw_unlock" }
        });

        nodes.Add(new SinkNode
        {
            id = "pw_reinforced_hose",
            branch = SinkType.PowerWasher,
            tier = 2,
            displayName = "Reinforced Hose",
            description = "Holding click for more than 3 seconds builds momentum, +5% per second, up to +25%.",
            loreDescription = "",
            cost = 600f,
            requires = new List<string> { "pw_unlock" }
        });

        nodes.Add(new SinkNode
        {
            id = "pw_turbo_jet",
            branch = SinkType.PowerWasher,
            tier = 2,
            displayName = "TECHNIQUE: Turbo Jet Mode",
            description = "Holding click for 30 seconds triggers a skill check. Success adds a burn effect that cleans twice as fast for 4 seconds.",
            loreDescription = "",
            cost = 1800f,
            isTechnique = true,
            requires = new List<string> { "pw_unlock" }
        });

        nodes.Add(new SinkNode
        {
            id = "pw_fire_hose",
            branch = SinkType.PowerWasher,
            tier = 3,
            displayName = "Fire Hose",
            description = "Upgrade Pressure Nozzle. Hold wash rate becomes +20%.",
            loreDescription = "",
            cost = 1800f,
            requires = new List<string> { "pw_pressure_nozzle" }
        });

        nodes.Add(new SinkNode
        {
            id = "pw_kevlar_lining",
            branch = SinkType.PowerWasher,
            tier = 3,
            displayName = "Kevlar Lining",
            description = "Upgrade Reinforced Hose. Momentum becomes +7% per second, up to +40%.",
            loreDescription = "",
            cost = 1800f,
            requires = new List<string> { "pw_reinforced_hose" }
        });

        nodes.Add(new SinkNode
        {
            id = "pw_industrial_pump",
            branch = SinkType.PowerWasher,
            tier = 4,
            displayName = "Industrial Pump",
            description = "Upgrade Fire Hose. Hold wash rate becomes +25%.",
            loreDescription = "",
            cost = 5000f,
            requires = new List<string> { "pw_fire_hose" }
        });

        nodes.Add(new SinkNode
        {
            id = "pw_tanks_barrel",
            branch = SinkType.PowerWasher,
            tier = 4,
            displayName = "A Tank's Barrel",
            description = "Upgrade Kevlar Lining. Momentum becomes +10% per second, up to +50%.",
            loreDescription = "",
            cost = 5000f,
            requires = new List<string> { "pw_kevlar_lining" }
        });

        // -------- Wash Basin --------
        nodes.Add(new SinkNode
        {
            id = "wb_unlock",
            branch = SinkType.WashBasin,
            tier = 1,
            displayName = "The Wash Basin",
            description = "Unlock the wash basin sink. Base manual dishes and all per-click dish upgrades are doubled.",
            loreDescription = "",
            cost = 250f,
            unlocksSinkType = true
        });

        nodes.Add(new SinkNode
        {
            id = "wb_deeper_hole",
            branch = SinkType.WashBasin,
            tier = 2,
            displayName = "Deeper Hole",
            description = "Increase basin depth. After manual washes, +2 dishes.",
            loreDescription = "",
            cost = 600f,
            requires = new List<string> { "wb_unlock" }
        });

        nodes.Add(new SinkNode
        {
            id = "wb_ambidextrous",
            branch = SinkType.WashBasin,
            tier = 2,
            displayName = "Ambidextrous",
            description = "Chance to clean extra dishes: 40% for +1, 15% for +2, 5% for +3.",
            loreDescription = "",
            cost = 600f,
            requires = new List<string> { "wb_unlock" }
        });

        nodes.Add(new SinkNode
        {
            id = "wb_overnight_soak",
            branch = SinkType.WashBasin,
            tier = 2,
            displayName = "TECHNIQUE: Overnight Soak",
            description = "Every 100 dishes cleaned, your next manual wash cleans double dishes.",
            loreDescription = "",
            cost = 1800f,
            isTechnique = true,
            requires = new List<string> { "wb_unlock" }
        });

        nodes.Add(new SinkNode
        {
            id = "wb_even_deeper_hole",
            branch = SinkType.WashBasin,
            tier = 3,
            displayName = "An Even Deeper Hole",
            description = "Increase depth further. +2 more dishes.",
            loreDescription = "",
            cost = 1800f,
            requires = new List<string> { "wb_deeper_hole" }
        });

        nodes.Add(new SinkNode
        {
            id = "wb_hands_and_feet",
            branch = SinkType.WashBasin,
            tier = 3,
            displayName = "Hands and Feet",
            description = "Increases extra dish odds and cap: 50% +1, 25% +2, 10% +3, 5% +4.",
            loreDescription = "",
            cost = 1800f,
            requires = new List<string> { "wb_ambidextrous" }
        });

        nodes.Add(new SinkNode
        {
            id = "wb_quarry",
            branch = SinkType.WashBasin,
            tier = 4,
            displayName = "A Quarry",
            description = "Increase depth again. +2 more dishes.",
            loreDescription = "",
            cost = 5000f,
            requires = new List<string> { "wb_even_deeper_hole" }
        });

        nodes.Add(new SinkNode
        {
            id = "wb_perfect_soaker",
            branch = SinkType.WashBasin,
            tier = 4,
            displayName = "The Perfect Soaker",
            description = "Guarantees +1 extra dish. Improves odds for more: 25% +2, 10% +3, 10% +4, 5% +5.",
            loreDescription = "",
            cost = 5000f,
            requires = new List<string> { "wb_hands_and_feet" }
        });

        // -------- Dishwasher --------
        nodes.Add(new SinkNode
        {
            id = "dw_unlock",
            branch = SinkType.Dishwasher,
            tier = 1,
            displayName = "The Dishwashing Machine",
            description = "Unlock the dishwasher sink. Adds a 5 minute rinse cycle. When complete, it auto-washes the amount of dishes your manual wash would do.",
            loreDescription = "",
            cost = 250f,
            unlocksSinkType = true
        });

        nodes.Add(new SinkNode
        {
            id = "dw_more_racks",
            branch = SinkType.Dishwasher,
            tier = 2,
            displayName = "More Racks",
            description = "Increase dishes done per cycle by 50%.",
            loreDescription = "",
            cost = 600f,
            requires = new List<string> { "dw_unlock" }
        });

        nodes.Add(new SinkNode
        {
            id = "dw_faster_cycle",
            branch = SinkType.Dishwasher,
            tier = 2,
            displayName = "Faster Cycle",
            description = "Decrease rinse cycle by 1 minute.",
            loreDescription = "",
            cost = 600f,
            requires = new List<string> { "dw_unlock" }
        });

        nodes.Add(new SinkNode
        {
            id = "dw_heat_dry_boost",
            branch = SinkType.Dishwasher,
            tier = 2,
            displayName = "TECHNIQUE: Heat Dry Boost",
            description = "Every 10th auto-wash yields double cash.",
            loreDescription = "",
            cost = 1800f,
            isTechnique = true,
            requires = new List<string> { "dw_unlock" }
        });

        nodes.Add(new SinkNode
        {
            id = "dw_efficient_placement",
            branch = SinkType.Dishwasher,
            tier = 3,
            displayName = "Efficient Placement",
            description = "Increase dishes done per cycle by 100%.",
            loreDescription = "",
            cost = 1800f,
            requires = new List<string> { "dw_more_racks" }
        });

        nodes.Add(new SinkNode
        {
            id = "dw_increase_water_pressure",
            branch = SinkType.Dishwasher,
            tier = 3,
            displayName = "Increase Water Pressure",
            description = "Decrease rinse cycle by an additional minute.",
            loreDescription = "",
            cost = 1800f,
            requires = new List<string> { "dw_faster_cycle" }
        });

        nodes.Add(new SinkNode
        {
            id = "dw_xl_dishwasher",
            branch = SinkType.Dishwasher,
            tier = 4,
            displayName = "XL Dishwasher",
            description = "Increase dishes done per cycle by 150%.",
            loreDescription = "",
            cost = 5000f,
            requires = new List<string> { "dw_efficient_placement" }
        });

        nodes.Add(new SinkNode
        {
            id = "dw_turbo_dishwasher",
            branch = SinkType.Dishwasher,
            tier = 4,
            displayName = "Turbo Dishwasher",
            description = "Decrease rinse cycle by an additional minute.",
            loreDescription = "",
            cost = 5000f,
            requires = new List<string> { "dw_increase_water_pressure" }
        });
    }
}
