using UnityEngine;
using Meta.XR;
using UnityEngine.SceneManagement;
using System.Collections;

public class VRFloatingBall_Final : MonoBehaviour
{
    [Header("玩家與手")]
    public Transform playerHead;
    public Transform rightHand;
    public Transform leftHand;

    [Header("頭前偏移，可在 Inspector 調整")]
    public Vector3 headOffset = new Vector3(0.3f, 0.2f, 2f);

    [Header("手前方跟隨")]
    public Vector3 handOffset = new Vector3(0f, 0.05f, 0.2f);
    public float hoverHeight = 0.05f;
    public float smoothTime = 0.12f;

    [Header("懸浮球")]
    public GameObject floatingBallPrefab;

    [Header("顏色循環")]
    public Color[] ballColors = new Color[] { Color.white, Color.blue, Color.green, Color.gray };
    public float swingThreshold = 0.5f;
    public float swingCooldown = 0.3f;

    [Header("吸附球")]
    public GameObject absorbBallPrefab;
    public Vector3 absorbOffset = new Vector3(0f, 0.3f, 0f);
    public ParticleSystem mergeParticlesPrefab;
    public ParticleSystem absorbParticlePrefab; // 飛行粒子
    public float absorbDistanceThreshold = 0.2f;

    // 對應顏色的場景
    private string[] sceneByColor = new string[] { "MR_Main", "VR_Ocean", "VR_Forest", "VR_Ore" };

    private GameObject floatingBall;
    private Renderer floatingBallRenderer;
    private GameObject leftAbsorbBall;

    private Rigidbody floatingRb;
    private Vector3 velocity = Vector3.zero;
    private float swingTimer = 0f;
    private int currentColorIndex = 0;

    private bool isHeld = false;
    private bool isLockedAtHand = false;
    private Vector3 originalOffsetPos;
    private Vector3 originalScale;
    private Color originalColor;

    private bool isMerging = false;
    private Transform mergeTarget;

    private TrailRenderer ballTrail;
    private bool isBeingAbsorbed = false;

    void Start()
    {
        floatingBall = Instantiate(floatingBallPrefab, floatingBallPrefab.transform.position, floatingBallPrefab.transform.rotation);
        originalScale = floatingBall.transform.localScale;

        SphereCollider sc = floatingBall.GetComponent<SphereCollider>();
        if (sc == null) sc = floatingBall.AddComponent<SphereCollider>();
        sc.isTrigger = true;

        floatingRb = floatingBall.GetComponent<Rigidbody>();
        if (floatingRb == null) floatingRb = floatingBall.AddComponent<Rigidbody>();
        floatingRb.isKinematic = true;
        floatingRb.useGravity = false;
        floatingRb.interpolation = RigidbodyInterpolation.Interpolate;
        floatingRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        floatingBallRenderer = floatingBall.GetComponent<Renderer>();
        if (floatingBallRenderer != null)
        {
            floatingBallRenderer.material = new Material(Shader.Find("Unlit/Color"));
            originalColor = floatingBallRenderer.material.color;
        }

        ballTrail = floatingBall.AddComponent<TrailRenderer>();
        ballTrail.time = 0.2f;
        ballTrail.startWidth = 0.05f;
        ballTrail.endWidth = 0f;
        ballTrail.material = new Material(Shader.Find("Unlit/Color"));
        ballTrail.material.color = originalColor;

        if (floatingBall.GetComponent<FloatingBallTrigger>() == null)
            floatingBall.AddComponent<FloatingBallTrigger>().Init(this);
    }

    void Update()
    {
        swingTimer += Time.deltaTime;

        Vector3 rightVelocity = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);
        if (rightVelocity.magnitude > swingThreshold && swingTimer > swingCooldown)
        {
            currentColorIndex = (currentColorIndex + 1) % ballColors.Length;
            floatingBallRenderer.material.color = ballColors[currentColorIndex];
            ballTrail.material.color = ballColors[currentColorIndex];
            originalColor = floatingBallRenderer.material.color;
            swingTimer = 0f;
        }

        isHeld = OVRInput.Get(OVRInput.Button.One);

        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            if (leftAbsorbBall == null)
            {
                leftAbsorbBall = Instantiate(absorbBallPrefab, leftHand.position + absorbOffset, Quaternion.identity);
                Collider col = leftAbsorbBall.GetComponent<Collider>();
                if (col == null) col = leftAbsorbBall.AddComponent<SphereCollider>();
                col.isTrigger = true;

                Rigidbody rbAbsorb = leftAbsorbBall.GetComponent<Rigidbody>();
                if (rbAbsorb == null) rbAbsorb = leftAbsorbBall.AddComponent<Rigidbody>();
                rbAbsorb.isKinematic = true;
            }
            else
            {
                Destroy(leftAbsorbBall);
                leftAbsorbBall = null;
            }
        }

        if (leftAbsorbBall != null)
            leftAbsorbBall.transform.position = leftHand.position + absorbOffset;
    }

    void FixedUpdate()
    {
        originalOffsetPos = playerHead.position + playerHead.forward * headOffset.z + playerHead.up * headOffset.y + playerHead.right * headOffset.x;
        Vector3 handPoint = rightHand.position + rightHand.forward * handOffset.z + Vector3.up * hoverHeight;
        Vector3 targetPos = originalOffsetPos;

        // 融合動畫
        if (isMerging && mergeTarget != null)
        {
            floatingBall.transform.position = Vector3.Lerp(floatingBall.transform.position, mergeTarget.position, Time.fixedDeltaTime * 1.5f);
            floatingBall.transform.localScale = Vector3.Lerp(floatingBall.transform.localScale, Vector3.zero, Time.fixedDeltaTime * 1.5f);
            floatingBallRenderer.material.color = Color.Lerp(originalColor, mergeTarget.GetComponent<Renderer>()?.material.color ?? Color.white, Time.fixedDeltaTime * 1.5f);

            if (Vector3.Distance(floatingBall.transform.position, mergeTarget.position) < 0.05f) // 放大距離判定
            {
                floatingBall.transform.localScale = originalScale;
                floatingBallRenderer.material.color = mergeTarget.GetComponent<Renderer>()?.material.color ?? Color.white;
                isMerging = false;

                if (mergeParticlesPrefab != null)
                {
                    ParticleSystem ps = Instantiate(mergeParticlesPrefab, mergeTarget.position, Quaternion.identity);
                    var main = ps.main;
                    main.simulationSpeed = 0.6f;
                    ps.Play();
                    Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
                }

                // 使用 Coroutine 延遲切換場景
                string sceneName = sceneByColor[currentColorIndex];
                Debug.Log("切換到場景: " + sceneName);
                StartCoroutine(LoadSceneCoroutine(sceneName));
            }
            return;
        }

        if (isHeld || isLockedAtHand)
            targetPos = handPoint;

        // 吸附球自動吸引
        if (leftAbsorbBall != null)
        {
            Vector3 toAbsorb = leftAbsorbBall.transform.position - floatingBall.transform.position;
            float distance = toAbsorb.magnitude;

            if (distance < absorbDistanceThreshold)
            {
                isBeingAbsorbed = true;
                float pullSpeed = Mathf.Lerp(2f, 6f, 1f - distance / absorbDistanceThreshold);
                floatingBall.transform.position += toAbsorb.normalized * pullSpeed * Time.fixedDeltaTime;
                floatingBall.transform.localScale = originalScale * Mathf.Lerp(0.5f, 1f, distance / absorbDistanceThreshold);

                if (absorbParticlePrefab != null && isBeingAbsorbed)
                {
                    ParticleSystem ps = Instantiate(absorbParticlePrefab, floatingBall.transform.position, Quaternion.identity);
                    var main = ps.main;
                    main.startColor = floatingBallRenderer.material.color;
                    ps.transform.LookAt(leftAbsorbBall.transform);
                    ps.Play();
                    Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
                }
            }
            else
            {
                isBeingAbsorbed = false;
            }
        }

        floatingRb.MovePosition(Vector3.SmoothDamp(floatingRb.position, targetPos, ref velocity, smoothTime));

        if (!isLockedAtHand && isHeld && Vector3.Distance(floatingRb.position, handPoint) < 0.02f)
            isLockedAtHand = true;
    }

    public void StartMerge(Transform target)
    {
        if (!isMerging)
        {
            mergeTarget = target;
            isMerging = true;
            isBeingAbsorbed = false;
        }
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        yield return new WaitForSeconds(0.1f);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
            Debug.Log("切換場景前已自動存檔");
        }

        SceneManager.LoadScene(sceneName);
    }
}

public class FloatingBallTrigger : MonoBehaviour
{
    private VRFloatingBall_Final manager;

    public void Init(VRFloatingBall_Final m)
    {
        manager = m;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("AbsorbBall"))
            manager.StartMerge(other.transform);
    }
}