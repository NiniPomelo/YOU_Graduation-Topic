using UnityEngine;
using System;

[RequireComponent(typeof(LineRenderer))]
public class VRRayGrab_Forest : MonoBehaviour
{
    [Header("手")]
    public Transform rightHandTransform;

    [Header("射線設定")]
    public float rayLength = 5f;
    public LayerMask interactableLayer;

    [Header("抓取")]
    private GameObject grabbedObject = null;
    private Rigidbody grabbedRb = null;

    [Header("工具判定")]
    public string axeObjectName = "Axe";

    [Header("可收集物名稱")]
    public string woodObjectName = "Wood";
    public string seedObjectName = "Seed";

    [Header("砍樹判定")]
    public float chopDistance = 0.3f;
    public float chopCooldown = 0.5f;
    public int axeDamagePerChop = 10;
    private float lastChopTime = -999f;

    [Header("掉落物 Prefab")]
    public GameObject woodPrefab;
    public GameObject seedPrefab;

    [Header("生成範圍與數量")]
    public float dropRadius = 0.5f;
    public int minWood = 1;
    public int maxWood = 3;
    public int minSeed = 1;
    public int maxSeed = 2;

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.01f;
        line.endWidth = 0.01f;
        line.material = new Material(Shader.Find("Unlit/Color"));
        line.material.color = Color.white;
    }

    void Update()
    {
        if (rightHandTransform == null) return;

        Ray ray = new Ray(rightHandTransform.position, rightHandTransform.forward);

        // 射線顯示
        line.SetPosition(0, rightHandTransform.position);
        line.SetPosition(1, rightHandTransform.position + rightHandTransform.forward * rayLength);

        HandleGrabAndCollect(ray);
        HandleChopping();
    }

    void HandleGrabAndCollect(Ray ray)
    {
        if (grabbedObject == null)
        {
            GameObject target = GetFirstValidHitObject(ray);

            if (target != null)
            {
                Debug.Log("Forest 有效命中物件：" + target.name);

                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                {
                    // 手上拿 Axe 時，只允許抓可收集物
                    if (IsHoldingAxe())
                    {
                        if (!IsCollectible(target))
                        {
                            Debug.Log("手上有 Axe，但命中物不是可收集物，略過：" + target.name);
                            return;
                        }
                    }

                    GrabObject(target);
                    Debug.Log("成功抓取：" + target.name);
                }
            }
        }
        else
        {
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                Debug.Log("放開 Trigger，收集：" + grabbedObject.name);
                CollectResource(grabbedObject);
                ReleaseObject();
            }
        }
    }

    GameObject GetFirstValidHitObject(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, rayLength, interactableLayer);
        if (hits == null || hits.Length == 0) return null;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            GameObject obj = hits[i].collider.gameObject;

            // 忽略右手本體與右手底下所有子物件（例如 Axe）
            if (obj.transform == rightHandTransform || obj.transform.IsChildOf(rightHandTransform))
                continue;

            return obj;
        }

        return null;
    }

    void HandleChopping()
    {
        if (!IsHoldingAxe()) return;
        if (Time.time - lastChopTime < chopCooldown) return;

        RaycastHit hit;
        if (Physics.Raycast(rightHandTransform.position, rightHandTransform.forward, out hit, chopDistance))
        {
            if (hit.collider.CompareTag("Tree"))
            {
                ChopTree(hit.collider.gameObject);
                lastChopTime = Time.time;
            }
            else
            {
                Transform root = hit.collider.transform.root;
                if (root != null && root.CompareTag("Tree"))
                {
                    ChopTree(root.gameObject);
                    lastChopTime = Time.time;
                }
            }
        }
    }

    bool IsHoldingAxe()
    {
        if (rightHandTransform == null) return false;

        for (int i = 0; i < rightHandTransform.childCount; i++)
        {
            Transform child = rightHandTransform.GetChild(i);
            string cleanName = CleanName(child.name);

            if (cleanName == axeObjectName || cleanName.Contains(axeObjectName))
                return true;
        }

        return false;
    }

    bool IsCollectible(GameObject obj)
    {
        string cleanName = CleanName(obj.name).ToLower();

        return cleanName.Contains(woodObjectName.ToLower()) ||
               cleanName.Contains(seedObjectName.ToLower());
    }

    string CleanName(string objName)
    {
        return objName.Replace("(Clone)", "").Trim();
    }

    void GrabObject(GameObject obj)
    {
        grabbedObject = obj;
        grabbedRb = obj.GetComponent<Rigidbody>();

        if (grabbedRb != null)
        {
            grabbedRb.isKinematic = true;
            grabbedRb.useGravity = false;
            grabbedRb.linearVelocity = Vector3.zero;
            grabbedRb.angularVelocity = Vector3.zero;
        }

        obj.transform.SetParent(rightHandTransform, true);
    }

    void ReleaseObject()
    {
        if (grabbedRb != null)
        {
            grabbedRb.isKinematic = false;
            grabbedRb.useGravity = true;
        }

        if (grabbedObject != null)
        {
            grabbedObject.transform.SetParent(null);
        }

        grabbedObject = null;
        grabbedRb = null;
    }

    void CollectResource(GameObject obj)
    {
        string typeName = CleanName(obj.name);

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource(typeName, 1);
        }
        else
        {
            Debug.LogWarning("ResourceManager.Instance 為 null");
        }

        Destroy(obj);
    }

    void ChopTree(GameObject tree)
    {
        if (tree == null) return;

        bool axeStillUsable = ResourceManager.Instance == null || ResourceManager.Instance.UseTool(axeObjectName, axeDamagePerChop);
        Vector3 treePos = tree.transform.position;
        //  加在這裡
        if (KarmaSystem.Instance != null)
            KarmaSystem.Instance.AddForestNegative(1);

        // 樹倒下
        tree.transform.Rotate(Vector3.right, 90f);
        Destroy(tree, 1f);

        // 生成 Wood
        int woodCount = UnityEngine.Random.Range(minWood, maxWood + 1);
        for (int i = 0; i < woodCount; i++)
        {
            Vector3 offset = new Vector3(
                UnityEngine.Random.Range(-dropRadius, dropRadius),
                0.5f,
                UnityEngine.Random.Range(-dropRadius, dropRadius)
            );

            GameObject wood = Instantiate(woodPrefab, treePos + offset, Quaternion.identity);

            Rigidbody rb = wood.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Vector3.up * UnityEngine.Random.Range(1f, 2f), ForceMode.Impulse);
            }
        }

        // 生成 Seed
        int seedCount = UnityEngine.Random.Range(minSeed, maxSeed + 1);
        for (int i = 0; i < seedCount; i++)
        {
            Vector3 offset = new Vector3(
                UnityEngine.Random.Range(-dropRadius, dropRadius),
                0.5f,
                UnityEngine.Random.Range(-dropRadius, dropRadius)
            );

            GameObject seed = Instantiate(seedPrefab, treePos + offset, Quaternion.identity);

            Rigidbody rb = seed.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Vector3.up * UnityEngine.Random.Range(0.5f, 1.5f), ForceMode.Impulse);
            }
        }

        if (!axeStillUsable)
            DestroyHeldTool(axeObjectName);
    }

    void DestroyHeldTool(string toolName)
    {
        if (rightHandTransform == null) return;

        for (int i = rightHandTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = rightHandTransform.GetChild(i);
            string cleanName = CleanName(child.name);

            if (cleanName == toolName || cleanName.Contains(toolName))
                Destroy(child.gameObject);
        }
    }
}