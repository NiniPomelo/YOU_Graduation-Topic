using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MRMainEndingUI : MonoBehaviour
{
    [Header("原本的資源 Panel")]
    public GameObject resourcePanel;

    [Header("結局視窗")]
    public GameObject endingPanel;

    [Header("文字")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text summaryText;

    [Header("重新開始要進的場景")]
    public string restartSceneName = "VR_Forest";

    void Start()
    {
        if (endingPanel != null)
            endingPanel.SetActive(false);

        ShowEndingIfNeeded();
    }

    void ShowEndingIfNeeded()
    {
        if (GameEndingState.Instance == null) return;
        if (!GameEndingState.Instance.hasPendingEnding) return;

        if (resourcePanel != null)
            resourcePanel.SetActive(false);

        if (endingPanel != null)
        {
            endingPanel.SetActive(true);
            endingPanel.transform.SetAsLastSibling();

            RectTransform rt = endingPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
        }

        if (titleText != null)
            titleText.text = GameEndingState.Instance.endingTitle;

        if (descriptionText != null)
            descriptionText.text = GameEndingState.Instance.endingDescription;

        if (summaryText != null)
            summaryText.text = "總負面行為值：" + GameEndingState.Instance.totalNegative;

        GameEndingState.Instance.ClearEndingData();
    }

    public void RestartGame()
    {
        if (KarmaSystem.Instance != null)
            KarmaSystem.Instance.ResetKarma();

        if (GameTimer.Instance != null)
            GameTimer.Instance.ResetTimer();

        SceneManager.LoadScene(restartSceneName);
    }

    public void BackToMain()
    {
        if (endingPanel != null)
            endingPanel.SetActive(false);

        if (resourcePanel != null)
            resourcePanel.SetActive(true);
    }
}
