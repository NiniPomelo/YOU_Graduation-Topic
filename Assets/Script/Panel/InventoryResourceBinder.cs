using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryResourceBinder : MonoBehaviour
{
    [System.Serializable]
    public class ResourceSlotBinding
    {
        [Header("Resource name used by ResourceManager")]
        public string resourceName;

        [Header("UI references")]
        public GameObject slotRoot;
        public TMP_Text countText;
        public Image iconImage;

        [Header("Hide this slot when owned and craftable amounts are both zero")]
        public bool hideWhenZero = false;
    }

    [Header("Bound resource slots")]
    public ResourceSlotBinding[] bindings;

    [Header("Refresh every frame")]
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

            int ownedAmount = ResourceManager.Instance.GetResource(binding.resourceName);
            int craftableAmount = ResourceManager.Instance.GetCraftableCount(binding.resourceName);
            ResourceManager.CraftRecipe recipe = ResourceManager.Instance.GetRecipe(binding.resourceName);

            if (binding.countText != null)
            {
                binding.countText.richText = true;
                binding.countText.text = recipe != null && craftableAmount > 0
                    ? ownedAmount + "\n<color=#FFD84A>+" + craftableAmount + "</color>"
                    : ownedAmount.ToString();
            }

            if (binding.slotRoot != null && binding.hideWhenZero)
                binding.slotRoot.SetActive(ownedAmount + craftableAmount > 0);
        }
    }
}
