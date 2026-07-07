using UnityEngine;

public class PlantGrowthTimer : MonoBehaviour
{
    [Header("Growth")]
    public float growSeconds = 36f;
    public GameObject treePrefab;
    public string treePrefabId = "Tree";
    public string resourcesTreePath = "MR/Tree";
    public int treeRestorationValue = 10;

    private float elapsedSeconds;
    private bool grown;

    void Update()
    {
        if (grown) return;

        elapsedSeconds += Time.deltaTime;
        if (elapsedSeconds < growSeconds) return;

        GrowToTree();
    }

    void GrowToTree()
    {
        grown = true;

        GameObject prefab = treePrefab;
        if (prefab == null && !string.IsNullOrEmpty(resourcesTreePath))
            prefab = Resources.Load<GameObject>(resourcesTreePath);

        if (KarmaSystem.Instance != null)
            KarmaSystem.Instance.AddRestorationKarma(treeRestorationValue);

        if (prefab == null)
        {
            Debug.LogWarning("Tree prefab not found. Restoration was counted, but Sprout visual was kept.");
            enabled = false;
            return;
        }

        GameObject tree = Instantiate(prefab, transform.position, transform.rotation);

        SpawnedObjectRecord record = tree.GetComponent<SpawnedObjectRecord>();
        if (record == null)
            record = tree.AddComponent<SpawnedObjectRecord>();

        record.prefabId = treePrefabId;

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        Destroy(gameObject);
    }
}