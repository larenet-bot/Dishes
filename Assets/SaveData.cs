using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int saveVersion = 4;

    // Legacy single-kitchen fields. These are still mirrored from kitchen_1 so old debugging
    // tools or old fallback paths do not immediately break.
    public long totalDishes = 0;
    public float totalProfit = 0f;
    public int dishCountIncrement = 1;
    public float profitPerDish = 1f;
    public float dishProfitMultiplier = 1f;

    public int currentSoapIndex = 0;
    public int currentGloveIndex = 0;
    public int currentSpongeIndex = 0;
    public bool radioOwned = false;

    public List<EmployeeSave> employees = new List<EmployeeSave>();
    public float employeeProfitMultiplier = 1f;

    public int currentSinkType = 0;
    public List<string> purchasedSinkNodeIds = new List<string>();

    public int currentLoanIndex = 0;

    // Legacy mirror of background earnings fields for kitchen_1.
    public float cachedMoneyPerSecond = 0f;
    public float cachedDishesPerSecond = 0f;
    public long lastBackgroundEarningsUnixSeconds = 0;
    public float backgroundDishFraction = 0f;

    public List<KitchenSaveData> kitchens = new List<KitchenSaveData>();
}

[Serializable]
public class KitchenSaveData
{
    public string kitchenId = "kitchen_1";

    public long totalDishes = 0;
    public float totalProfit = 0f;
    public int dishCountIncrement = 1;
    public float profitPerDish = 1f;
    public float dishProfitMultiplier = 1f;

    public int currentSoapIndex = 0;
    public int currentGloveIndex = 0;
    public int currentSpongeIndex = 0;
    public bool radioOwned = false;

    public List<EmployeeSave> employees = new List<EmployeeSave>();
    public float employeeProfitMultiplier = 1f;

    public int currentSinkType = 0;
    public List<string> purchasedSinkNodeIds = new List<string>();

    public int currentLoanIndex = 0;

    // These are the rates shown in the Other Businesses menu.
    // Offline and unloaded kitchens earn from this stored money/sec rate.
    public float cachedMoneyPerSecond = 0f;
    public float cachedDishesPerSecond = 0f;

    // Last time this kitchen's unloaded/offline production was applied.
    public long lastBackgroundEarningsUnixSeconds = 0;

    // Keeps fractional dish production between unloaded/offline earning ticks.
    public float backgroundDishFraction = 0f;

    // Achievements unlocked for this kitchen (persisted)
    public List<string> achievedAchievementIds = new List<string>();
}

[Serializable]
public class EmployeeSave
{
    public int count = 0;
    public float currentCost = 0f;
    public int currentUpgradeIndex = 0;
    public float currentDebuff = 0f;
}
