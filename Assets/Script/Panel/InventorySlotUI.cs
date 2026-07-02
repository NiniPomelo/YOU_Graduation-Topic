using UnityEngine;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("資源資訊")]
    public string resourceName;
    public bool isTool = false;
    public GameObject spawnPrefab;

    [Header("UI")]
    public TextMeshProUGUI countText;

    [Header("選取效果")]
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = Vector3.one * 1.2f;

    void Update()
    {
        if (countText == null || ResourceManager.Instance == null) return;

        ResourceManager.CraftRecipe recipe = ResourceManager.Instance.GetRecipe(resourceName);
        int ownedAmount = ResourceManager.Instance.GetResource(resourceName);

        if (recipe == null)
        {
            countText.text = ownedAmount.ToString();
            return;
        }

        int craftableAmount = ResourceManager.Instance.GetCraftableCount(resourceName);
        countText.richText = true;

        if (craftableAmount > 0)
            countText.text = ownedAmount + "\n<color=#FFD84A>+" + craftableAmount + "</color>";
        else
            countText.text = ownedAmount.ToString();
    }

    public void SetSelected(bool selected)
    {
        transform.localScale = selected ? selectedScale : normalScale;
    }
}