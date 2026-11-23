using System;
using System.Collections.Generic;

/// <summary>Added to KitchenSaveData; one independent load/cycle per kitchen.</summary>
[Serializable]
public class DishwasherSaveState
{
    public bool hasPile = false;
    public bool cycleRunning = false;
    public float secondsRemaining = 0f;
    public float cycleDuration = 0f;
    public int completedCycles = 0;
    public List<DishwasherSavedDish> dishes = new List<DishwasherSavedDish>();
}

[Serializable]
public class DishwasherSavedDish
{
    // The asset name prevents a reordered DishSpawner pool from changing the dish.
    public string dishAssetName;
    public int fallbackPoolIndex = -1;
    public bool inDishwasher;
    // Coordinates relative to the center of the dishwasher or staging area.
    public float normalizedX;
    public float normalizedY;
    public int quarterTurns;
}
