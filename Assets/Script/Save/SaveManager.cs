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

        Debug.Log("�C���w�s�ɡG" + SavePath);
    }

    void SaveSpawnedObjectsWithSceneCheck(SaveData data)
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "MR_Main")
        {
            SaveSpawnedObjects(data);
            Debug.Log("�ثe�b MR_Main�A��s MR �ͦ�����s��");
        }
        else
        {
            PreserveOldSpawnedObjects(data);
            Debug.Log("�ثe���b MR_Main�A�O�d�ª� MR �ͦ�������");
        }
    }

    void PreserveOldSpawnedObjects(SaveData data)
    {
        if (!File.Exists(SavePath)) return;

        string oldJson = File.ReadAllText(SavePath);
        SaveData oldData = JsonUtility.FromJson<SaveData>(oldJson);

        if (oldData != null && oldData.spawnedObjects != null)
        {
            data.spawnedObjects = oldData.spawnedObjects;
        }
    }
    void SaveResources(SaveData data)
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

    void SaveKarma(SaveData data)
    {
        if (KarmaSystem.Instance == null) return;

        data.forestNegative = KarmaSystem.Instance.forestNegative;
        data.oceanNegative = KarmaSystem.Instance.oceanNegative;
        data.mineNegative = KarmaSystem.Instance.mineNegative;
    }

    void SaveEnding(SaveData data)
    {
        if (GameEndingState.Instance == null) return;

        data.hasPendingEnding = GameEndingState.Instance.hasPendingEnding;
        data.endingTitle = GameEndingState.Instance.endingTitle;
        data.endingDescription = GameEndingState.Instance.endingDescription;
        data.totalNegative = GameEndingState.Instance.totalNegative;
        data.isDisasterEnding = GameEndingState.Instance.isDisasterEnding;
    }

    void SaveSpawnedObjects(SaveData data)
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
            Debug.LogWarning("Ū�ɥu��b Play Mode ����A�Х��� Play�C");
            return;
        }

        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("�S�����s��");
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

        Debug.Log("�C��Ū�ɧ���");
    }

    void LoadResources(SaveData data)
    {
        if (ResourceManager.Instance == null) return;

        ResourceManager.Instance.ResetAllResources();

        foreach (var item in data.resources)
        {
            ResourceManager.Instance.AddResource(item.resourceName, item.amount);
        }
    }

    void LoadKarma(SaveData data)
    {
        if (KarmaSystem.Instance == null) return;

        KarmaSystem.Instance.forestNegative = data.forestNegative;
        KarmaSystem.Instance.oceanNegative = data.oceanNegative;
        KarmaSystem.Instance.mineNegative = data.mineNegative;
    }

    void LoadEnding(SaveData data)
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
            Debug.Log("�S���s�ɡA�ҥH�����J MR �ͦ�����");
            return;
        }

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        LoadSpawnedObjects(data);

        Debug.Log("MR �ͦ�����w�۰ʫ�_");
    }
    public void LoadSpawnedObjects(SaveData data)
    {
        SpawnedObjectRecord[] oldObjects =
            FindObjectsByType<SpawnedObjectRecord>(FindObjectsSortMode.None);

        foreach (var oldObj in oldObjects)
        {
            Destroy(oldObj.gameObject);
        }

        if (SpawnPrefabDatabase.Instance == null)
        {
            Debug.LogWarning("SpawnPrefabDatabase not found, falling back to Resources for spawned objects");
        }

        foreach (var objectData in data.spawnedObjects)
        {
            GameObject prefab = SpawnPrefabDatabase.Instance != null
                ? SpawnPrefabDatabase.Instance.GetPrefab(objectData.prefabId)
                : null;

            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("MR/" + objectData.prefabId);
            }

            if (prefab == null)
            {
                Debug.LogWarning("�䤣�� Prefab�G" + objectData.prefabId);
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
            {
                record = obj.AddComponent<SpawnedObjectRecord>();
            }

            record.prefabId = objectData.prefabId;
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("�s�ɤw�R��");
        }
    }

    [ContextMenu("���զs��")]
    public void TestSave()
    {
        SaveGame();
    }

    [ContextMenu("����Ū��")]
    public void TestLoad()
    {
        LoadGame();
    }

    public bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }
}