using UnityEngine;

public class VRHandPanelSystem : MonoBehaviour
{
    public Transform leftHand;
    public GameObject panel;

    public Vector3 offset = new Vector3(0, 0.1f, 0.1f);

    void Update()
    {
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger))
        {
            panel.SetActive(true);

            panel.transform.position =
                leftHand.position +
                leftHand.TransformDirection(offset);

            panel.transform.LookAt(Camera.main.transform);
            panel.transform.Rotate(0, 180, 0);
        }
        else
        {
            panel.SetActive(false);
        }
    }
}