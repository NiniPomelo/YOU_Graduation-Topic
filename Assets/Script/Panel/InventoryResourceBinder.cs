using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryResourceBinder : MonoBehaviour
{
    [System.Serializable]
    public class ResourceSlotBinding
    {
        [Header("資源名稱，要跟 AddResource 時用的字一樣")]
        public string resourceName;

        [Header("這格的 UI")]
        public GameObject slotRoot;
        public TMP_Text countText;
        public Image iconImage;

        [Header("當數量為 0 時要不要隱藏")]
        public bool hideWhenZero = false;
    }

    [Header("所有要綁定的資源格子")]
    public ResourceSlotBinding[] bindings;

    [Header("是否每幀刷新")]
    public bool refreshEveryFrame = false;

    private void Start()
    {
        RefreshUI();
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    private void Update()
    {
        if (refreshEveryFrame)
            RefreshUI();
    }

    public void RefreshUI()
    {
        if (ResourceManager.Instance == null || bindings == null) return;

        for (int i = 0; i < bindings.Length; i++)
        {
            ResourceSlotBinding binding = bindings[i];
            if (binding == null || string.IsNullOrEmpty(binding.resourceName)) continue;

            int amount = ResourceManager.Instance.GetResource(binding.resourceName);

            if (binding.countText != null)
                binding.countText.text = amount.ToString();

            if (binding.slotRoot != null && binding.hideWhenZero)
                binding.slotRoot.SetActive(amount > 0);
        }
    }
}