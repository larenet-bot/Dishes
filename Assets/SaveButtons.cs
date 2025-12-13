using UnityEngine;

public class SaveButtons : MonoBehaviour
{
    public void WipeSave()
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.WipeSave();
    }

    public void SaveNow()
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.Save();
    }

    public void LoadNow()
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.Load();
    }
}
