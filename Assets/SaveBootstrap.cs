using UnityEngine;

public static class SaveBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureSaveManager()
    {
        if (Object.FindAnyObjectByType<SaveManager>() != null) return;

        var go = new GameObject("SaveManager");
        go.AddComponent<SaveManager>();
    }
}
