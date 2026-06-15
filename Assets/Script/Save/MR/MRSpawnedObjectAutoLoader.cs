using UnityEngine;

public class MRSpawnedObjectAutoLoader : MonoBehaviour
{
    private void Start()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadSpawnedObjectsOnly();
        }
    }
}