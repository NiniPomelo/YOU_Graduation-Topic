using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;

    [Header("Total Time Seconds")]
    public float totalTime = 6000f;

    [Header("Time Scale")]
    public float gameYearsPerRealMinute = 1f;

    [Header("UI Optional")]
    public TMP_Text timerText;
    public string timerTextObjectName = "TimerText";

    private float currentTime;
    private bool isRunning = true;
    private bool timeUpTriggered = false;

    public float CurrentTime => currentTime;
    public bool IsRunning => isRunning;
    public float ElapsedRealSeconds => Mathf.Clamp(totalTime - currentTime, 0f, totalTime);
    public float ElapsedGameYears => ElapsedRealSeconds / 60f * gameYearsPerRealMinute;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void Start()
    {
        if (currentTime <= 0f)
            currentTime = totalTime;

        FindTimerTextIfNeeded();
        UpdateTimerUI();
        UpdateTimeKarma();
    }

    void Update()
    {
        FindTimerTextIfNeeded();

        if (!isRunning)
        {
            UpdateTimerUI();
            return;
        }

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isRunning = false;
            UpdateTimerUI();
            UpdateTimeKarma();

            if (!timeUpTriggered && EndingConditionManager.Instance != null)
            {
                timeUpTriggered = true;
                EndingConditionManager.Instance.TriggerTimeUpEnding();
            }

            return;
        }

        UpdateTimerUI();
        UpdateTimeKarma();
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;

        int elapsedMinutes = Mathf.FloorToInt(ElapsedRealSeconds / 60f);
        int elapsedSeconds = Mathf.FloorToInt(ElapsedRealSeconds % 60f);
        int years = Mathf.FloorToInt(ElapsedGameYears);
        int maxYears = Mathf.RoundToInt(gameYearsPerRealMinute * totalTime / 60f);
        timerText.text = $"Year {years:00} / {maxYears:00}  Time {elapsedMinutes:00}:{elapsedSeconds:00}";
    }

    void UpdateTimeKarma()
    {
        if (KarmaSystem.Instance != null)
            KarmaSystem.Instance.UpdateTimeKarma(ElapsedGameYears);
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindTimerTextIfNeeded(true);
        UpdateTimerUI();
    }

    void FindTimerTextIfNeeded(bool force = false)
    {
        if (!force && timerText != null) return;

        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null) continue;

            if (texts[i].gameObject.name == timerTextObjectName ||
                texts[i].gameObject.name.ToLower().Contains("timer"))
            {
                timerText = texts[i];
                return;
            }
        }
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        currentTime = totalTime;
        isRunning = true;
        timeUpTriggered = false;
        UpdateTimerUI();
        UpdateTimeKarma();
    }

    public void SetCurrentTime(float remainingTime, bool running)
    {
        currentTime = Mathf.Clamp(remainingTime, 0f, totalTime);
        isRunning = running && currentTime > 0f;
        timeUpTriggered = currentTime <= 0f;
        UpdateTimerUI();
        UpdateTimeKarma();
    }
}
