using UnityEngine;

public class MRSeedPlanter : MonoBehaviour
{
    [Header("Sprout")]
    public GameObject sproutPrefab;
    public string sproutPrefabId = "Sprout";
    public string resourcesSproutPath = "MR/Sprout";

    [Header("Planting")]
    public float plantYOffset = 0.02f;
    public bool destroySeedAfterPlanting = true;
    public LayerMask groundLayerMask = ~0;
    public float groundRaycastDistance = 2f;
    public bool useFallbackGroundY = true;
    public float fallbackGroundY = 0f;
    public float fallbackPlantDepth = 0.05f;

    private bool planted;
    private bool armed;
    private Vector3 lastDropPosition;

    public void Configure(GameObject prefab, string prefabId)
    {
        sproutPrefab = prefab;

        if (!string.IsNullOrEmpty(prefabId))
            sproutPrefabId = prefabId;
    }

    public void Arm()
    {
        armed = true;
        lastDropPosition = transform.position;
    }

    private void Update()
    {
        if (!armed || planted) return;

        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundRaycastDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            if (transform.position.y <= hit.point.y + plantYOffset)
            {
                Plant(hit.point);
            }

            return;
        }

        if (useFallbackGroundY && transform.position.y <= fallbackGroundY - fallbackPlantDepth)
        {
            Vector3 fallbackPosition = new Vector3(transform.position.x, fallbackGroundY, transform.position.z);

            if (lastDropPosition != Vector3.zero)
                fallbackPosition = new Vector3(lastDropPosition.x, fallbackGroundY, lastDropPosition.z);

            Plant(fallbackPosition);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!armed || planted) return;
        PlantAtCollision(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!armed || planted) return;
        Plant(transform.position);
    }

    void PlantAtCollision(Collision collision)
    {
        Vector3 position = transform.position;

        if (collision != null && collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);
            position = contact.point;
        }

        Plant(position);
    }

    void Plant(Vector3 position)
    {
        planted = true;

        GameObject prefab = sproutPrefab;
        if (prefab == null && !string.IsNullOrEmpty(resourcesSproutPath))
            prefab = Resources.Load<GameObject>(resourcesSproutPath);

        if (prefab == null)
        {
            Debug.LogWarning("Sprout prefab not found, cannot plant seed");
            planted = false;
            return;
        }

        Vector3 spawnPosition = position + Vector3.up * plantYOffset;
        Quaternion spawnRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        GameObject sprout = Instantiate(prefab, spawnPosition, spawnRotation);

        SpawnedObjectRecord record = sprout.GetComponent<SpawnedObjectRecord>();
        if (record == null)
            record = sprout.AddComponent<SpawnedObjectRecord>();

        record.prefabId = sproutPrefabId;

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        if (destroySeedAfterPlanting)
            Destroy(gameObject);
        else
            enabled = false;
    }
}
