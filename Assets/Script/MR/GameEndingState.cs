using UnityEngine;

public class GameEndingState : MonoBehaviour
{
    public static GameEndingState Instance;

    [Header("Pending Ending")]
    public bool hasPendingEnding = false;

    [Header("Ending Data")]
    public string endingType;
    public string endingTitle;
    [TextArea] public string endingDescription;
    public string environmentalStage;
    public int totalNegative;
    public int totalBeforeRestoration;
    public int restorationKarma;
    public float elapsedGameYears;
    public bool isDisasterEnding;

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

    public void SetEndingData(
        string type,
        string title,
        string description,
        string stage,
        int negative,
        int beforeRestoration,
        int restoration,
        float elapsedYears,
        bool isDisaster)
    {
        hasPendingEnding = true;
        endingType = type;
        endingTitle = title;
        endingDescription = description;
        environmentalStage = stage;
        totalNegative = negative;
        totalBeforeRestoration = beforeRestoration;
        restorationKarma = restoration;
        elapsedGameYears = elapsedYears;
        isDisasterEnding = isDisaster;
    }

    public void SetEndingData(string title, string description, int negative, bool isDisaster)
    {
        SetEndingData(
            isDisaster ? "Disaster" : "TimeUp",
            title,
            description,
            "",
            negative,
            negative,
            0,
            0f,
            isDisaster
        );
    }

    public void ClearEndingData()
    {
        hasPendingEnding = false;
        endingType = "";
        endingTitle = "";
        endingDescription = "";
        environmentalStage = "";
        totalNegative = 0;
        totalBeforeRestoration = 0;
        restorationKarma = 0;
        elapsedGameYears = 0f;
        isDisasterEnding = false;
    }
}