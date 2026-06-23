using UnityEngine;

public class CarTrigger : MonoBehaviour
{
    public CarEnterSystem car;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            car.ShowGetInButton();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            car.HideGetInButton();
        }
    }
}