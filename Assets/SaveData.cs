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
    public bool radioOwned;            // NEW: whether the single-tier radio was purchased
    public int currentMp3Index; 

    // MP3 Player state 
    public List<string> mp3Queue = new List<string>();   // list of clip names in queue order
    public string mp3LoopedSongName = null;              // clip name set to loop (null = none)
    public bool mp3LoopEnabled = false;                  // whether looping is enabled

    // Employees
    public List<EmployeeSave> employees = new List<EmployeeSave>();

    //Cutscene
    public bool hasWatched = false;

    //Sink
    public int currentSinkType; // 0=Basic, 1=PowerWasher, 2=WashBasin, 3=Dishwasher
    public List<string> purchasedSinkNodeIds = new List<string>();
}

[Serializable]
public class EmployeeSave
{
    public int count;
    public float currentCost;
    public int currentUpgradeIndex;
    public float currentDebuff;
}
