using UnityEngine;
using System.Collections;

public class OilPlatformShake : MonoBehaviour
{
    public float shakeAmount = 0.05f;
    public float duration = 1.5f;
    public float speed = 8f;

    private Vector3 startPos;
    private Coroutine shakeRoutine;

    void Start()
    {
        startPos = transform.position;
    }

    public void StartShake()
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float x = Mathf.Sin(Time.time * speed) * shakeAmount;
            float z = Mathf.Cos(Time.time * speed) * shakeAmount;

            transform.position = startPos + new Vector3(x, 0f, z);

            yield return null;
        }

        transform.position = startPos;
        shakeRoutine = null;
    }
}