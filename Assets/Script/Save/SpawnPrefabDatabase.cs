using System.Collections.Generic;
using UnityEngine;

public class SpawnPrefabDatabase : MonoBehaviour
{
    public static SpawnPrefabDatabase Instance;

    [System.Serializable]
    public class PrefabEntry
    {
        public string prefabId;
        public GameObject prefab;
    }

    public List<PrefabEntry> prefabs = new List<PrefabEntry>();

    private Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();

    private void Awake()
    {
        Instance = this;

        prefabDict.Clear();

        foreach (var entry in prefabs)
        {
            if (!string.IsNullOrEmpty(entry.prefabId) && entry.prefab != null)
            {
                prefabDict[entry.prefabId] = entry.prefab;
            }
        }
    }

    public GameObject GetPrefab(string prefabId)
    {
        if (prefabDict.ContainsKey(prefabId))
            return prefabDict[prefabId];

        return null;
    }
}