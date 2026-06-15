using UnityEngine;
using TMPro;

public class OceanUIBinder : MonoBehaviour
{
    public TMP_Text oilText;
    public TMP_Text gasText;

    void Update()
    {
        if (ResourceManager.Instance == null) return;

        oilText.text = ResourceManager.Instance.GetResource("Oil").ToString();
        gasText.text = ResourceManager.Instance.GetResource("Gas").ToString();
    }
}