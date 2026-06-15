using UnityEngine;

public class RockMining : MonoBehaviour
{
    public GameObject sandPrefab;
    public GameObject ironPrefab;
    public GameObject limestonePrefab;
    public GameObject marblePrefab;

    public int minDrop = 1;
    public int maxDrop = 3;
    public float dropRadius = 0.3f;

    private int hp = 3;

    // 新增：記錄最後一次敲擊位置
    private Vector3 lastHitPoint;

    // 傳入碰撞點
    public void HitRock(Vector3 hitPoint)
    {
        lastHitPoint = hitPoint;

        hp--;

        if (hp <= 0)
        {
            if (KarmaSystem.Instance != null)
                KarmaSystem.Instance.AddMineNegative(1);

            MineRock();

            hp = 3;
        }
    }

    void MineRock()
    {
        // 改成從敲擊點生成
        Vector3 spawnPos = lastHitPoint;

        for (int i = 0; i < Random.Range(minDrop, maxDrop + 1); i++)
        {
            GameObject[] ores =
            {
                sandPrefab,
                ironPrefab,
                limestonePrefab,
                marblePrefab
            };

            GameObject ore = ores[Random.Range(0, ores.Length)];

            Vector3 offset = new Vector3(
                Random.Range(-dropRadius, dropRadius),
                Random.Range(0f, 0.2f),
                Random.Range(-dropRadius, dropRadius)
            );

            GameObject spawned =
                Instantiate(ore, spawnPos + offset, Quaternion.identity);

            Rigidbody rb = spawned.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.AddForce(
                    Vector3.up * Random.Range(1f, 2f),
                    ForceMode.Impulse
                );
            }
        }
    }
}