using UnityEngine;
using Meta.XR;

public class VRPlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 2f;

    [Header("Rotation")]
    public float rotateSpeed = 60f;

    [Header("Gravity")]
    public float gravity = -9.8f;

    CharacterController controller;
    Transform head;

    float verticalVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        head = GetComponentInChildren<Camera>().transform;
    }

    void Update()
    {
        MovePlayer();
        RotatePlayer();
        ApplyGravity();
    }

    void MovePlayer()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        Vector3 forward = head.forward;
        Vector3 right = head.right;

        forward.y = 0;
        right.y = 0;

        Vector3 direction = forward * input.y + right * input.x;

        controller.Move(direction * moveSpeed * Time.deltaTime);
    }

    void RotatePlayer()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        float rotation = input.x * rotateSpeed * Time.deltaTime;

        transform.Rotate(0, rotation, 0);
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 gravityMove = new Vector3(0, verticalVelocity, 0);
        controller.Move(gravityMove * Time.deltaTime);
    }
}