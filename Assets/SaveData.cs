using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int saveVersion = 2;

    // New multi-kitchen save container.
    public List<KitchenSaveData> kitchens = new List<KitchenSaveData>();

    // ---- Legacy single-kitchen fields ----
    // These are kept so old save.json files can be migrated into kitchen_1.
    public long totalDishes;
    public float totalProfit;
    public int dishCountIncrement = 1;
    public float dishProfitMultiplier = 1f;

    public int currentSoapIndex;
    public int currentGloveIndex;
    public int currentSpongeIndex;
    public bool radioOwned;

    public List<EmployeeSave> employees = new List<EmployeeSave>();
    public float employeeProfitMultiplier = 1f;

    public int currentSinkType;
    public List<string> purchasedSinkNodeIds = new List<string>();

    public int currentLoanIndex;
}

[Serializable]
public class KitchenSaveData
{
    public string kitchenId = "kitchen_1";

    // MoneyUI / score state.
    public long totalDishes;
    public float totalProfit;
    public int dishCountIncrement = 1;
    public float dishProfitMultiplier = 1f;

    // Shelf upgrades.
    public int currentSoapIndex;
    public int currentGloveIndex;
    public int currentSpongeIndex;
    public bool radioOwned;

    // Employees.
    public List<EmployeeSave> employees = new List<EmployeeSave>();
    public float employeeProfitMultiplier = 1f;

    // Sink upgrades / selected sink.
    public int currentSinkType;
    public List<string> purchasedSinkNodeIds = new List<string>();

    // Loans are also recorded here so the JSON file knows the kitchen's debt state.
    // The LoanManager still mirrors this to PlayerPrefs for the existing business-menu flow.
    public int currentLoanIndex;
}

[Serializable]
public class EmployeeSave
{
    public int count;
    public float currentCost;
    public int currentUpgradeIndex;
    public float currentDebuff;
}
