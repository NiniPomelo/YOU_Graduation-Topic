using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

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

    [Header("Craft Dialog UI")]
    public GameObject craftDialogPanel;
    public TMP_Text craftDialogTitleText;
    public TMP_Text craftDialogQuantityText;

    private GameObject currentSpawnedObject;
    private bool currentSpawnedIsTool = false;
    private CraftQuantityDialog craftDialog;
    private InventorySlotUI lastAutoPromptedSlot;
    private int lastAutoPromptedCraftableCount = -1;
    private ResourceManager subscribedResourceManager;

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

        EnsureCraftDialog();
        SubscribeResourceEventsIfNeeded();

        if (panelController != null)
            panelController.CurrentSlotChanged += HandleCurrentSlotChanged;
    }

    void OnDestroy()
    {
        if (panelController != null)
            panelController.CurrentSlotChanged -= HandleCurrentSlotChanged;

        UnsubscribeResourceEvents();
    }

    void Update()
    {
        if (panelController == null) return;

        EnsureCraftDialog();
        SubscribeResourceEventsIfNeeded();

        if (panelController.IsPanelOpen())
            TryShowCraftDialogForSelectedSlot(panelController.GetCurrentSlot());
        else
        {
            lastAutoPromptedSlot = null;
            lastAutoPromptedCraftableCount = -1;
        }

        if (craftDialog != null && craftDialog.IsOpen) return;
        if (rightHandAnchor == null) return;

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

    void HandleCurrentSlotChanged(InventorySlotUI slot)
    {
        TryShowCraftDialogForSelectedSlot(slot, true);
    }

    void HandleResourcesChanged()
    {
        if (panelController != null)
            panelController.RefreshResourceUI();

        lastAutoPromptedSlot = null;
        lastAutoPromptedCraftableCount = -1;

        if (panelController != null && panelController.IsPanelOpen())
            TryShowCraftDialogForSelectedSlot(panelController.GetCurrentSlot(), true);
    }

    void SubscribeResourceEventsIfNeeded()
    {
        ResourceManager manager = ResourceManager.Instance;
        if (subscribedResourceManager == manager) return;

        UnsubscribeResourceEvents();

        subscribedResourceManager = manager;
        if (subscribedResourceManager != null)
            subscribedResourceManager.ResourcesChanged += HandleResourcesChanged;
    }

    void UnsubscribeResourceEvents()
    {
        if (subscribedResourceManager != null)
            subscribedResourceManager.ResourcesChanged -= HandleResourcesChanged;

        subscribedResourceManager = null;
    }

    void TryShowCraftDialogForSelectedSlot(InventorySlotUI slot, bool forceCheck = false)
    {
        if (slot == null || ResourceManager.Instance == null) return;
        if (craftDialog != null && craftDialog.IsOpen) return;

        int craftableCount = GetCraftableDialogCount(slot);
        if (craftableCount <= 0)
            return;

        if (!forceCheck && slot == lastAutoPromptedSlot && craftableCount == lastAutoPromptedCraftableCount)
            return;

        lastAutoPromptedSlot = slot;
        lastAutoPromptedCraftableCount = craftableCount;
        ShowCraftDialog(slot);
    }

    bool ShouldOpenCraftDialog(InventorySlotUI slot)
    {
        return GetCraftableDialogCount(slot) > 0;
    }

    int GetCraftableDialogCount(InventorySlotUI slot)
    {
        if (slot == null || string.IsNullOrEmpty(slot.resourceName)) return 0;
        if (IsSeedSlot(slot)) return 0;
        if (ResourceManager.Instance == null) return 0;

        ResourceManager.CraftRecipe recipe = ResourceManager.Instance.GetRecipe(slot.resourceName);
        if (recipe == null) return 0;

        return ResourceManager.Instance.GetCraftableCount(slot.resourceName);
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

        if (!currentSlotIsSeed && !TryPrepareSlotItem(currentSlot))
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

        currentSpawnedIsTool = IsToolSlot(currentSlot);

        if (!currentSlotIsSeed && !currentSpawnedIsTool && KarmaSystem.Instance != null)
            KarmaSystem.Instance.AddConstructionKarma(currentSlot.resourceName);

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
        bool isBlocked = currentSceneName != mrSceneName && isMrWorldSection && !IsToolSlot(slot);

        if (isBlocked)
            Debug.Log("MR world generation is disabled in scene: " + currentSceneName);

        return isBlocked;
    }

    bool IsSeedSlot(InventorySlotUI slot)
    {
        return slot != null && slot.resourceName == seedResourceName;
    }

    bool IsToolSlot(InventorySlotUI slot)
    {
        if (slot == null || ResourceManager.Instance == null) return false;
        return ResourceManager.Instance.IsToolItem(slot.resourceName);
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

        if (KarmaSystem.Instance != null)
            KarmaSystem.Instance.AddRestorationKarma(2);

        if (panelController != null)
            panelController.RefreshResourceUI();

        return true;
    }

    bool TryPrepareSlotItem(InventorySlotUI slot)
    {
        if (slot == null || ResourceManager.Instance == null)
        {
            Debug.LogWarning("ResourceManager not found, cannot prepare selected item");
            return false;
        }

        string resourceName = slot.resourceName;
        ResourceManager.CraftRecipe recipe = ResourceManager.Instance.GetRecipe(resourceName);
        int ownedAmount = ResourceManager.Instance.GetResource(resourceName);

        if (recipe == null)
            return ownedAmount > 0;

        if (ownedAmount > 0)
        {
            if (!IsToolSlot(slot) && !ResourceManager.Instance.ConsumeResource(resourceName, 1))
                return false;

            if (panelController != null)
                panelController.RefreshResourceUI();

            return true;
        }

        if (ResourceManager.Instance.GetCraftableCount(resourceName) > 0)
        {
            ShowCraftDialog(slot);
            return false;
        }

        Debug.LogWarning("Not enough resources to prepare item: " + resourceName);
        return false;
    }

    void EnsureCraftDialog()
    {
        if (craftDialog != null) return;

        FindCraftDialogReferencesIfNeeded();

        GameObject dialogObject = new GameObject("CraftQuantityDialogController");
        dialogObject.transform.SetParent(panelController != null ? panelController.transform : transform, false);
        craftDialog = dialogObject.AddComponent<CraftQuantityDialog>();
        craftDialog.Initialize(panelController, craftDialogPanel, craftDialogTitleText, craftDialogQuantityText);
    }

    void FindCraftDialogReferencesIfNeeded()
    {
        if (panelController == null) return;

        if (craftDialogPanel == null)
        {
            Transform dialogTransform = null;

            if (panelController.panel != null)
                dialogTransform = FindChildRecursive(panelController.panel.transform, "CraftDialogPanel");

            if (dialogTransform == null)
                dialogTransform = FindChildRecursive(panelController.transform, "CraftDialogPanel");

            if (dialogTransform != null)
                craftDialogPanel = dialogTransform.gameObject;
        }

        if (craftDialogPanel == null) return;

        if (craftDialogTitleText == null)
            craftDialogTitleText = FindTextInChildren(craftDialogPanel.transform, "TitleText");

        if (craftDialogQuantityText == null)
            craftDialogQuantityText = FindTextInChildren(craftDialogPanel.transform, "QuantityText");
    }

    Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null) return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }

    TMP_Text FindTextInChildren(Transform parent, string objectName)
    {
        if (parent == null) return null;

        TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].gameObject.name == objectName)
                return texts[i];
        }

        return null;
    }

    void ShowCraftDialog(InventorySlotUI slot)
    {
        EnsureCraftDialog();
        if (craftDialog != null)
            craftDialog.Show(slot);
    }

    public void IncreaseCraftQuantity()
    {
        EnsureCraftDialog();
        if (craftDialog != null)
            craftDialog.ChangeQuantity(1);
    }

    public void DecreaseCraftQuantity()
    {
        EnsureCraftDialog();
        if (craftDialog != null)
            craftDialog.ChangeQuantity(-1);
    }

    public void ConfirmCraftQuantity()
    {
        EnsureCraftDialog();
        if (craftDialog != null)
            craftDialog.Confirm();
    }

    public void CancelCraftQuantity()
    {
        EnsureCraftDialog();
        if (craftDialog != null)
            craftDialog.Hide();
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

class CraftQuantityDialog : MonoBehaviour
{
    public static bool AnyDialogOpen { get; private set; }

    private InventoryPanelController panelController;
    private InventorySlotUI currentSlot;
    private GameObject root;
    private TMP_Text titleText;
    private TMP_Text quantityText;
    private Transform rayStartPoint;
    private int maxQuantity;
    private int selectedQuantity = 1;
    private float lastQuantityInputTime;
    private bool usingManualUI;
    private const float quantityInputCooldown = 0.2f;
    private const float buttonRayDistance = 5f;

    public bool IsOpen => root != null && root.activeSelf;

    public void Initialize(InventoryPanelController controller, GameObject manualRoot = null, TMP_Text manualTitleText = null, TMP_Text manualQuantityText = null)
    {
        panelController = controller;
        if (manualRoot != null)
            SetManualUI(manualRoot, manualTitleText, manualQuantityText);
        else
            BuildUI();

        Hide();
    }

    public void SetManualUI(GameObject manualRoot, TMP_Text manualTitleText, TMP_Text manualQuantityText)
    {
        if (manualRoot == null) return;

        bool wasOpen = IsOpen;
        GameObject previousRoot = root;

        if (previousRoot != null && previousRoot != manualRoot)
            previousRoot.SetActive(false);

        root = manualRoot;
        usingManualUI = true;
        titleText = manualTitleText != null ? manualTitleText : FindTextInRoot("TitleText");
        quantityText = manualQuantityText != null ? manualQuantityText : FindTextInRoot("QuantityText");
        WireManualButtons();
        PrepareDialogRootForPanelDisplay();

        root.SetActive(wasOpen);
    }

    public void Show(InventorySlotUI slot)
    {
        if (slot == null || ResourceManager.Instance == null) return;

        BuildUI();
        currentSlot = slot;
        maxQuantity = ResourceManager.Instance.GetCraftableCount(slot.resourceName);
        selectedQuantity = Mathf.Clamp(1, 1, maxQuantity);

        if (maxQuantity <= 0)
        {
            Hide();
            return;
        }

        if (panelController != null)
            panelController.SetInputLocked(true);

        PrepareDialogRootForPanelDisplay();
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        AnyDialogOpen = true;
        RefreshText();
        Debug.Log("Craft dialog opened for " + currentSlot.resourceName + " | root = " + root.name);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);

        if (panelController != null)
            panelController.SetInputLocked(false);

        AnyDialogOpen = false;
        currentSlot = null;
        maxQuantity = 0;
        selectedQuantity = 1;
    }

    private void Update()
    {
        if (!IsOpen) return;

        Vector2 axis = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick, OVRInput.Controller.RTouch);

        if (Time.time - lastQuantityInputTime >= quantityInputCooldown)
        {
            if (axis.x > 0.6f)
            {
                ChangeQuantity(1);
                lastQuantityInputTime = Time.time;
            }
            else if (axis.x < -0.6f)
            {
                ChangeQuantity(-1);
                lastQuantityInputTime = Time.time;
            }
        }

        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            Confirm();
        else if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            Hide();

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            TryClickButtonWithRay();
    }

    private void BuildUI()
    {
        if (root != null)
        {
            if (titleText == null)
                titleText = FindTextInRoot("TitleText");

            if (quantityText == null)
                quantityText = FindTextInRoot("QuantityText");

            return;
        }

        Transform parent = panelController != null && panelController.panel != null
            ? panelController.panel.transform
            : transform;

        root = new GameObject("CraftQuantityDialog", typeof(RectTransform), typeof(Image));
        usingManualUI = false;
        root.transform.SetParent(parent, false);
        root.transform.SetAsLastSibling();

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(520f, 300f);

        Image rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0.7f, 0.05f, 0.05f, 0.92f);

        titleText = CreateText("Title", root.transform, new Vector2(0f, 98f), new Vector2(460f, 52f), 34);
        quantityText = CreateText("Quantity", root.transform, new Vector2(0f, 38f), new Vector2(460f, 48f), 26);

        CreateButton("Minus", "-", new Vector2(-165f, -78f), () => ChangeQuantity(-1));
        CreateButton("Plus", "+", new Vector2(-55f, -78f), () => ChangeQuantity(1));
        CreateButton("Confirm", "O", new Vector2(65f, -78f), Confirm);
        CreateButton("Cancel", "X", new Vector2(175f, -78f), Hide);
    }

    private void PrepareDialogRootForPanelDisplay()
    {
        if (root == null) return;

        if (panelController != null && panelController.panel != null && root.transform.parent != panelController.panel.transform)
            root.transform.SetParent(panelController.panel.transform, false);

        Canvas nestedCanvas = root.GetComponent<Canvas>();
        if (nestedCanvas != null)
            nestedCanvas.enabled = false;

        GraphicRaycaster nestedRaycaster = root.GetComponent<GraphicRaycaster>();
        if (nestedRaycaster != null)
            nestedRaycaster.enabled = false;

        RectTransform rect = root.GetComponent<RectTransform>();
        if (rect == null) return;

        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        Vector3 localPosition = rect.localPosition;
        localPosition.z = 0f;
        rect.localPosition = localPosition;

        if (usingManualUI)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 300f);
        rect.anchoredPosition = Vector2.zero;

        Image image = root.GetComponent<Image>();
        if (image != null)
        {
            image.enabled = true;
            image.color = new Color(0.7f, 0.05f, 0.05f, 0.92f);
            image.raycastTarget = true;
        }
    }

    private TMP_Text CreateText(string name, Transform parent, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text text = obj.GetComponent<TMP_Text>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = Color.white;
        return text;
    }

    private TMP_Text FindTextInRoot(string objectName)
    {
        if (root == null) return null;

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text != null && text.gameObject.name == objectName)
                return text;
        }

        return null;
    }

    private void WireManualButtons()
    {
        if (root == null) return;

        WireManualButton("MinusButton", () => ChangeQuantity(-1));
        WireManualButton("PlusButton", () => ChangeQuantity(1));
        WireManualButton("ConfirmButton", Confirm);
        WireManualButton("CancelButton", Hide);
    }

    private void WireManualButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        Transform buttonTransform = FindChildRecursive(root.transform, objectName);
        if (buttonTransform == null) return;

        Button button = buttonTransform.GetComponent<Button>();
        if (button == null) return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void TryClickButtonWithRay()
    {
        EnsureRayStartPoint();
        if (rayStartPoint == null || root == null) return;

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        Button hitButton = null;
        float closestDistance = buttonRayDistance;
        Ray ray = new Ray(rayStartPoint.position, rayStartPoint.forward);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
                continue;

            RectTransform rect = button.transform as RectTransform;
            if (rect == null)
                continue;

            if (!TryGetRayRectHitDistance(ray, rect, out float distance))
                continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                hitButton = button;
            }
        }

        if (hitButton == null)
            return;

        Debug.Log("Craft dialog ray clicked: " + hitButton.gameObject.name);
        hitButton.onClick.Invoke();
    }

    private void EnsureRayStartPoint()
    {
        if (rayStartPoint != null)
            return;

        OVRCameraRig rig = Object.FindFirstObjectByType<OVRCameraRig>();
        if (rig != null && rig.rightHandAnchor != null)
        {
            rayStartPoint = rig.rightHandAnchor;
            return;
        }

        GameObject rightHand = GameObject.Find("RightHandAnchor");
        if (rightHand != null)
            rayStartPoint = rightHand.transform;
    }

    private bool TryGetRayRectHitDistance(Ray ray, RectTransform rect, out float distance)
    {
        distance = 0f;

        Plane plane = new Plane(rect.forward, rect.position);
        if (!plane.Raycast(ray, out distance))
            return false;

        if (distance < 0f || distance > buttonRayDistance)
            return false;

        Vector3 localPoint = rect.InverseTransformPoint(ray.GetPoint(distance));
        return rect.rect.Contains(new Vector2(localPoint.x, localPoint.y));
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null) return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }

    private void CreateButton(string name, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(root.transform, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(72f, 52f);

        Image image = obj.GetComponent<Image>();
        image.color = label == "O" ? new Color(0.2f, 0.55f, 0.25f, 1f) : new Color(0.35f, 0.35f, 0.35f, 1f);

        Button button = obj.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        TMP_Text text = CreateText(label + "Text", obj.transform, Vector2.zero, rect.sizeDelta, 28);
        text.text = label;
    }

    public void ChangeQuantity(int delta)
    {
        if (!IsOpen || maxQuantity <= 0) return;

        selectedQuantity = Mathf.Clamp(selectedQuantity + delta, 1, maxQuantity);
        RefreshText();
    }

    private void RefreshText()
    {
        if (currentSlot == null) return;

        if (titleText != null)
            titleText.text = currentSlot.resourceName;

        if (quantityText != null)
            quantityText.text = "Can make " + maxQuantity + " / Select " + selectedQuantity;
    }

    public void Confirm()
    {
        if (currentSlot == null || ResourceManager.Instance == null) return;

        if (ResourceManager.Instance.TryCraftItems(currentSlot.resourceName, selectedQuantity))
        {
            if (panelController != null)
                panelController.RefreshResourceUI();

            Hide();
        }
    }
}




