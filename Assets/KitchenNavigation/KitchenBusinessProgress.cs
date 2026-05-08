using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerPrefs-backed business progression helper.
/// This stores cross-kitchen unlock/discovery flags, not kitchen money.
/// </summary>
public static class KitchenBusinessProgress
{
    private const string OtherBusinessesUnlockedKey = "OTHER_BUSINESSES_UNLOCKED";
    private const string KnownKitchenIdsKey = "KITCHEN_PROGRESS_KNOWN_IDS";

    private static string KitchenDiscoveredKey(string kitchenId)
    {
        return $"KITCHEN_DISCOVERED_{kitchenId}";
    }

    private static string KitchenDiscoveryCutsceneSeenKey(string kitchenId)
    {
        return $"KITCHEN_DISCOVERY_CUTSCENE_SEEN_{kitchenId}";
    }

    private static string KitchenLoansPaidKey(string kitchenId)
    {
        return $"KITCHEN_LOANS_PAID_{kitchenId}";
    }

    private static void RegisterKitchenId(string kitchenId)
    {
        if (string.IsNullOrWhiteSpace(kitchenId))
        {
            return;
        }

        HashSet<string> ids = GetKnownKitchenIds();
        if (ids.Add(kitchenId))
        {
            PlayerPrefs.SetString(KnownKitchenIdsKey, string.Join("|", ids));
        }
    }

    private static HashSet<string> GetKnownKitchenIds()
    {
        HashSet<string> ids = new HashSet<string>();
        string raw = PlayerPrefs.GetString(KnownKitchenIdsKey, string.Empty);

        if (string.IsNullOrWhiteSpace(raw))
        {
            return ids;
        }

        string[] split = raw.Split('|');
        for (int i = 0; i < split.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(split[i]))
            {
                ids.Add(split[i]);
            }
        }

        return ids;
    }

    public static bool AreOtherBusinessesUnlocked()
    {
        return PlayerPrefs.GetInt(OtherBusinessesUnlockedKey, 0) == 1;
    }

    public static void SetOtherBusinessesUnlocked(bool unlocked)
    {
        PlayerPrefs.SetInt(OtherBusinessesUnlockedKey, unlocked ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool IsKitchenDiscovered(string kitchenId, bool startsDiscovered = false)
    {
        if (startsDiscovered)
        {
            RegisterKitchenId(kitchenId);
            return true;
        }

        if (string.IsNullOrWhiteSpace(kitchenId))
        {
            return false;
        }

        RegisterKitchenId(kitchenId);
        return PlayerPrefs.GetInt(KitchenDiscoveredKey(kitchenId), 0) == 1;
    }

    public static void MarkKitchenDiscovered(string kitchenId)
    {
        if (string.IsNullOrWhiteSpace(kitchenId))
        {
            return;
        }

        RegisterKitchenId(kitchenId);
        PlayerPrefs.SetInt(KitchenDiscoveredKey(kitchenId), 1);
        PlayerPrefs.Save();
    }

    public static bool HasSeenDiscoveryCutscene(string kitchenId)
    {
        if (string.IsNullOrWhiteSpace(kitchenId))
        {
            return false;
        }

        RegisterKitchenId(kitchenId);
        return PlayerPrefs.GetInt(KitchenDiscoveryCutsceneSeenKey(kitchenId), 0) == 1;
    }

    public static void MarkDiscoveryCutsceneSeen(string kitchenId)
    {
        if (string.IsNullOrWhiteSpace(kitchenId))
        {
            return;
        }

        RegisterKitchenId(kitchenId);
        PlayerPrefs.SetInt(KitchenDiscoveryCutsceneSeenKey(kitchenId), 1);
        PlayerPrefs.Save();
    }

    public static bool AreKitchenLoansPaid(string kitchenId)
    {
        if (string.IsNullOrWhiteSpace(kitchenId))
        {
            return false;
        }

        RegisterKitchenId(kitchenId);
        return PlayerPrefs.GetInt(KitchenLoansPaidKey(kitchenId), 0) == 1;
    }

    public static void SetKitchenLoansPaid(string kitchenId, bool paid)
    {
        if (string.IsNullOrWhiteSpace(kitchenId))
        {
            return;
        }

        RegisterKitchenId(kitchenId);
        PlayerPrefs.SetInt(KitchenLoansPaidKey(kitchenId), paid ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void ClearAllBusinessProgress()
    {
        HashSet<string> ids = GetKnownKitchenIds();

        foreach (string id in ids)
        {
            PlayerPrefs.DeleteKey(KitchenDiscoveredKey(id));
            PlayerPrefs.DeleteKey(KitchenDiscoveryCutsceneSeenKey(id));
            PlayerPrefs.DeleteKey(KitchenLoansPaidKey(id));
            PlayerPrefs.DeleteKey($"LOAN_CURRENT_INDEX_{id}");
        }

        PlayerPrefs.DeleteKey(OtherBusinessesUnlockedKey);
        PlayerPrefs.DeleteKey(KnownKitchenIdsKey);
        PlayerPrefs.DeleteKey("LOAN_CURRENT_INDEX");
        PlayerPrefs.Save();
    }
}
