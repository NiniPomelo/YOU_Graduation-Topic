using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public string currentSceneName;

    public bool hasTimerData;
    public float remainingTime;
    public bool timerIsRunning;

    public int forestNegative;
    public int oceanNegative;
    public int mineNegative;
    public int constructionKarma;
    public int timeKarma;
    public int restorationKarma;

    public int choppedTreeCount;
    public int oilExtractedCount;
    public int gasExtractedCount;
    public int houseCount;
    public int factoryCount;

    public bool hasPendingEnding;
    public string endingType;
    public string endingTitle;
    public string endingDescription;
    public string environmentalStage;
    public int totalNegative;
    public int totalBeforeRestoration;
    public int endingRestorationKarma;
    public float elapsedGameYears;
    public bool isDisasterEnding;

    public List<ResourceSaveData> resources = new List<ResourceSaveData>();
    public List<ToolDurabilitySaveData> toolDurabilities = new List<ToolDurabilitySaveData>();

    public List<SpawnedObjectSaveData> spawnedObjects = new List<SpawnedObjectSaveData>();
}

[System.Serializable]
public class ResourceSaveData
{
    public string resourceName;
    public int amount;
}

[System.Serializable]
public class ToolDurabilitySaveData
{
    public string toolName;
    public int durability;
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