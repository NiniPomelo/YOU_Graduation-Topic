using UnityEngine;
using System.Collections;

public class CapsuleButtonDistanceWithCooldown : MonoBehaviour
{
    [Header("Resource System")]
    public OceanResourceSystem resourceSystem;

    [Header("Platform Shake")]
    public OilPlatformShake platformShake;

    [Header("Hands")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("Detection")]
    public float detectRadius = 0.4f;
    public float triggerCooldown = 0.2f;
    public float resetDistanceBuffer = 0.05f;

    private bool leftReady = true;
    private bool rightReady = true;
    private bool leftInside = false;
    private bool rightInside = false;
    private float nextTriggerTime = 0f;

    void Update()
    {
        Check(leftHand, true);
        Check(rightHand, false);
    }

    void Check(Transform hand, bool isLeftHand)
    {
        if (hand == null) return;

        bool isInside = isLeftHand ? leftInside : rightInside;
        float dist = Vector3.Distance(hand.position, transform.position);
        float resetDistance = detectRadius + resetDistanceBuffer;

        if (isInside)
        {
            if (dist > resetDistance)
            {
                if (isLeftHand)
                    leftInside = false;
                else
                    rightInside = false;
            }

            return;
        }

        if (isLeftHand && !leftReady) return;
        if (!isLeftHand && !rightReady) return;

        if (dist <= detectRadius)
        {
            if (isLeftHand)
                leftInside = true;
            else
                rightInside = true;

            if (Time.time >= nextTriggerTime)
            {
                Trigger();
                nextTriggerTime = Time.time + triggerCooldown;
            }

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

        if (resourceSystem != null)
            resourceSystem.GenerateResources();
        else
            Debug.LogWarning("resourceSystem is not assigned.");

        if (platformShake != null)
            platformShake.StartShake();
        else
            Debug.LogWarning("platformShake is not assigned.");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}