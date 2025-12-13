using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int currentLoanIndex;
    // ScoreManager
    public long totalDishes;
    public float totalProfit;
    public int dishCountIncrement;
    public float dishProfitMultiplier;

    // Upgrades
    public int currentSoapIndex;
    public int currentGloveIndex;
    public int currentSpongeIndex;

    // Employees
    public List<EmployeeSave> employees = new List<EmployeeSave>();
}

[Serializable]
public class EmployeeSave
{
    public int count;
    public float currentCost;
    public int currentUpgradeIndex;
    public float currentDebuff;
}
