using UnityEngine;

public class CarTrigger : MonoBehaviour
{
    public CarEnterSystem car;
    public Transform playerRoot;
    public string playerTag = "MainCamera";

    private void OnTriggerEnter(Collider other)
    {
        if (car != null && IsPlayer(other))
        {
            car.ShowGetInButton();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (car != null && IsPlayer(other))
        {
            car.HideGetInButton();
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag))
            return true;

        Transform otherTransform = other.transform;

        if (playerRoot != null &&
            (otherTransform == playerRoot || otherTransform.IsChildOf(playerRoot) || playerRoot.IsChildOf(otherTransform)))
        {
            return true;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        Transform cameraTransform = mainCamera.transform;
        return otherTransform == cameraTransform ||
               otherTransform.IsChildOf(cameraTransform) ||
               cameraTransform.IsChildOf(otherTransform);
    }
}
