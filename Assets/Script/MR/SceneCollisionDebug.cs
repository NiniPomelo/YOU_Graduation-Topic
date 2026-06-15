using UnityEngine;

public class SceneCollisionDebug : MonoBehaviour
{
    [Header("共用 Panel")]
    public InventoryPanelController inventoryPanel;

    [Header("生成按鍵")]
    public OVRInput.Button spawnButton = OVRInput.Button.PrimaryIndexTrigger;

    [Header("射線起點")]
    public Transform rayStartPoint;   // 拖 RightHandAnchor
    public float spawnDistance = 1.5f;

    [Header("貼地設定")]
    public bool stickToGround = true;
    public float groundY = 0f;         // 地面高度，通常先用 0
    public float yOffset = 0.02f;      // 避免物件陷進地面

    [Header("可視化射線")]
    public bool useLineVisual = true;
    public LineRenderer line;
    public float lineLength = 1.5f;

    [Header("生成冷卻")]
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
        Vector3 endPos = GetFixedSpawnPosition();

        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
    }

    void HandleTriggerSpawn()
    {
        if (inventoryPanel == null)
        {
            Debug.LogWarning("inventoryPanel 沒有指定");
            return;
        }

        if (rayStartPoint == null)
        {
            Debug.LogWarning("rayStartPoint 沒有指定，請拖 RightHandAnchor");
            return;
        }

        if (inventoryPanel.GetCurrentSectionIndex() != 0)
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
            Debug.LogWarning("目前沒有選到任何 Slot");
            return;
        }

        GameObject prefabToSpawn = slot.spawnPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("目前選到的 Slot 沒有設定 spawnPrefab");
            return;
        }

        Vector3 spawnPos = GetFixedSpawnPosition();

        Quaternion spawnRot = Quaternion.Euler(
            0f,
            rayStartPoint.eulerAngles.y,
            0f
        );

        GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPos, spawnRot);

        SpawnedObjectRecord record = spawnedObj.GetComponent<SpawnedObjectRecord>();
        if (record == null)
        {
            record = spawnedObj.AddComponent<SpawnedObjectRecord>();
        }

        record.prefabId = prefabToSpawn.name;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        lastInputTime = Time.time;

        Debug.Log("固定位置生成 Prefab: " + prefabToSpawn.name + " at " + spawnPos);
    }

    Vector3 GetFixedSpawnPosition()
    {
        Vector3 pos = rayStartPoint.position + rayStartPoint.forward * spawnDistance;

        if (stickToGround)
        {
            pos.y = groundY + yOffset;
        }

        return pos;
    }
}