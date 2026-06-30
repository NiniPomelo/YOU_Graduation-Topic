using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    [Serializable]
    public class ResourceCost
    {
        public string resourceName;
        public int amount;

        public ResourceCost(string resourceName, int amount)
        {
            this.resourceName = resourceName;
            this.amount = amount;
        }
    }

    [Serializable]
    public class CraftRecipe
    {
        public string itemName;
        public ResourceCost[] costs;

        public CraftRecipe(string itemName, ResourceCost[] costs)
        {
            this.itemName = itemName;
            this.costs = costs;
        }
    }

    [Header("Starter Tools")]
    public bool grantStarterTools = true;
    public string starterAxeName = "Axe";
    public string starterPickName = "Pick";

    [Header("Tool Durability")]
    public int axeMaxDurability = 100;
    public int pickMaxDurability = 100;

    private readonly Dictionary<string, int> resources = new Dictionary<string, int>();
    private readonly Dictionary<string, int> toolDurability = new Dictionary<string, int>();
    private bool suppressAutoSave;
    private CraftRecipe[] defaultRecipes;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDefaultRecipes();
            GrantStarterTools();
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
        EnsureToolDurability(resourceName);

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

    public int GetAvailableCraftCount(string itemName)
    {
        int ownedAmount = GetResource(itemName);
        CraftRecipe recipe = GetRecipe(itemName);

        if (recipe == null)
            return ownedAmount;

        return ownedAmount + GetCraftableAmount(recipe);
    }

    public bool TryPrepareItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return false;

        if (GetResource(itemName) > 0)
            return true;

        CraftRecipe recipe = GetRecipe(itemName);
        if (recipe == null)
            return false;

        if (GetCraftableAmount(recipe) <= 0)
            return false;

        for (int i = 0; i < recipe.costs.Length; i++)
        {
            ResourceCost cost = recipe.costs[i];
            if (cost == null) continue;

            resources[cost.resourceName] = GetResource(cost.resourceName) - cost.amount;
        }

        if (IsTool(itemName))
        {
            if (!resources.ContainsKey(itemName))
                resources[itemName] = 0;

            resources[itemName] += 1;
            toolDurability[itemName] = GetMaxDurability(itemName);
        }

        Debug.Log("Crafted item -> " + itemName);
        AutoSaveIfAllowed();

        return true;
    }

    public bool UseTool(string toolName, int damage)
    {
        if (!IsTool(toolName) || damage <= 0) return true;
        if (GetResource(toolName) <= 0) return false;

        EnsureToolDurability(toolName);
        toolDurability[toolName] -= damage;

        if (toolDurability[toolName] > 0)
        {
            AutoSaveIfAllowed();
            return true;
        }

        resources[toolName] = Mathf.Max(0, GetResource(toolName) - 1);

        if (resources[toolName] > 0)
            toolDurability[toolName] = GetMaxDurability(toolName);
        else
            toolDurability.Remove(toolName);

        Debug.Log(toolName + " broke");
        AutoSaveIfAllowed();

        return false;
    }

    public int GetToolDurability(string toolName)
    {
        if (!IsTool(toolName) || GetResource(toolName) <= 0) return 0;

        EnsureToolDurability(toolName);
        return toolDurability[toolName];
    }

    public int GetResource(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return 0;
        return resources.TryGetValue(resourceName, out int value) ? value : 0;
    }

    public void ResetAllResources()
    {
        resources.Clear();
        toolDurability.Clear();
        Debug.Log("All resources reset");
    }

    public void ReplaceAllResources(Dictionary<string, int> newResources, bool saveAfterReplace = false)
    {
        resources.Clear();
        toolDurability.Clear();

        if (newResources != null)
        {
            foreach (var pair in newResources)
            {
                if (string.IsNullOrEmpty(pair.Key)) continue;
                resources[pair.Key] = Mathf.Max(0, pair.Value);
                EnsureToolDurability(pair.Key);
            }
        }

        GrantStarterTools();

        if (saveAfterReplace)
            AutoSaveIfAllowed();
    }

    public Dictionary<string, int> GetAllResources()
    {
        return new Dictionary<string, int>(resources);
    }

    public Dictionary<string, int> GetAllToolDurabilities()
    {
        return new Dictionary<string, int>(toolDurability);
    }

    public void SetToolDurability(string toolName, int durability)
    {
        if (!IsTool(toolName) || GetResource(toolName) <= 0) return;

        toolDurability[toolName] = Mathf.Clamp(durability, 1, GetMaxDurability(toolName));
    }

    public CraftRecipe GetRecipe(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;
        if (defaultRecipes == null) InitializeDefaultRecipes();

        for (int i = 0; i < defaultRecipes.Length; i++)
        {
            if (defaultRecipes[i] != null && defaultRecipes[i].itemName == itemName)
                return defaultRecipes[i];
        }

        return null;
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

    private void InitializeDefaultRecipes()
    {
        defaultRecipes = new CraftRecipe[]
        {
            new CraftRecipe("House", new ResourceCost[]
            {
                new ResourceCost("Wood", 8),
                new ResourceCost("Sand", 4),
                new ResourceCost("Limestone", 5)
            }),
            new CraftRecipe("Factory", new ResourceCost[]
            {
                new ResourceCost("IronOre", 8),
                new ResourceCost("Oil", 4),
                new ResourceCost("Gas", 3),
                new ResourceCost("Limestone", 6)
            }),
            new CraftRecipe("Axe", new ResourceCost[]
            {
                new ResourceCost("Wood", 2),
                new ResourceCost("IronOre", 1)
            }),
            new CraftRecipe("Pick", new ResourceCost[]
            {
                new ResourceCost("Wood", 2),
                new ResourceCost("IronOre", 3)
            })
        };
    }

    private int GetCraftableAmount(CraftRecipe recipe)
    {
        if (recipe == null || recipe.costs == null || recipe.costs.Length == 0)
            return 0;

        int craftableAmount = int.MaxValue;

        for (int i = 0; i < recipe.costs.Length; i++)
        {
            ResourceCost cost = recipe.costs[i];
            if (cost == null || string.IsNullOrEmpty(cost.resourceName) || cost.amount <= 0)
                continue;

            craftableAmount = Mathf.Min(craftableAmount, GetResource(cost.resourceName) / cost.amount);
        }

        return craftableAmount == int.MaxValue ? 0 : craftableAmount;
    }

    private void GrantStarterTools()
    {
        if (!grantStarterTools) return;

        GrantStarterTool(starterAxeName);
        GrantStarterTool(starterPickName);
    }

    private void GrantStarterTool(string toolName)
    {
        if (string.IsNullOrEmpty(toolName)) return;

        if (!resources.ContainsKey(toolName))
            resources[toolName] = 1;

        EnsureToolDurability(toolName);
    }

    private void EnsureToolDurability(string toolName)
    {
        if (!IsTool(toolName) || GetResource(toolName) <= 0) return;

        if (!toolDurability.ContainsKey(toolName))
            toolDurability[toolName] = GetMaxDurability(toolName);
    }

    private bool IsTool(string resourceName)
    {
        return resourceName == starterAxeName || resourceName == starterPickName;
    }

    private int GetMaxDurability(string toolName)
    {
        if (toolName == starterAxeName) return axeMaxDurability;
        if (toolName == starterPickName) return pickMaxDurability;

        return 100;
    }
}