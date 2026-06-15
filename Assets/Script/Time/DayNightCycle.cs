using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public Light sun;
    public float dayDuration = 120f; // 一天幾秒

    void Update()
    {
        if (sun == null) return;

        // 用全域時間
        float timeOfDay = (Time.time % dayDuration) / dayDuration;

        float sunAngle = timeOfDay * 360f - 90f;
        sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0);

        float intensity = Mathf.Clamp01(Mathf.Cos(timeOfDay * Mathf.PI * 2) * 0.5f + 0.5f);
        sun.intensity = intensity;

        RenderSettings.ambientIntensity = intensity;
    }
}