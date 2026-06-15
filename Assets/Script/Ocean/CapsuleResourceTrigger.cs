using UnityEngine;
using System.Collections;

public class CapsuleButtonDistanceWithCooldown : MonoBehaviour
{
    [Header("資源系統")]
    public OceanResourceSystem resourceSystem;

    [Header("平台震動")]
    public OilPlatformShake platformShake;

    [Header("手部")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("偵測")]
    public float detectRadius = 0.4f;
    public float triggerCooldown = 0.2f;

    private bool leftReady = true;
    private bool rightReady = true;

    void Update()
    {
        Check(leftHand, true);
        Check(rightHand, false);
    }

    void Check(Transform hand, bool isLeftHand)
    {
        if (hand == null) return;

        if (isLeftHand && !leftReady) return;
        if (!isLeftHand && !rightReady) return;

        float dist = Vector3.Distance(hand.position, transform.position);

        if (dist <= detectRadius)
        {
            Trigger();

            if (isLeftHand)
            {
                leftReady = false;
                StartCoroutine(Cooldown(true));
            }
            else
            {
                rightReady = false;
                StartCoroutine(Cooldown(false));
            }
        }
    }

    IEnumerator Cooldown(bool isLeftHand)
    {
        yield return new WaitForSeconds(triggerCooldown);

        if (isLeftHand)
            leftReady = true;
        else
            rightReady = true;
    }

    void Trigger()
    {
        Debug.Log("Ocean Capsule Trigger!");

        // 加在這裡（觸發資源 = 負面行為）
        if (KarmaSystem.Instance != null)
            KarmaSystem.Instance.AddOceanNegative(1);

        if (resourceSystem != null)
            resourceSystem.GenerateResources();
        else
            Debug.LogWarning("resourceSystem 沒有指定！");

        if (platformShake != null)
            platformShake.StartShake();
        else
            Debug.LogWarning("platformShake 沒有指定！");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}