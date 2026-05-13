using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerPrefs-backed business progression helper.
/// Stores cross-kitchen unlock/discovery flags and generic minigame cooldown end times.
/// Cooldowns use real UTC time, so they keep counting while scenes are unloaded.
/// </summary>
public static class KitchenBusinessProgress
{
    private const string OtherBusinessesUnlockedKey = "OTHER_BUSINESSES_UNLOCKED";
    private const string KnownKitchenIdsKey = "KITCHEN_PROGRESS_KNOWN_IDS";

    private static readonly DateTime UnixEpoch = new DateTime(
        1970,
        1,
        1,
        0,
        0,
        0,
        DateTimeKind.Utc
    );

    private static readonly long SessionStartUnixSeconds = GetUtcNowUnixSeconds();
    private static readonly HashSet<string> InitialCooldownsSeededThisSession = new HashSet<string>();

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

    private static string KnownMinigameCooldownIdsKey(string kitchenId)
    {
        return $"KITCHEN_MINIGAME_COOLDOWN_IDS_{kitchenId}";
    }

    private static string MinigameCooldownEndKey(string kitchenId, string minigameId)
    {
        return $"KITCHEN_MINIGAME_COOLDOWN_END_{kitchenId}_{minigameId}";
    }

    private static string MinigameCooldownDurationKey(string kitchenId, string minigameId)
    {
        return $"KITCHEN_MINIGAME_COOLDOWN_DURATION_{kitchenId}_{minigameId}";
    }

    private static string MinigameSessionKey(string kitchenId, string minigameId)
    {
        return $"{kitchenId}|{minigameId}";
    }

    private static long GetUtcNowUnixSeconds()
    {
        return (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
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

    private static void RegisterMinigameCooldownId(string kitchenId, string minigameId)
    {
        if (string.IsNullOrWhiteSpace(kitchenId) || string.IsNullOrWhiteSpace(minigameId))
        {
            return;
        }

        RegisterKitchenId(kitchenId);

        HashSet<string> ids = GetKnownMinigameCooldownIds(kitchenId);
        if (ids.Add(minigameId))
        {
            PlayerPrefs.SetString(KnownMinigameCooldownIdsKey(kitchenId), string.Join("|", ids));
        }
    }

    private static HashSet<string> GetKnownMinigameCooldownIds(string kitchenId)
    {
        HashSet<string> ids = new HashSet<string>();
        string raw = PlayerPrefs.GetString(KnownMinigameCooldownIdsKey(kitchenId), string.Empty);

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

    public static void EnsureInitialMinigameCooldownStartedThisSession(
        string kitchenId,
        string minigameId,
        float cooldownSeconds)
    {
        if (string.IsNullOrWhiteSpace(kitchenId) || string.IsNullOrWhiteSpace(minigameId))
        {
            return;
        }

        string sessionKey = MinigameSessionKey(kitchenId, minigameId);
        if (InitialCooldownsSeededThisSession.Contains(sessionKey))
        {
            return;
        }

        InitialCooldownsSeededThisSession.Add(sessionKey);
        RegisterMinigameCooldownId(kitchenId, minigameId);

        if (IsMinigameOnCooldown(kitchenId, minigameId))
        {
            return;
        }

        float duration = Mathf.Max(0.1f, cooldownSeconds);
        long endUnixSeconds = SessionStartUnixSeconds + Mathf.CeilToInt(duration);

        PlayerPrefs.SetFloat(MinigameCooldownDurationKey(kitchenId, minigameId), duration);
        PlayerPrefs.SetString(MinigameCooldownEndKey(kitchenId, minigameId), endUnixSeconds.ToString());
        PlayerPrefs.Save();
    }

    public static void StartMinigameCooldown(string kitchenId, string minigameId, float cooldownSeconds)
    {
        if (string.IsNullOrWhiteSpace(kitchenId) || string.IsNullOrWhiteSpace(minigameId))
        {
            return;
        }

        RegisterMinigameCooldownId(kitchenId, minigameId);

        float duration = Mathf.Max(0.1f, cooldownSeconds);
        long endUnixSeconds = GetUtcNowUnixSeconds() + Mathf.CeilToInt(duration);

        PlayerPrefs.SetFloat(MinigameCooldownDurationKey(kitchenId, minigameId), duration);
        PlayerPrefs.SetString(MinigameCooldownEndKey(kitchenId, minigameId), endUnixSeconds.ToString());
        PlayerPrefs.Save();
    }

    public static bool IsMinigameOnCooldown(string kitchenId, string minigameId)
    {
        return GetMinigameCooldownRemainingSeconds(kitchenId, minigameId) > 0f;
    }

    public static float GetMinigameCooldownRemainingSeconds(string kitchenId, string minigameId)
    {
        if (string.IsNullOrWhiteSpace(kitchenId) || string.IsNullOrWhiteSpace(minigameId))
        {
            return 0f;
        }

        string rawEnd = PlayerPrefs.GetString(MinigameCooldownEndKey(kitchenId, minigameId), string.Empty);
        if (string.IsNullOrWhiteSpace(rawEnd))
        {
            return 0f;
        }

        if (!long.TryParse(rawEnd, out long endUnixSeconds))
        {
            return 0f;
        }

        return Mathf.Max(0f, endUnixSeconds - GetUtcNowUnixSeconds());
    }

    public static float GetMinigameCooldownDurationSeconds(string kitchenId, string minigameId)
    {
        if (string.IsNullOrWhiteSpace(kitchenId) || string.IsNullOrWhiteSpace(minigameId))
        {
            return 0f;
        }

        return Mathf.Max(0f, PlayerPrefs.GetFloat(MinigameCooldownDurationKey(kitchenId, minigameId), 0f));
    }

    public static float GetMinigameCooldownProgress01(string kitchenId, string minigameId)
    {
        float duration = GetMinigameCooldownDurationSeconds(kitchenId, minigameId);
        if (duration <= 0.0001f)
        {
            return 1f;
        }

        float remaining = GetMinigameCooldownRemainingSeconds(kitchenId, minigameId);
        if (remaining <= 0f)
        {
            return 1f;
        }

        return 1f - Mathf.Clamp01(remaining / duration);
    }

    public static void ClearMinigameCooldown(string kitchenId, string minigameId)
    {
        if (string.IsNullOrWhiteSpace(kitchenId) || string.IsNullOrWhiteSpace(minigameId))
        {
            return;
        }

        PlayerPrefs.DeleteKey(MinigameCooldownEndKey(kitchenId, minigameId));
        PlayerPrefs.DeleteKey(MinigameCooldownDurationKey(kitchenId, minigameId));
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

            HashSet<string> minigameIds = GetKnownMinigameCooldownIds(id);
            foreach (string minigameId in minigameIds)
            {
                PlayerPrefs.DeleteKey(MinigameCooldownEndKey(id, minigameId));
                PlayerPrefs.DeleteKey(MinigameCooldownDurationKey(id, minigameId));
            }

            PlayerPrefs.DeleteKey(KnownMinigameCooldownIdsKey(id));
        }

        PlayerPrefs.DeleteKey(OtherBusinessesUnlockedKey);
        PlayerPrefs.DeleteKey(KnownKitchenIdsKey);
        PlayerPrefs.DeleteKey("LOAN_CURRENT_INDEX");
        PlayerPrefs.Save();
    }
}
