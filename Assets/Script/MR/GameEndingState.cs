using UnityEngine;

public class GameEndingState : MonoBehaviour
{
    public static GameEndingState Instance;

    [Header("是否有待顯示結局")]
    public bool hasPendingEnding = false;

    [Header("結局資料")]
    public string endingTitle;
    [TextArea] public string endingDescription;
    public int totalNegative;
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

    public void SetEndingData(string title, string description, int negative, bool isDisaster)
    {
        hasPendingEnding = true;
        endingTitle = title;
        endingDescription = description;
        totalNegative = negative;
        isDisasterEnding = isDisaster;
    }

    public void ClearEndingData()
    {
        hasPendingEnding = false;
        endingTitle = "";
        endingDescription = "";
        totalNegative = 0;
        isDisasterEnding = false;
    }
}