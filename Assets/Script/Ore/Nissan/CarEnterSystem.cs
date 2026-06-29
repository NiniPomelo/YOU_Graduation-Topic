using UnityEngine;

public class CarEnterSystem : MonoBehaviour
{
    public Transform driverSeat;
    public Transform exitPoint;

    public GameObject getInButton;
    public GameObject getOffButton;

    public Camera playerCamera;
    public Transform playerRoot;
    public bool enableControllerFallback = true;
    public OVRInput.Button controllerButton = OVRInput.Button.PrimaryIndexTrigger;
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;

    [Header("Car View")]
    public Vector3 driverViewPositionOffset;
    public Vector3 driverViewRotationOffset;

    [Header("Driving")]
    public bool enableCarDriving = true;
    public float driveSpeed = 6f;
    public float reverseSpeed = 3f;
    public float turnSpeed = 60f;
    public float inputDeadZone = 0.15f;
    public OVRInput.Controller throttleController = OVRInput.Controller.LTouch;
    public OVRInput.Axis2D throttleAxis = OVRInput.Axis2D.PrimaryThumbstick;
    public OVRInput.Controller steeringController = OVRInput.Controller.RTouch;
    public OVRInput.Axis2D steeringAxis = OVRInput.Axis2D.PrimaryThumbstick;

    [Header("Ground Follow")]
    public LayerMask groundLayerMask = ~0;
    public float groundRaycastHeight = 3f;
    public float groundRaycastDistance = 8f;
    public float groundOffset = 0.2f;
    public float groundAlignSpeed = 12f;

    [Header("Disable While Driving")]
    public bool autoDisablePlayerScripts = true;
    public MonoBehaviour[] disableWhileInCar;

    private Vector3 originalRootPosition;
    private Quaternion originalRootRotation;
    private bool[] disableWhileInCarOriginalStates;
    private float currentCarYaw;
    private Vector3 currentGroundNormal = Vector3.up;

    bool inCar = false;

    void Start()
    {
        getInButton.SetActive(false);
        getOffButton.SetActive(false);
    }

    void Update()
    {
        if (inCar)
        {
            HandleCarDriving();
            MovePlayerTo(driverSeat);
        }

        if (!enableControllerFallback)
            return;

        if (!OVRInput.GetDown(controllerButton, controller))
            return;

        if (!inCar && getInButton.activeSelf)
        {
            GetIn();
        }
        else if (inCar && getOffButton.activeSelf)
        {
            GetOff();
        }
    }

    public void ShowGetInButton()
    {
        if (!inCar)
            getInButton.SetActive(true);
    }

    public void HideGetInButton()
    {
        getInButton.SetActive(false);
    }

    public void GetIn()
    {
        Transform root = GetPlayerRoot();
        originalRootPosition = root.position;
        originalRootRotation = root.rotation;
        currentCarYaw = transform.eulerAngles.y;

        SnapCarToGround(true);
        MovePlayerTo(driverSeat);
        SetPlayerControlsEnabled(false);

        getInButton.SetActive(false);
        getOffButton.SetActive(true);

        inCar = true;
    }

    public void GetOff()
    {
        MovePlayerTo(exitPoint);
        SetPlayerControlsEnabled(true);

        getOffButton.SetActive(false);

        inCar = false;
    }

    private void HandleCarDriving()
    {
        if (!enableCarDriving)
            return;

        float forwardInput = ApplyDeadZone(OVRInput.Get(throttleAxis, throttleController).y);
        float turnInput = ApplyDeadZone(OVRInput.Get(steeringAxis, steeringController).x);

        if (Mathf.Abs(forwardInput) > 0.01f)
        {
            float turnDirection = forwardInput >= 0f ? 1f : -1f;
            currentCarYaw += turnInput * turnSpeed * turnDirection * Time.deltaTime;
        }

        Quaternion yawRotation = Quaternion.Euler(0f, currentCarYaw, 0f);
        Vector3 driveDirection = Vector3.ProjectOnPlane(yawRotation * Vector3.forward, currentGroundNormal).normalized;

        float speed = forwardInput >= 0f ? driveSpeed : reverseSpeed;
        transform.position += driveDirection * forwardInput * speed * Time.deltaTime;

        SnapCarToGround(false);
    }

    private float ApplyDeadZone(float value)
    {
        return Mathf.Abs(value) < inputDeadZone ? 0f : value;
    }

    private Transform GetPlayerRoot()
    {
        if (playerRoot != null)
            return playerRoot;

        CharacterController characterController = playerCamera.GetComponentInParent<CharacterController>();
        if (characterController != null)
            return characterController.transform;

        OVRCameraRig cameraRig = playerCamera.GetComponentInParent<OVRCameraRig>();
        if (cameraRig != null)
            return cameraRig.transform;

        return playerCamera.transform;
    }

    private void MovePlayerTo(Transform target)
    {
        if (target == null || playerCamera == null)
            return;

        Transform root = GetPlayerRoot();
        CharacterController characterController = root.GetComponent<CharacterController>();
        bool wasCharacterControllerEnabled = characterController != null && characterController.enabled;

        if (characterController != null)
            characterController.enabled = false;

        Quaternion targetRotation = target.rotation * Quaternion.Euler(driverViewRotationOffset);
        Vector3 targetPosition = target.TransformPoint(driverViewPositionOffset);

        root.rotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
        root.position += targetPosition - playerCamera.transform.position;

        if (characterController != null)
            characterController.enabled = wasCharacterControllerEnabled;
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        if (autoDisablePlayerScripts)
        {
            SetComponentEnabled<VRPlayerController>(enabled);
            SetComponentEnabled<VRRayGrabOre>(enabled);
        }

        if (disableWhileInCar == null)
            return;

        if (!enabled)
        {
            disableWhileInCarOriginalStates = new bool[disableWhileInCar.Length];

            for (int i = 0; i < disableWhileInCar.Length; i++)
            {
                if (disableWhileInCar[i] == null)
                    continue;

                disableWhileInCarOriginalStates[i] = disableWhileInCar[i].enabled;
                disableWhileInCar[i].enabled = false;
            }
        }
        else if (disableWhileInCarOriginalStates != null)
        {
            for (int i = 0; i < disableWhileInCar.Length && i < disableWhileInCarOriginalStates.Length; i++)
            {
                if (disableWhileInCar[i] != null)
                    disableWhileInCar[i].enabled = disableWhileInCarOriginalStates[i];
            }
        }
    }

    private void SetComponentEnabled<T>(bool enabled) where T : MonoBehaviour
    {
        Transform root = GetPlayerRoot();
        T component = root.GetComponentInChildren<T>(true);

        if (component == null)
            component = FindFirstObjectByType<T>();

        if (component != null && component != this)
            component.enabled = enabled;
    }

    private void SnapCarToGround(bool instant)
    {
        RaycastHit groundHit;
        if (!TryGetGroundHit(out groundHit))
            return;

        currentGroundNormal = groundHit.normal;

        Vector3 targetPosition = groundHit.point + currentGroundNormal * groundOffset;
        transform.position = instant
            ? targetPosition
            : Vector3.Lerp(transform.position, targetPosition, groundAlignSpeed * Time.deltaTime);

        Quaternion yawRotation = Quaternion.Euler(0f, currentCarYaw, 0f);
        Vector3 forward = Vector3.ProjectOnPlane(yawRotation * Vector3.forward, currentGroundNormal).normalized;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(transform.forward, currentGroundNormal).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(forward, currentGroundNormal);
        transform.rotation = instant
            ? targetRotation
            : Quaternion.Slerp(transform.rotation, targetRotation, groundAlignSpeed * Time.deltaTime);
    }

    private bool TryGetGroundHit(out RaycastHit groundHit)
    {
        Vector3 rayOrigin = transform.position + Vector3.up * groundRaycastHeight;
        float rayDistance = groundRaycastHeight + groundRaycastDistance;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, rayDistance, groundLayerMask, QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                continue;

            groundHit = hit;
            return true;
        }

        groundHit = default;
        return false;
    }
}
