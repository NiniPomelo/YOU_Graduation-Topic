using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    public GameObject treePrefab;
    public Transform plane;
    public int treeCount = 10;

    void Start()
    {
        for (int i = 0; i < treeCount; i++)
        {
            SpawnTree();
        }
    }

    void SpawnTree()
    {
        Vector3 size = plane.localScale * 5;

        float x = Random.Range(-size.x, size.x);
        float z = Random.Range(-size.z, size.z);

        Vector3 pos =
            new Vector3(
                plane.position.x + x,
                plane.position.y,
                plane.position.z + z
            );

        Instantiate(treePrefab, pos, Quaternion.identity);
    }
}