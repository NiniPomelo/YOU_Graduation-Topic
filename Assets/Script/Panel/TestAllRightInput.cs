using UnityEngine;

public class TestAllRightInput : MonoBehaviour
{
    void Update()
    {
        // Button 測試
        if (OVRInput.GetDown(OVRInput.Button.One))
            Debug.Log("Button.One 有按到");

        if (OVRInput.GetDown(OVRInput.Button.Two))
            Debug.Log("Button.Two 有按到");

        if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
            Debug.Log("SecondaryHandTrigger 有按到");

        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
            Debug.Log("SecondaryIndexTrigger 有按到");

        // 軸值測試
        float rg = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);
        float rt = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);

        if (rg > 0.01f || rt > 0.01f)
            Debug.Log("右手軸值 -> Grip: " + rg + " Trigger: " + rt);

        // 控制器連線狀態
        if (Time.frameCount % 120 == 0)
        {
            Debug.Log("Connected Controllers: " + OVRInput.GetConnectedControllers());
        }
    }
}