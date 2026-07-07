using UnityEngine;
using UnityEngine.SceneManagement;

public enum EndingTriggerType
{
    TimeUp,
    Disaster
}

public class EndingConditionManager : MonoBehaviour
{
    public static EndingConditionManager Instance;

    [Header("Thresholds")]
    public int goodEndingMax = 499;
    public int warningEndingMax = 999;
    public int disasterThreshold = 1500;
    public float minimumDisasterElapsedYears = 10f;

    [Header("Ending Scene")]
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
        float elapsedYears = GameTimer.Instance != null ? GameTimer.Instance.ElapsedGameYears : 0f;

        if (elapsedYears >= minimumDisasterElapsedYears && totalNegative >= disasterThreshold)
            TriggerEnding(EndingTriggerType.Disaster);
    }

    public void TriggerDisasterEnding()
    {
        TriggerEnding(EndingTriggerType.Disaster);
    }

    public void TriggerTimeUpEnding()
    {
        TriggerEnding(EndingTriggerType.TimeUp);
    }

    public void TriggerEnding(EndingTriggerType triggerType)
    {
        if (gameEnded) return;
        gameEnded = true;

        int totalNegative = 0;
        int totalBeforeRestoration = 0;
        int restorationKarma = 0;
        float elapsedYears = 0f;

        if (GameTimer.Instance != null)
        {
            elapsedYears = GameTimer.Instance.ElapsedGameYears;
            GameTimer.Instance.StopTimer();
        }

        if (KarmaSystem.Instance != null)
        {
            KarmaSystem.Instance.UpdateTimeKarma(elapsedYears);
            totalNegative = KarmaSystem.Instance.GetTotalNegative();
            totalBeforeRestoration = KarmaSystem.Instance.TotalBeforeRestoration;
            restorationKarma = KarmaSystem.Instance.RestorationKarma;
        }

        string stage = GetEnvironmentalStage(totalNegative);
        string title = GetEndingTitle(triggerType, totalNegative);
        string description = GetEndingDescription(triggerType, totalNegative, stage);
        AICausalEvaluator.Result aiResult = KarmaSystem.Instance != null
            ? AICausalEvaluator.Evaluate(KarmaSystem.Instance, totalNegative, totalBeforeRestoration, restorationKarma, elapsedYears)
            : null;

        if (aiResult != null)
            description += "\n\n" + aiResult.reportText;

        bool isDisaster = triggerType == EndingTriggerType.Disaster || totalNegative >= disasterThreshold;

        if (GameEndingState.Instance != null)
        {
            GameEndingState.Instance.SetEndingData(
                triggerType.ToString(),
                title,
                description,
                stage,
                totalNegative,
                totalBeforeRestoration,
                restorationKarma,
                elapsedYears,
                isDisaster
            );
        }

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        SceneManager.LoadScene(endingSceneName);
    }

    string GetEndingTitle(EndingTriggerType triggerType, int totalNegative)
    {
        if (triggerType == EndingTriggerType.Disaster)
            return "\u751f\u614b\u5d29\u58de";

        if (totalNegative <= goodEndingMax)
            return "\u6c38\u7e8c\u5171\u751f";

        if (totalNegative <= warningEndingMax)
            return "\u751f\u614b\u5931\u8861";

        return "\u751f\u614b\u5d29\u58de";
    }

    string GetEndingDescription(EndingTriggerType triggerType, int totalNegative, string stage)
    {
        if (triggerType == EndingTriggerType.Disaster)
            return "\u56e0\u679c\u503c\u5df2\u7d93\u8d85\u904e\u74b0\u5883\u627f\u8f09\u4e0a\u9650\uff0c\u904a\u6232\u63d0\u524d\u56de\u5230MR\u7d50\u7b97\u3002\u76ee\u524d\u74b0\u5883\u968e\u6bb5\uff1a" + stage + "\u3002";

        if (totalNegative <= goodEndingMax)
            return "\u6642\u9593\u7d50\u7b97\u6642\uff0c\u4f60\u7684\u7834\u58de\u8207\u5fa9\u80b2\u884c\u70ba\u7dad\u6301\u5728\u53ef\u627f\u53d7\u7bc4\u570d\u5167\u3002\u76ee\u524d\u74b0\u5883\u968e\u6bb5\uff1a" + stage + "\u3002";

        if (totalNegative <= warningEndingMax)
            return "\u6642\u9593\u7d50\u7b97\u6642\uff0c\u74b0\u5883\u5df2\u7d93\u51fa\u73fe\u660e\u986f\u5931\u8861\uff0c\u4f46\u9084\u6c92\u6709\u8d70\u5230\u4e0d\u53ef\u633d\u56de\u3002\u76ee\u524d\u74b0\u5883\u968e\u6bb5\uff1a" + stage + "\u3002";

        return "\u6642\u9593\u7d50\u7b97\u6642\uff0c\u751f\u614b\u5df2\u7d93\u9032\u5165\u9ad8\u98a8\u96aa\u72c0\u614b\u3002\u76ee\u524d\u74b0\u5883\u968e\u6bb5\uff1a" + stage + "\u3002";
    }

    string GetEnvironmentalStage(int totalNegative)
    {
        if (totalNegative <= goodEndingMax)
            return "Stable";

        if (totalNegative <= warningEndingMax)
            return "Stressed";

        if (totalNegative < disasterThreshold)
            return "Critical";

        return "Collapse";
    }

    public void ResetEndingState()
    {
        gameEnded = false;
    }
}
