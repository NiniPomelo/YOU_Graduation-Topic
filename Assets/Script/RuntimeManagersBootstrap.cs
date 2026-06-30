using UnityEngine;

public static class RuntimeManagersBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureCoreManagers()
    {
        EnsureManager<ResourceManager>("ResourceManager");
        EnsureManager<SaveManager>("SaveManager");
        EnsureManager<KarmaSystem>("KarmaSystem");
        EnsureManager<GameEndingState>("GameEndingState");
    }

    private static void EnsureManager<T>(string objectName) where T : MonoBehaviour
    {
        if (Object.FindFirstObjectByType<T>() != null)
            return;

        GameObject managerObject = new GameObject(objectName);
        managerObject.AddComponent<T>();
        Object.DontDestroyOnLoad(managerObject);
    }
}
