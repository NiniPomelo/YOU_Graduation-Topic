using System.Collections;
using UnityEngine;

public class TreeGrowthController : MonoBehaviour
{
    [Header("Growth Stages")]
    public GameObject sprout;
    public GameObject middleTree;
    public GameObject bigTree;

    [Header("Effects")]
    public GameObject smokePrefab;
    public float scaleDuration = 0.5f;

    [Header("Audio")]
    public AudioClip growthSound;

    [Range(0f, 1f)]
    public float soundVolume = 0.8f;

    [Header("Karma")]
    public int bigTreeRestorationValue = 10;

    private const float SproutTime = 10f;
    private const float BigTreeTime = 25f;

    [SerializeField]
    private float elapsedTime;

    private Coroutine growthCoroutine;
    private bool initialized;
    private bool hasAwardedBigTreeRestoration;

    private void Start()
    {
        Invoke(nameof(InitializeGrowth), 0f);
    }

    private void InitializeGrowth()
    {
        initialized = true;
        RestoreTreeState();
        StartGrowthIfNeeded();
    }

    public float GetElapsedTime()
    {
        return elapsedTime;
    }

    public void SetElapsedTime(float time)
    {
        elapsedTime = Mathf.Clamp(time, 0f, BigTreeTime);

        RestoreTreeState();

        if (growthCoroutine != null)
        {
            StopCoroutine(growthCoroutine);
            growthCoroutine = null;
        }

        if (initialized)
            StartGrowthIfNeeded();
    }

    private void StartGrowthIfNeeded()
    {
        if (growthCoroutine == null && elapsedTime < BigTreeTime)
            growthCoroutine = StartCoroutine(GrowthSequence());
    }

    private void RestoreTreeState()
    {
        if (sprout != null)
            sprout.SetActive(false);

        if (middleTree != null)
            middleTree.SetActive(false);

        if (bigTree != null)
            bigTree.SetActive(false);

        if (elapsedTime >= BigTreeTime)
        {
            if (bigTree != null)
                bigTree.SetActive(true);
        }
        else if (elapsedTime >= SproutTime)
        {
            if (middleTree != null)
                middleTree.SetActive(true);
        }
        else
        {
            if (sprout != null)
                sprout.SetActive(true);
        }
    }

    private IEnumerator GrowthSequence()
    {
        float saveTimer = 0f;

        while (elapsedTime < SproutTime)
        {
            elapsedTime += Time.deltaTime;
            saveTimer += Time.deltaTime;

            if (saveTimer >= 1f)
            {
                saveTimer = 0f;

                if (SaveManager.Instance != null)
                    SaveManager.Instance.SaveGame();
            }

            yield return null;
        }

        if (sprout != null)
            sprout.SetActive(false);

        if (middleTree != null && !middleTree.activeSelf)
        {
            TriggerEffects(middleTree.transform.position);
            yield return StartCoroutine(ScaleUpObject(middleTree));

            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();
        }

        while (elapsedTime < BigTreeTime)
        {
            elapsedTime += Time.deltaTime;
            saveTimer += Time.deltaTime;

            if (saveTimer >= 1f)
            {
                saveTimer = 0f;

                if (SaveManager.Instance != null)
                    SaveManager.Instance.SaveGame();
            }

            yield return null;
        }

        if (middleTree != null)
            middleTree.SetActive(false);

        if (bigTree != null && !bigTree.activeSelf)
        {
            TriggerEffects(bigTree.transform.position);
            yield return StartCoroutine(ScaleUpObject(bigTree));

            AwardBigTreeRestoration();

            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();
        }
    }

    private void AwardBigTreeRestoration()
    {
        if (hasAwardedBigTreeRestoration)
            return;

        hasAwardedBigTreeRestoration = true;

        if (KarmaSystem.Instance != null)
            KarmaSystem.Instance.AddRestorationKarma(bigTreeRestorationValue);
    }

    private void TriggerEffects(Vector3 position)
    {
        if (smokePrefab != null)
            Instantiate(smokePrefab, position, Quaternion.identity);

        if (growthSound != null)
            AudioSource.PlayClipAtPoint(growthSound, position, soundVolume);
    }

    private IEnumerator ScaleUpObject(GameObject target)
    {
        target.SetActive(true);

        Vector3 targetScale = target.transform.localScale;
        target.transform.localScale = Vector3.zero;

        float currentTime = 0f;

        while (currentTime < scaleDuration)
        {
            currentTime += Time.deltaTime;
            target.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, currentTime / scaleDuration);
            yield return null;
        }

        target.transform.localScale = targetScale;
    }

    private void OnDestroy()
    {
        if (growthCoroutine != null)
        {
            StopCoroutine(growthCoroutine);
            growthCoroutine = null;
        }
    }
}
