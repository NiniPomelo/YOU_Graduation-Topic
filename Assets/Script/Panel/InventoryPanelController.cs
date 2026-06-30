using UnityEngine;
using TMPro;

public class InventoryPanelController : MonoBehaviour
{
    [Header("左手")]
    public Transform leftHand;

    [Header("Panel")]
    public GameObject panel;
    public Vector3 offset = new Vector3(0.05f, 0.1f, 0.1f);

    [Header("Title")]
    public TextMeshProUGUI titleText;

    [Header("環境 Sections")]
    public GameObject[] sections;

    [Header("環境名稱")]
    public string[] sectionNames;

    [Header("每個環境的 Slot 容器（HorizontalSlots）")]
    public Transform[] slotParents;

    [Header("玩家移動腳本（Panel開啟時停用）")]
    public MonoBehaviour[] locomotionScripts;

    private InventorySlotUI[][] slots;

    private int currentSection = 0;
    private int currentSlot = 0;

    [Header("輸入設定")]
    public float inputCooldown = 0.25f;
    private float lastInputTime = 0f;

    private InventoryResourceBinder resourceBinder;

    void Start()
    {
        if (panel != null)
            panel.SetActive(false);

        resourceBinder = GetComponent<InventoryResourceBinder>();
        if (resourceBinder == null)
            resourceBinder = GetComponentInChildren<InventoryResourceBinder>(true);

        BuildSlotArrays();
        UpdateSection();
        SetLocomotionEnabled(true);

        if (resourceBinder != null)
            resourceBinder.RefreshUI();
    }

    void Update()
    {
        HandlePanelToggle();
        HandlePanelFollow();
        RefreshCraftableSlotVisibility();

        if (panel == null || !panel.activeSelf) return;

        HandleSectionSwitch();
        HandleSlotSelection();
    }

    void BuildSlotArrays()
    {
        if (slotParents == null) return;

        slots = new InventorySlotUI[slotParents.Length][];

        for (int i = 0; i < slotParents.Length; i++)
        {
            if (slotParents[i] == null)
            {
                slots[i] = new InventorySlotUI[0];
                continue;
            }

            slots[i] = slotParents[i].GetComponentsInChildren<InventorySlotUI>(true);
        }
    }

    void HandlePanelToggle()
    {
        if (panel == null) return;

        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
        {
            bool newState = !panel.activeSelf;
            panel.SetActive(newState);

            if (newState)
            {
                currentSlot = 0;
                UpdateSection();
                SetLocomotionEnabled(false);

                if (resourceBinder != null)
                    resourceBinder.RefreshUI();
            }
            else
            {
                SetLocomotionEnabled(true);
            }
        }
    }

    void HandlePanelFollow()
    {
        if (leftHand == null || panel == null || !panel.activeSelf) return;
        if (Camera.main == null) return;

        panel.transform.position =
            leftHand.position
            + leftHand.forward * offset.z
            + leftHand.up * offset.y
            + leftHand.right * offset.x;

        panel.transform.rotation =
            Quaternion.LookRotation(panel.transform.position - Camera.main.transform.position);
    }

    void HandleSectionSwitch()
    {
        if (Time.time - lastInputTime < inputCooldown) return;
        if (sections == null || sections.Length == 0) return;

        Vector2 axis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);

        if (axis.y > 0.5f)
        {
            currentSection = (currentSection + 1) % sections.Length;
            currentSlot = 0;
            UpdateSection();
            lastInputTime = Time.time;
        }
        else if (axis.y < -0.5f)
        {
            currentSection = (currentSection - 1 + sections.Length) % sections.Length;
            currentSlot = 0;
            UpdateSection();
            lastInputTime = Time.time;
        }
    }

    void HandleSlotSelection()
    {
        if (Time.time - lastInputTime < inputCooldown) return;
        if (slots == null || currentSection >= slots.Length) return;
        if (slots[currentSection] == null || slots[currentSection].Length == 0) return;

        Vector2 axis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        int slotCount = slots[currentSection].Length;

        if (axis.x > 0.5f)
        {
            currentSlot = GetNextAvailableSlotIndex(1, slotCount);
            UpdateSlotSelection();
            lastInputTime = Time.time;
        }
        else if (axis.x < -0.5f)
        {
            currentSlot = GetNextAvailableSlotIndex(-1, slotCount);
            UpdateSlotSelection();
            lastInputTime = Time.time;
        }
    }

    void UpdateSection()
    {
        if (sections == null || sectionNames == null) return;
        if (currentSection < 0 || currentSection >= sections.Length) return;

        for (int i = 0; i < sections.Length; i++)
        {
            if (sections[i] != null)
                sections[i].SetActive(i == currentSection);
        }

        if (titleText != null && currentSection < sectionNames.Length)
        {
            titleText.text = sectionNames[currentSection];
        }

        UpdateSlotSelection();
    }

    int GetNextAvailableSlotIndex(int direction, int slotCount)
    {
        if (slotCount <= 0) return currentSlot;

        for (int step = 1; step <= slotCount; step++)
        {
            int nextIndex = (currentSlot + direction * step + slotCount) % slotCount;

            if (IsSlotVisible(currentSection, nextIndex))
                return nextIndex;
        }

        return currentSlot;
    }

    void UpdateSlotSelection()
    {
        if (slots == null) return;

        if (currentSection >= 0 && currentSection < slots.Length)
            currentSlot = GetFirstVisibleSlotOrCurrent(currentSection, currentSlot);

        for (int s = 0; s < slots.Length; s++)
        {
            if (slots[s] == null) continue;

            for (int i = 0; i < slots[s].Length; i++)
            {
                bool selected = (s == currentSection && i == currentSlot);
                slots[s][i].SetSelected(selected);
            }
        }
    }

    void RefreshCraftableSlotVisibility()
    {
        if (slots == null || ResourceManager.Instance == null) return;

        for (int s = 0; s < slots.Length; s++)
        {
            if (slots[s] == null) continue;

            for (int i = 0; i < slots[s].Length; i++)
            {
                InventorySlotUI slot = slots[s][i];
                if (slot == null || string.IsNullOrEmpty(slot.resourceName)) continue;

                ResourceManager.CraftRecipe recipe = ResourceManager.Instance.GetRecipe(slot.resourceName);
                if (recipe == null) continue;

                if (slot.isTool)
                {
                    slot.gameObject.SetActive(true);
                    continue;
                }

                slot.gameObject.SetActive(ResourceManager.Instance.GetAvailableCraftCount(slot.resourceName) > 0);
            }
        }
    }

    int GetFirstVisibleSlotOrCurrent(int sectionIndex, int preferredIndex)
    {
        if (slots[sectionIndex] == null || slots[sectionIndex].Length == 0)
            return preferredIndex;

        if (IsSlotVisible(sectionIndex, preferredIndex))
            return preferredIndex;

        for (int i = 0; i < slots[sectionIndex].Length; i++)
        {
            if (IsSlotVisible(sectionIndex, i))
                return i;
        }

        return preferredIndex;
    }

    bool IsSlotVisible(int sectionIndex, int slotIndex)
    {
        if (slots == null) return false;
        if (sectionIndex < 0 || sectionIndex >= slots.Length) return false;
        if (slots[sectionIndex] == null) return false;
        if (slotIndex < 0 || slotIndex >= slots[sectionIndex].Length) return false;
        if (slots[sectionIndex][slotIndex] == null) return false;

        return slots[sectionIndex][slotIndex].gameObject.activeInHierarchy;
    }

    public InventorySlotUI GetCurrentSlot()
    {
        if (slots == null) return null;
        if (currentSection < 0 || currentSection >= slots.Length) return null;
        if (slots[currentSection] == null || slots[currentSection].Length == 0) return null;
        if (currentSlot < 0 || currentSlot >= slots[currentSection].Length) return null;

        return slots[currentSection][currentSlot];
    }

    public bool IsPanelOpen()
    {
        return panel != null && panel.activeSelf;
    }

    public int GetCurrentSectionIndex()
    {
        return currentSection;
    }

    public int GetCurrentSlotIndex()
    {
        return currentSlot;
    }

    void SetLocomotionEnabled(bool enabledState)
    {
        if (locomotionScripts == null) return;

        for (int i = 0; i < locomotionScripts.Length; i++)
        {
            if (locomotionScripts[i] != null)
            {
                Debug.Log("切換腳本: " + locomotionScripts[i].GetType().Name + " -> " + enabledState);
                locomotionScripts[i].enabled = enabledState;
            }
        }
    }

    public void RefreshResourceUI()
    {
        RefreshCraftableSlotVisibility();

        if (resourceBinder != null)
            resourceBinder.RefreshUI();
    }
}
