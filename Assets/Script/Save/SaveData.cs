using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public string currentSceneName;

    public int forestNegative;
    public int oceanNegative;
    public int mineNegative;

    public bool hasPendingEnding;
    public string endingTitle;
    public string endingDescription;
    public int totalNegative;
    public bool isDisasterEnding;

    public List<ResourceSaveData> resources = new List<ResourceSaveData>();

    public List<SpawnedObjectSaveData> spawnedObjects = new List<SpawnedObjectSaveData>();
}

[System.Serializable]
public class ResourceSaveData
{
    public string resourceName;
    public int amount;
}

[System.Serializable]
public class SpawnedObjectSaveData
{
    public string prefabId;

    public float posX;
    public float posY;
    public float posZ;

    public float rotX;
    public float rotY;
    public float rotZ;
    public float rotW;

    public float scaleX;
    public float scaleY;
    public float scaleZ;
}