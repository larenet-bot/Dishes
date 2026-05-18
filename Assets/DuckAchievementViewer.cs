//Assets/DuckAchievementViewer.cs
using System.Collections.Generic;
using UnityEngine;

public class DuckAchievementViewer : MonoBehaviour
{
    [Header("References")]
    public GameObject achievementWindow;
    public Transform contentParent;

    [Header("Prefabs")]
    public AchievementUIEntry entryPrefab;

    private bool built = false;

    public void ToggleAchievements()
    {
        if (achievementWindow == null)
            return;

        bool newState = !achievementWindow.activeSelf;

        achievementWindow.SetActive(newState);

        if (newState && !built)
        {
            BuildList();
        }
    }

    private void BuildList()
    {
        built = true;

        if (AchievementManager.Instance == null)
            return;

        List<AchievementData> achievements =
            AchievementManager.Instance.GetAllAchievements();

        foreach (AchievementData achievement in achievements)
        {
            if (achievement == null)
                continue;

            AchievementUIEntry entry =
                Instantiate(entryPrefab, contentParent);

            bool unlocked =
                AchievementManager.Instance.IsUnlocked(achievement.id);

            entry.Setup(achievement, unlocked);
        }
    }

    public void RefreshList()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        built = false;

        BuildList();
    }
}