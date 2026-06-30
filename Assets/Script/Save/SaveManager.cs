using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();

        data.currentSceneName = SceneManager.GetActiveScene().name;

        SaveResources(data);
        SaveKarma(data);
        SaveEnding(data);
        SaveSpawnedObjectsWithSceneCheck(data);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        Debug.Log("Game saved: " + SavePath);
    }

    private void SaveSpawnedObjectsWithSceneCheck(SaveData data)
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "MR_Main")
        {
            SaveSpawnedObjects(data);
            Debug.Log("Saved MR_Main spawned objects.");
        }
        else
        {
            PreserveOldSpawnedObjects(data);
            Debug.Log("Preserved MR_Main spawned objects while saving from another scene.");
        }
    }

    private void PreserveOldSpawnedObjects(SaveData data)
    {
        if (!File.Exists(SavePath)) return;

        string oldJson = File.ReadAllText(SavePath);
        SaveData oldData = JsonUtility.FromJson<SaveData>(oldJson);

        if (oldData != null && oldData.spawnedObjects != null)
            data.spawnedObjects = oldData.spawnedObjects;
    }

    private void SaveResources(SaveData data)
    {
        if (ResourceManager.Instance == null) return;

        Dictionary<string, int> allResources = ResourceManager.Instance.GetAllResources();

        foreach (var pair in allResources)
        {
            data.resources.Add(new ResourceSaveData
            {
                resourceName = pair.Key,
                amount = pair.Value
            });
        }
    }

    private void SaveToolDurabilities(SaveData data)
    {
        if (ResourceManager.Instance == null) return;

        Dictionary<string, int> allDurabilities = ResourceManager.Instance.GetAllToolDurabilities();

        foreach (var pair in allDurabilities)
        {
            data.toolDurabilities.Add(new ToolDurabilitySaveData
            {
                toolName = pair.Key,
                durability = pair.Value
            });
        }
    }

    private void SaveKarma(SaveData data)
    {
        if (KarmaSystem.Instance == null) return;

        data.forestNegative = KarmaSystem.Instance.forestNegative;
        data.oceanNegative = KarmaSystem.Instance.oceanNegative;
        data.mineNegative = KarmaSystem.Instance.mineNegative;
    }

    private void SaveEnding(SaveData data)
    {
        if (GameEndingState.Instance == null) return;

        data.hasPendingEnding = GameEndingState.Instance.hasPendingEnding;
        data.endingTitle = GameEndingState.Instance.endingTitle;
        data.endingDescription = GameEndingState.Instance.endingDescription;
        data.totalNegative = GameEndingState.Instance.totalNegative;
        data.isDisasterEnding = GameEndingState.Instance.isDisasterEnding;
    }

    private void SaveSpawnedObjects(SaveData data)
    {
        SpawnedObjectRecord[] objects =
            FindObjectsByType<SpawnedObjectRecord>(FindObjectsSortMode.None);

        foreach (var obj in objects)
        {
            if (string.IsNullOrEmpty(obj.prefabId)) continue;

            Transform t = obj.transform;

            data.spawnedObjects.Add(new SpawnedObjectSaveData
            {
                prefabId = obj.prefabId,

                posX = t.position.x,
                posY = t.position.y,
                posZ = t.position.z,

                rotX = t.rotation.x,
                rotY = t.rotation.y,
                rotZ = t.rotation.z,
                rotW = t.rotation.w,

                scaleX = t.localScale.x,
                scaleY = t.localScale.y,
                scaleZ = t.localScale.z
            });
        }
    }

    public void LoadGame()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("LoadGame can only run in Play Mode.");
            return;
        }

        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("No save file found.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        StartCoroutine(LoadSceneAndApplyData(data));
    }

    private IEnumerator LoadSceneAndApplyData(SaveData data)
    {
        yield return SceneManager.LoadSceneAsync(data.currentSceneName);
        yield return null;

        LoadResources(data);
        LoadKarma(data);
        LoadEnding(data);
        LoadSpawnedObjects(data);

        Debug.Log("Game loaded.");
    }

    private void LoadResources(SaveData data)
    {
        if (ResourceManager.Instance == null) return;

        Dictionary<string, int> loadedResources = new Dictionary<string, int>();

        if (data.resources != null)
        {
            foreach (var item in data.resources)
            {
                if (item == null || string.IsNullOrEmpty(item.resourceName)) continue;
                loadedResources[item.resourceName] = item.amount;
            }
        }

        ResourceManager.Instance.ReplaceAllResources(loadedResources);
    }

    private void LoadToolDurabilities(SaveData data)
    {
        if (ResourceManager.Instance == null || data.toolDurabilities == null) return;

        foreach (var item in data.toolDurabilities)
        {
            if (item == null || string.IsNullOrEmpty(item.toolName)) continue;
            ResourceManager.Instance.SetToolDurability(item.toolName, item.durability);
        }
    }

    private void LoadKarma(SaveData data)
    {
        if (KarmaSystem.Instance == null) return;

        KarmaSystem.Instance.forestNegative = data.forestNegative;
        KarmaSystem.Instance.oceanNegative = data.oceanNegative;
        KarmaSystem.Instance.mineNegative = data.mineNegative;
    }

    private void LoadEnding(SaveData data)
    {
        if (GameEndingState.Instance == null) return;

        if (data.hasPendingEnding)
        {
            GameEndingState.Instance.SetEndingData(
                data.endingTitle,
                data.endingDescription,
                data.totalNegative,
                data.isDisasterEnding
            );
        }
        else
        {
            GameEndingState.Instance.ClearEndingData();
        }
    }

    public void LoadSpawnedObjectsOnly()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("No save file found, skipping MR spawned object loading.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        LoadSpawnedObjects(data);

        Debug.Log("MR spawned objects loaded.");
    }

    public void LoadSpawnedObjects(SaveData data)
    {
        if (data == null || data.spawnedObjects == null) return;

        SpawnedObjectRecord[] oldObjects =
            FindObjectsByType<SpawnedObjectRecord>(FindObjectsSortMode.None);

        foreach (var oldObj in oldObjects)
            Destroy(oldObj.gameObject);

        if (SpawnPrefabDatabase.Instance == null)
            Debug.LogWarning("SpawnPrefabDatabase not found, falling back to Resources for spawned objects.");

        foreach (var objectData in data.spawnedObjects)
        {
            GameObject prefab = SpawnPrefabDatabase.Instance != null
                ? SpawnPrefabDatabase.Instance.GetPrefab(objectData.prefabId)
                : null;

            if (prefab == null)
                prefab = Resources.Load<GameObject>("MR/" + objectData.prefabId);

            if (prefab == null && objectData.prefabId == "Sprout")
                prefab = Resources.Load<GameObject>("MR/sprout 1");

            if (prefab == null)
            {
                Debug.LogWarning("Missing prefab for saved object: " + objectData.prefabId);
                continue;
            }

            Vector3 position = new Vector3(objectData.posX, objectData.posY, objectData.posZ);
            Quaternion rotation = new Quaternion(
                objectData.rotX,
                objectData.rotY,
                objectData.rotZ,
                objectData.rotW
            );
            Vector3 scale = new Vector3(
                objectData.scaleX,
                objectData.scaleY,
                objectData.scaleZ
            );

            GameObject obj = Instantiate(prefab, position, rotation);
            obj.transform.localScale = scale;

            SpawnedObjectRecord record = obj.GetComponent<SpawnedObjectRecord>();
            if (record == null)
                record = obj.AddComponent<SpawnedObjectRecord>();

            record.prefabId = objectData.prefabId;
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("Save file deleted.");
        }
    }

    [ContextMenu("Test Save")]
    public void TestSave()
    {
        SaveGame();
    }

    [ContextMenu("Test Load")]
    public void TestLoad()
    {
        LoadGame();
    }

    public bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }
}
