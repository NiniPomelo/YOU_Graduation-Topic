using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VRRayGrabOre : MonoBehaviour
{
    [Header("��")]
    public Transform rightHandTransform;

    [Header("�g�u�]�w")]
    public float rayLength = 5f;
    public LayerMask interactableLayer;

    [Header("���")]
    private GameObject grabbedObject = null;
    private Rigidbody grabbedRb = null;

    [Header("���q�P�w")]
    public float mineDistance = 0.8f;
    public string pickObjectName = "Pick";
    public int pickDamagePerHit = 10;

    [Header("�q�� Prefab")]
    public GameObject sandPrefab;
    public GameObject ironOrePrefab;
    public GameObject limestonePrefab;
    public GameObject marblePrefab;

    [Header("�ͦ��]�w")]
    public float dropRadius = 0.5f;
    public int minDrop = 1;
    public int maxDrop = 3;

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
        if (!rightHandTransform) return;

        Ray ray = new Ray(rightHandTransform.position, rightHandTransform.forward);

        // �g�u���
        line.SetPosition(0, rightHandTransform.position);
        line.SetPosition(1, rightHandTransform.position + rightHandTransform.forward * rayLength);

        HandleGrabAndCollect(ray);
        HandleMining();
    }

    void HandleGrabAndCollect(Ray ray)
    {
        RaycastHit hit;

        if (grabbedObject == null)
        {
            if (Physics.Raycast(ray, out hit, rayLength, interactableLayer))
            {
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                {
                    // �p�G��W�ثe�w�g���� Pick�A�N���n��@�ɪ���
                    if (IsHoldingPick()) return;

                    GrabObject(hit.collider.gameObject);
                }
            }
        }
        else
        {
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                CollectResource(grabbedObject);
                ReleaseObject();
            }
        }
    }

    void HandleMining()
    {
        // �u���k��ثe���� Pick �~���
        if (!IsHoldingPick()) return;

        RaycastHit hit;
        if (Physics.Raycast(rightHandTransform.position, rightHandTransform.forward, out hit, mineDistance))
        {
            if (hit.collider.CompareTag("Rock"))
            {
                RockMining rock = hit.collider.GetComponent<RockMining>();
                if (rock != null)
                {
                    bool pickStillUsable = ResourceManager.Instance == null || ResourceManager.Instance.UseTool(pickObjectName, pickDamagePerHit);
                    rock.HitRock(hit.point);

                    if (!pickStillUsable)
                        DestroyHeldTool(pickObjectName);
                }
            }
        }
    }

    bool IsHoldingPick()
    {
        if (rightHandTransform == null) return false;

        for (int i = 0; i < rightHandTransform.childCount; i++)
        {
            Transform child = rightHandTransform.GetChild(i);

            // �B�z Instantiate ��i��X�{�� (Clone)
            string cleanName = child.name.Replace("(Clone)", "").Trim();

            if (cleanName == pickObjectName)
                return true;
        }

        return false;
    }

    void GrabObject(GameObject obj)
    {
        grabbedObject = obj;

        grabbedRb = obj.GetComponent<Rigidbody>();
        if (grabbedRb != null)
            grabbedRb.isKinematic = true;

        obj.transform.SetParent(rightHandTransform, true);
    }

    void ReleaseObject()
    {
        if (grabbedRb != null)
            grabbedRb.isKinematic = false;

        if (grabbedObject != null)
            grabbedObject.transform.SetParent(null);

        grabbedObject = null;
        grabbedRb = null;
    }

    void CollectResource(GameObject obj)
    {
        string typeName = obj.name.Replace("(Clone)", "").Trim();
        Debug.Log("�B����귽: [" + typeName + "]");

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource(typeName, 1);
            Debug.Log("�w�[�J ResourceManager: " + typeName);
        }

        Destroy(obj);
    }

    // ----------- �p�G�A�����٭n�b�o��̭��H�����q�A�i�H�O�d -----------

    void MineRock(GameObject rock)
    {
        Vector3 rockPos = rock.transform.position;

        int dropCount = Random.Range(minDrop, maxDrop + 1);

        for (int i = 0; i < dropCount; i++)
        {
            GameObject prefab = GetRandomOre();

            Vector3 offset = new Vector3(
                Random.Range(-dropRadius, dropRadius),
                0.5f,
                Random.Range(-dropRadius, dropRadius)
            );

            GameObject ore = Instantiate(prefab, rockPos + offset, Quaternion.identity);

            Rigidbody rb = ore.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(Vector3.up * Random.Range(1f, 2f), ForceMode.Impulse);
        }
    }

    GameObject GetRandomOre()
    {
        int rand = Random.Range(0, 4);

        switch (rand)
        {
            case 0: return sandPrefab;
            case 1: return ironOrePrefab;
            case 2: return limestonePrefab;
            case 3: return marblePrefab;
        }

        return sandPrefab;
    }

    void DestroyHeldTool(string toolName)
    {
        if (rightHandTransform == null) return;

        for (int i = rightHandTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = rightHandTransform.GetChild(i);
            string cleanName = child.name.Replace("(Clone)", "").Trim();

            if (cleanName == toolName)
                Destroy(child.gameObject);
        }
    }
}