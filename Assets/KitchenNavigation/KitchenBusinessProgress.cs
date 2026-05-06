using UnityEngine;

/// <summary>
/// Small PlayerPrefs-backed progression helper for the business menu.
/// This only saves discovery/cutscene/business-unlock flags, not per-kitchen money.
/// </summary>
public static class KitchenBusinessProgress
{
    private const string OtherBusinessesUnlockedKey = "OTHER_BUSINESSES_UNLOCKED";

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
            return true;
        }

        if (string.IsNullOrEmpty(kitchenId))
        {
            return false;
        }

        return PlayerPrefs.GetInt(KitchenDiscoveredKey(kitchenId), 0) == 1;
    }

    public static void MarkKitchenDiscovered(string kitchenId)
    {
        if (string.IsNullOrEmpty(kitchenId))
        {
            return;
        }

        PlayerPrefs.SetInt(KitchenDiscoveredKey(kitchenId), 1);
        PlayerPrefs.Save();
    }

    public static bool HasSeenDiscoveryCutscene(string kitchenId)
    {
        if (string.IsNullOrEmpty(kitchenId))
        {
            return false;
        }

        return PlayerPrefs.GetInt(KitchenDiscoveryCutsceneSeenKey(kitchenId), 0) == 1;
    }

    public static void MarkDiscoveryCutsceneSeen(string kitchenId)
    {
        if (string.IsNullOrEmpty(kitchenId))
        {
            return;
        }

        PlayerPrefs.SetInt(KitchenDiscoveryCutsceneSeenKey(kitchenId), 1);
        PlayerPrefs.Save();
    }

    public static bool AreKitchenLoansPaid(string kitchenId)
    {
        if (string.IsNullOrEmpty(kitchenId))
        {
            return false;
        }

        return PlayerPrefs.GetInt(KitchenLoansPaidKey(kitchenId), 0) == 1;
    }

    public static void SetKitchenLoansPaid(string kitchenId, bool paid)
    {
        if (string.IsNullOrEmpty(kitchenId))
        {
            return;
        }

        PlayerPrefs.SetInt(KitchenLoansPaidKey(kitchenId), paid ? 1 : 0);
        PlayerPrefs.Save();
    }
}
