using UnityEngine;

public class CarEnterSystem : MonoBehaviour
{
    public Transform driverSeat;
    public Transform exitPoint;

    public GameObject getInButton;
    public GameObject getOffButton;

    public Camera playerCamera;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    bool inCar = false;

    void Start()
    {
        getInButton.SetActive(false);
        getOffButton.SetActive(false);
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
        originalPosition = playerCamera.transform.position;
        originalRotation = playerCamera.transform.rotation;

        playerCamera.transform.position = driverSeat.position;
        playerCamera.transform.rotation = driverSeat.rotation;

        getInButton.SetActive(false);
        getOffButton.SetActive(true);

        inCar = true;
    }

    public void GetOff()
    {
        playerCamera.transform.position = exitPoint.position;
        playerCamera.transform.rotation = exitPoint.rotation;

        getOffButton.SetActive(false);

        inCar = false;
    }
}