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

    // Overnight Soak tracking (wash basin technique)
    private long washBasinSoakProgress = 0;
    private bool washBasinSoakReady = false;

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
        if (nodes == null) return new List<SinkNode>();

        return nodes
            .Select((n, idx) => new { n, idx })
            .Where(x => x.n != null && x.n.branch == branch)
            .OrderBy(x => x.n.tier)
            .ThenBy(x => x.idx) // keeps your inspector order within a tier
            .Select(x => x.n)
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

        washBasinSoakProgress = 0;
        washBasinSoakReady = false;
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
        if (IsPurchased("pw_rate3")) return 1.25f;
        if (IsPurchased("pw_rate2")) return 1.20f;
        if (IsPurchased("pw_rate1")) return 1.15f;
        return 1f;
    }


    public bool HasPowerWasherMomentum() => HasAny("pw_momentum1", "pw_momentum2", "pw_momentum3");
    public void GetPowerWasherMomentumSettings(out float startAfterSeconds, out float perSecondBonus, out float maxBonus)
    {
        startAfterSeconds = 3f;

        if (IsPurchased("pw_momentum3"))
        {
            perSecondBonus = 0.10f;
            maxBonus = 0.50f;
            return;
        }
        if (IsPurchased("pw_momentum2"))
        {
            perSecondBonus = 0.07f;
            maxBonus = 0.40f;
            return;
        }
        if (IsPurchased("pw_momentum1"))
        {
            perSecondBonus = 0.05f;
            maxBonus = 0.25f;
            return;
        }

        perSecondBonus = 0f;
        maxBonus = 0f;
    }


    public bool HasTurboJetTechnique() => IsPurchased("pw_technique");
    // Wash Basin
    public int GetWashBasinManualMultiplier()
    {
        if (currentSinkType != SinkType.WashBasin) return 1;
        return 2;
    }

    public int GetWashBasinFlatBonusDishes()
    {
        int bonus = 0;
        if (IsPurchased("wb_hole1")) bonus += 2;
        if (IsPurchased("wb_hole2")) bonus += 2;
        if (IsPurchased("wb_hole3")) bonus += 2;
        return bonus;
    }


    public bool HasOvernightSoakTechnique() => IsPurchased("wb_technique");
    public bool TryRollWashBasinExtraDishes(out int extra)
    {
        extra = 0;

        if (IsPurchased("wb_chance3"))
        {
            extra = 1;

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

        if (IsPurchased("wb_chance2"))
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

        if (IsPurchased("wb_chance1"))
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


    // Overnight Soak (Wash Basin technique)
    public bool IsOvernightSoakReady()
    {
        return washBasinSoakReady;
    }

    public void AddOvernightSoakProgress(long dishesCleaned)
    {
        if (currentSinkType != SinkType.WashBasin) return;
        if (!HasOvernightSoakTechnique()) return;
        if (washBasinSoakReady) return;

        if (dishesCleaned < 0) dishesCleaned = 0;

        washBasinSoakProgress += dishesCleaned;

        if (washBasinSoakProgress >= 100)
            washBasinSoakReady = true;
    }

    public long ApplyOvernightSoakIfReady(long dishesAwarded)
    {
        if (currentSinkType != SinkType.WashBasin) return dishesAwarded;
        if (!HasOvernightSoakTechnique()) return dishesAwarded;
        if (!washBasinSoakReady) return dishesAwarded;

        washBasinSoakReady = false;
        washBasinSoakProgress = 0;

        return dishesAwarded * 2;
    }

    // Dishwasher
    public bool HasDishwasher() => currentSinkType == SinkType.Dishwasher;

    public float GetDishwasherCycleSeconds()
    {
        float seconds = 300f;

        if (IsPurchased("dw_time1")) seconds -= 60f;
        if (IsPurchased("dw_time2")) seconds -= 60f;
        if (IsPurchased("dw_time3")) seconds -= 60f;

        return Mathf.Max(30f, seconds);
    }


    public float GetDishwasherDishesMultiplier()
    {
        if (IsPurchased("dw_amount3")) return 2.50f;          // +150%
        if (IsPurchased("dw_amount2")) return 2.00f;          // +100%
        if (IsPurchased("dw_amount1")) return 1.50f;          // +50%
        return 1f;
    }


    public bool HasHeatDryBoostTechnique() => IsPurchased("dw_technique");
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

    private bool HasAny(params string[] ids)
    {
        if (ids == null) return false;
        for (int i = 0; i < ids.Length; i++)
        {
            if (IsPurchased(ids[i])) return true;
        }
        return false;
    }

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
            id = "pw_rate1",
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
            id = "pw_momentum1",
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
            id = "pw_technique",
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
            id = "pw_rate2",
            branch = SinkType.PowerWasher,
            tier = 3,
            displayName = "Fire Hose",
            description = "Upgrade Pressure Nozzle. Hold wash rate becomes +20%.",
            loreDescription = "",
            cost = 1800f,
            requires = new List<string> { "pw_rate1" }
        });

        nodes.Add(new SinkNode
        {
            id = "pw_momentum2",
            branch = SinkType.PowerWasher,
            tier = 3,
            displayName = "Kevlar Lining",
            description = "Upgrade Reinforced Hose. Momentum becomes +7% per second, up to +40%.",
            loreDescription = "",
            cost = 1800f,
            requires = new List<string> { "pw_momentum1" }
        });

        nodes.Add(new SinkNode
        {
            id = "pw_rate3",
            branch = SinkType.PowerWasher,
            tier = 4,
            displayName = "Industrial Pump",
            description = "Upgrade Fire Hose. Hold wash rate becomes +25%.",
            loreDescription = "",
            cost = 5000f,
            requires = new List<string> { "pw_rate2" }
        });

        nodes.Add(new SinkNode
        {
            id = "pw_momentum3",
            branch = SinkType.PowerWasher,
            tier = 4,
            displayName = "A Tank's Barrel",
            description = "Upgrade Kevlar Lining. Momentum becomes +10% per second, up to +50%.",
            loreDescription = "",
            cost = 5000f,
            requires = new List<string> { "pw_momentum2" }
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
            id = "wb_hole1",
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
            id = "wb_chance1",
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
            id = "wb_technique",
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
            id = "wb_hole2",
            branch = SinkType.WashBasin,
            tier = 3,
            displayName = "An Even Deeper Hole",
            description = "Increase depth further. +2 more dishes.",
            loreDescription = "",
            cost = 1800f,
            requires = new List<string> { "wb_hole1" }
        });

        nodes.Add(new SinkNode
        {
            id = "wb_chance2",
            branch = SinkType.WashBasin,
            tier = 3,
            displayName = "Hands and Feet",
            description = "Increases extra dish odds and cap: 50% +1, 25% +2, 10% +3, 5% +4.",
            loreDescription = "",
            cost = 1800f,
            requires = new List<string> { "wb_chance1" }
        });

        nodes.Add(new SinkNode
        {
            id = "wb_hole3",
            branch = SinkType.WashBasin,
            tier = 4,
            displayName = "A Quarry",
            description = "Increase depth again. +2 more dishes.",
            loreDescription = "",
            cost = 5000f,
            requires = new List<string> { "wb_hole2" }
        });

        nodes.Add(new SinkNode
        {
            id = "wb_chance3",
            branch = SinkType.WashBasin,
            tier = 4,
            displayName = "The Perfect Soaker",
            description = "Guarantees +1 extra dish. Improves odds for more: 25% +2, 10% +3, 10% +4, 5% +5.",
            loreDescription = "",
            cost = 5000f,
            requires = new List<string> { "wb_chance2" }
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
            id = "dw_amount1",
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
            id = "dw_time1",
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
            id = "dw_technique",
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
            id = "dw_amount2",
            branch = SinkType.Dishwasher,
            tier = 3,
            displayName = "Efficient Placement",
            description = "Increase dishes done per cycle by 100%.",
            loreDescription = "",
            cost = 1800f,
            requires = new List<string> { "dw_amount1" }
        });

        nodes.Add(new SinkNode
        {
            id = "dw_time2",
            branch = SinkType.Dishwasher,
            tier = 3,
            displayName = "Increase Water Pressure",
            description = "Decrease rinse cycle by an additional minute.",
            loreDescription = "",
            cost = 1800f,
            requires = new List<string> { "dw_time1" }
        });

        nodes.Add(new SinkNode
        {
            id = "dw_amount3",
            branch = SinkType.Dishwasher,
            tier = 4,
            displayName = "XL Dishwasher",
            description = "Increase dishes done per cycle by 150%.",
            loreDescription = "",
            cost = 5000f,
            requires = new List<string> { "dw_amount2" }
        });

        nodes.Add(new SinkNode
        {
            id = "dw_time3",
            branch = SinkType.Dishwasher,
            tier = 4,
            displayName = "Turbo Dishwasher",
            description = "Decrease rinse cycle by an additional minute.",
            loreDescription = "",
            cost = 5000f,
            requires = new List<string> { "dw_time2" }
        });
    }
}
