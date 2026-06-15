using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    private Dictionary<string, int> resources = new Dictionary<string, int>();

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

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
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

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        return true;
    }

    public int GetResource(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return 0;

        int value = 0;
        if (resources.ContainsKey(resourceName))
            value = resources[resourceName];

        Debug.Log("GetResource -> " + resourceName + " = " + value);
        return value;
    }

    public void ResetAllResources()
    {
        resources.Clear();
        Debug.Log("All resources reset");
    }

    public Dictionary<string, int> GetAllResources()
    {
        return new Dictionary<string, int>(resources);
    }
}
