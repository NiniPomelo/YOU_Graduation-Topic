using UnityEngine;

public class OceanResourceSystem : MonoBehaviour
{
    [Header("隨機資源數量")]
    public int minResource = 2;
    public int maxResource = 5;

    public void GenerateResources()
    {
        int oil = Random.Range(minResource, maxResource + 1);
        int gas = Random.Range(minResource, maxResource + 1);

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource("Oil", oil);
            ResourceManager.Instance.AddResource("Gas", gas);

            Debug.Log($"Ocean → Oil +{oil}, Gas +{gas}");
        }
        else
        {
            Debug.LogWarning("ResourceManager 不存在！");
        }
    }
}   