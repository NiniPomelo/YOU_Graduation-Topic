using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingConditionManager : MonoBehaviour
{
    public static EndingConditionManager Instance;

    [Header("引爆值")]
    public int disasterThreshold = 100;

    [Header("結局場景名稱")]
    public string endingSceneName = "MR_Main";

    private bool gameEnded = false;

    public bool GameEnded => gameEnded;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void CheckDisasterCondition()
    {
        if (gameEnded) return;
        if (KarmaSystem.Instance == null) return;

        int totalNegative = KarmaSystem.Instance.GetTotalNegative();

        if (totalNegative >= disasterThreshold)
        {
            TriggerDisasterEnding();
        }
    }

    public void TriggerDisasterEnding()
    {
        if (gameEnded) return;
        gameEnded = true;

        int totalNegative = 0;
        if (KarmaSystem.Instance != null)
            totalNegative = KarmaSystem.Instance.GetTotalNegative();

        if (GameTimer.Instance != null)
            GameTimer.Instance.StopTimer();

        if (GameEndingState.Instance != null)
        {
            GameEndingState.Instance.SetEndingData(
                "災難降臨",
                "你在各個環境中的破壞行為累積到失衡臨界點，最終引發了無法挽回的後果。",
                totalNegative,
                true
            );
        }

        SceneManager.LoadScene(endingSceneName);
    }

    public void TriggerTimeUpEnding()
    {
        if (gameEnded) return;
        gameEnded = true;

        int totalNegative = 0;
        if (KarmaSystem.Instance != null)
            totalNegative = KarmaSystem.Instance.GetTotalNegative();

        string title;
        string description;

        if (totalNegative < 30)
        {
            title = "表面平靜";
            description = "時間到了。雖然世界尚未立即崩壞，但你留下的影響仍會慢慢擴散。";
        }
        else if (totalNegative < 60)
        {
            title = "生態受損";
            description = "時間到了。你的行為已讓環境出現明顯損傷，失衡正在悄悄累積。";
        }
        else
        {
            title = "崩壞前夕";
            description = "時間到了。雖然災難尚未全面爆發，但世界已站在失控邊緣。";
        }

        if (GameEndingState.Instance != null)
        {
            GameEndingState.Instance.SetEndingData(
                title,
                description,
                totalNegative,
                false
            );
        }

        SceneManager.LoadScene(endingSceneName);
    }

    public void ResetEndingState()
    {
        gameEnded = false;
    }
}