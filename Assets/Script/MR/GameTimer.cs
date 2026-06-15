using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;

    [Header("總時間（秒）")]
    public float totalTime = 300f;

    [Header("UI（可不填）")]
    public TMP_Text timerText;

    private float currentTime;
    private bool isRunning = true;
    private bool timeUpTriggered = false;

    public float CurrentTime => currentTime;
    public bool IsRunning => isRunning;

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

    void Start()
    {
        currentTime = totalTime;
        UpdateTimerUI();
    }

    void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isRunning = false;
            UpdateTimerUI();

            if (!timeUpTriggered && EndingConditionManager.Instance != null)
            {
                timeUpTriggered = true;
                EndingConditionManager.Instance.TriggerTimeUpEnding();
            }

            return;
        }

        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
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
    }
}