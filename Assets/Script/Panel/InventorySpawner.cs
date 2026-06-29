using UnityEngine;
using UnityEngine.SceneManagement;

public class InventorySpawner : MonoBehaviour
{
    [Header("Right hand spawn anchor")]
    public Transform rightHandAnchor;

    [Header("Panel Controller")]
    public InventoryPanelController panelController;

    [Header("Held object local position")]
    public Vector3 spawnLocalOffset = new Vector3(0f, 0.05f, 0.25f);

    [Header("Held object local rotation")]
    public Vector3 spawnLocalEuler = Vector3.zero;

    [Header("Grip")]
    public float gripThreshold = 0.8f;
    public float releaseThreshold = 0.2f;
    private bool gripReady = true;

    [Header("Avoid Duplicate Spawn")]
    public bool onlyOneHeldObject = true;

    [Header("MR World Generation Restriction")]
    public string mrSceneName = "MR_Main";
    public int mrWorldSectionIndex = 0;

    [Header("MR Planting")]
    public string seedResourceName = "Seed";
    public GameObject sproutPrefab;
    public string sproutPrefabId = "Sprout";

    private GameObject currentSpawnedObject;
    private bool currentSpawnedIsTool = false;

    void Start()
    {
        if (panelController == null)
            panelController = FindFirstObjectByType<InventoryPanelController>();

        if (rightHandAnchor == null)
        {
            GameObject rightHand = GameObject.Find("RightHandAnchor");
            if (rightHand != null)
                rightHandAnchor = rightHand.transform;
        }
    }

    void Update()
    {
        if (panelController == null || rightHandAnchor == null) return;

        float grip = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);

        if (grip > gripThreshold && gripReady)
        {
            gripReady = false;
            TrySpawnCurrentSlotItem();
        }
        else if (grip < releaseThreshold)
        {
            if (!gripReady)
                HandleGripRelease();

            gripReady = true;
        }
    }

    void TrySpawnCurrentSlotItem()
    {
        InventorySlotUI currentSlot = panelController.GetCurrentSlot();
        if (currentSlot == null)
        {
            Debug.LogWarning("No inventory slot is selected");
            return;
        }

        if (IsMrWorldGenerationBlocked(currentSlot))
            return;

        if (currentSlot.spawnPrefab == null)
        {
            Debug.LogWarning("Selected slot has no spawnPrefab: " + currentSlot.resourceName);
            return;
        }

        bool currentSlotIsSeed = IsSeedSlot(currentSlot);
        if (currentSlotIsSeed && !TryConsumeSeed(currentSlot.resourceName))
            return;

        if (onlyOneHeldObject && currentSpawnedObject != null)
        {
            Destroy(currentSpawnedObject);
            currentSpawnedObject = null;
        }

        Vector3 originalScale = currentSlot.spawnPrefab.transform.localScale;

        currentSpawnedObject = Instantiate(currentSlot.spawnPrefab);
        currentSpawnedObject.transform.SetParent(rightHandAnchor, false);
        currentSpawnedObject.transform.localPosition = spawnLocalOffset;
        currentSpawnedObject.transform.localRotation = Quaternion.Euler(spawnLocalEuler);
        currentSpawnedObject.transform.localScale = originalScale;

        currentSpawnedIsTool = currentSlot.isTool;

        if (currentSlotIsSeed)
        {
            MRSeedPlanter planter = currentSpawnedObject.GetComponent<MRSeedPlanter>();
            if (planter == null)
                planter = currentSpawnedObject.AddComponent<MRSeedPlanter>();

            planter.Configure(sproutPrefab, sproutPrefabId);
        }

        Rigidbody rb = currentSpawnedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("Spawned held object: " + currentSpawnedObject.name + " | isTool = " + currentSpawnedIsTool);
    }

    bool IsMrWorldGenerationBlocked(InventorySlotUI slot)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool isMrWorldSection = panelController.GetCurrentSectionIndex() == mrWorldSectionIndex;
        bool isBlocked = currentSceneName != mrSceneName && isMrWorldSection && !slot.isTool;

        if (isBlocked)
            Debug.Log("MR world generation is disabled in scene: " + currentSceneName);

        return isBlocked;
    }

    bool IsSeedSlot(InventorySlotUI slot)
    {
        return slot != null && slot.resourceName == seedResourceName;
    }

    bool TryConsumeSeed(string resourceName)
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning("ResourceManager not found, cannot use Seed");
            return false;
        }

        if (!ResourceManager.Instance.ConsumeResource(resourceName, 1))
            return false;

        if (panelController != null)
            panelController.RefreshResourceUI();

        return true;
    }

    void HandleGripRelease()
    {
        if (currentSpawnedObject == null) return;

        if (currentSpawnedIsTool)
        {
            Debug.Log("Released tool, returning it to inventory");
            Destroy(currentSpawnedObject);
        }
        else
        {
            MRSeedPlanter planter = currentSpawnedObject.GetComponent<MRSeedPlanter>();
            if (planter != null)
                planter.Arm();

            currentSpawnedObject.transform.SetParent(null, true);

            Rigidbody rb = currentSpawnedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }

        currentSpawnedObject = null;
        currentSpawnedIsTool = false;
    }

    public void ClearSpawnedObject()
    {
        if (currentSpawnedObject != null)
        {
            Destroy(currentSpawnedObject);
            currentSpawnedObject = null;
            currentSpawnedIsTool = false;
        }
    }
}
