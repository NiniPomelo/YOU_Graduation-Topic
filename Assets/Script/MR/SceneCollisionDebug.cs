using UnityEngine;

public class SceneCollisionDebug : MonoBehaviour
{
    [Header("Inventory Panel")]
    public InventoryPanelController inventoryPanel;

    [Header("Spawn Button")]
    public OVRInput.Button spawnButton = OVRInput.Button.PrimaryIndexTrigger;

    [Header("Ray Start")]
    public Transform rayStartPoint;
    public float spawnDistance = 1.5f;

    [Header("Ground Placement")]
    public bool stickToGround = true;
    public float groundY = 0f;
    public float yOffset = 0.02f;

    [Header("Line Visual")]
    public bool useLineVisual = true;
    public LineRenderer line;
    public float lineLength = 1.5f;

    [Header("Spawn Cooldown")]
    public float inputCooldown = 0.2f;
    private float lastInputTime;

    void Start()
    {
        InitializeXRReferences();
        SetupLineRenderer();

        if (line != null)
            line.enabled = false;
    }

    void InitializeXRReferences()
    {
        if (inventoryPanel == null)
            inventoryPanel = Object.FindFirstObjectByType<InventoryPanelController>();

        if (rayStartPoint == null)
        {
            var rig = Object.FindFirstObjectByType<OVRCameraRig>();
            if (rig != null)
                rayStartPoint = rig.rightHandAnchor;
        }
    }

    void SetupLineRenderer()
    {
        if (line == null) return;

        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = 0.005f;
        line.endWidth = 0.005f;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 0;
        line.numCornerVertices = 0;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
    }

    void Update()
    {
        HandleLineVisual();
        HandleTriggerSpawn();
    }

    void HandleLineVisual()
    {
        if (!useLineVisual || line == null || rayStartPoint == null)
            return;

        bool showLine = inventoryPanel != null && inventoryPanel.IsPanelOpen();
        line.enabled = showLine;

        if (!showLine) return;

        Vector3 startPos = rayStartPoint.position + rayStartPoint.forward * 0.03f;
        Vector3 endPos = startPos + rayStartPoint.forward * lineLength;

        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
    }

    void HandleTriggerSpawn()
    {
        if (inventoryPanel == null)
        {
            Debug.LogWarning("inventoryPanel is not assigned.");
            return;
        }

        if (rayStartPoint == null)
        {
            Debug.LogWarning("rayStartPoint is not assigned. Please assign RightHandAnchor.");
            return;
        }

        if (inventoryPanel.GetCurrentSectionIndex() != 0)
            return;

        if (CraftQuantityDialog.AnyDialogOpen)
            return;

        float triggerValue = OVRInput.Get(
            OVRInput.Axis1D.PrimaryIndexTrigger,
            OVRInput.Controller.RTouch
        );

        if (triggerValue <= 0.8f)
            return;

        if (Time.time - lastInputTime <= inputCooldown)
            return;

        InventorySlotUI slot = inventoryPanel.GetCurrentSlot();

        if (slot == null)
        {
            Debug.LogWarning("No selected slot.");
            return;
        }

        GameObject prefabToSpawn = slot.spawnPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Selected slot has no spawnPrefab.");
            return;
        }

        if (!TryConsumeSelectedSlotForSpawn(slot))
            return;

        Vector3 spawnPos = GetFixedSpawnPosition();

        Quaternion spawnRot = Quaternion.Euler(
            0f,
            rayStartPoint.eulerAngles.y,
            0f
        );

        GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPos, spawnRot);

        SpawnedObjectRecord record = spawnedObj.GetComponent<SpawnedObjectRecord>();
        if (record == null)
            record = spawnedObj.AddComponent<SpawnedObjectRecord>();

        record.prefabId = prefabToSpawn.name;

        if (KarmaSystem.Instance != null)
            KarmaSystem.Instance.AddConstructionKarma(slot.resourceName);

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        lastInputTime = Time.time;

        Debug.Log("Spawned prefab: " + prefabToSpawn.name + " at " + spawnPos);
    }

    bool TryConsumeSelectedSlotForSpawn(InventorySlotUI slot)
    {
        if (slot == null || string.IsNullOrEmpty(slot.resourceName))
            return false;

        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning("ResourceManager not found, cannot spawn: " + slot.resourceName);
            return false;
        }

        if (ResourceManager.Instance.IsToolItem(slot.resourceName))
            return true;

        if (!ResourceManager.Instance.ConsumeResource(slot.resourceName, 1))
        {
            Debug.LogWarning("Not enough owned prefab count to spawn: " + slot.resourceName);
            return false;
        }

        if (inventoryPanel != null)
            inventoryPanel.RefreshResourceUI();

        return true;
    }

    Vector3 GetFixedSpawnPosition()
    {
        Vector3 pos = rayStartPoint.position + rayStartPoint.forward * spawnDistance;

        if (stickToGround)
            pos.y = groundY + yOffset;

        return pos;
    }
}