using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    private readonly Dictionary<string, int> resources = new Dictionary<string, int>();
    private bool suppressAutoSave;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("ResourceManager created: " + gameObject.name);
        }
        else
        {
            Debug.LogWarning("Duplicate ResourceManager destroyed: " + gameObject.name);
            Destroy(gameObject);
        }
    }

    public void AddResource(string resourceName, int amount)
    {
        if (string.IsNullOrEmpty(resourceName) || amount == 0) return;

        if (!resources.ContainsKey(resourceName))
            resources[resourceName] = 0;

        resources[resourceName] += amount;

        Debug.Log("AddResource -> " + resourceName + " = " + resources[resourceName]);
        AutoSaveIfAllowed();
    }

    public bool ConsumeResource(string resourceName, int amount)
    {
        if (string.IsNullOrEmpty(resourceName) || amount <= 0) return false;

        int currentAmount = GetResource(resourceName);
        if (currentAmount < amount)
        {
            Debug.LogWarning("Resource not enough: " + resourceName);
            return false;
        }

        resources[resourceName] = currentAmount - amount;
        Debug.Log("ConsumeResource -> " + resourceName + " = " + resources[resourceName]);
        AutoSaveIfAllowed();

        return true;
    }

    public int GetResource(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return 0;
        return resources.TryGetValue(resourceName, out int value) ? value : 0;
    }

    public void ResetAllResources()
    {
        resources.Clear();
        Debug.Log("All resources reset");
    }

    public void ReplaceAllResources(Dictionary<string, int> newResources, bool saveAfterReplace = false)
    {
        resources.Clear();

        if (newResources != null)
        {
            foreach (var pair in newResources)
            {
                if (string.IsNullOrEmpty(pair.Key)) continue;
                resources[pair.Key] = Mathf.Max(0, pair.Value);
            }
        }

        if (saveAfterReplace)
            AutoSaveIfAllowed();
    }

    public Dictionary<string, int> GetAllResources()
    {
        return new Dictionary<string, int>(resources);
    }

    public void SetAutoSaveSuppressed(bool suppressed)
    {
        suppressAutoSave = suppressed;
    }

    private void AutoSaveIfAllowed()
    {
        if (suppressAutoSave) return;

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }
}
