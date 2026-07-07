using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Main Scene")]
    public string mainSceneName = "MR_Main";

    public void OnClickStart()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.HasSaveFile())
        {
            Debug.Log("Start: save found, loading previous progress.");
            SaveManager.Instance.LoadGame();
            return;
        }

        if (GameTimer.Instance != null)
            GameTimer.Instance.ResetTimer();

        Debug.Log("Start: no save found, starting a new game.");
        SceneManager.LoadScene(mainSceneName);
    }

    public void OnClickRestart()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.DeleteSave();

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.ResetAllResources();

        if (KarmaSystem.Instance != null)
            KarmaSystem.Instance.ResetKarma();

        if (GameEndingState.Instance != null)
            GameEndingState.Instance.ClearEndingData();

        if (GameTimer.Instance != null)
            GameTimer.Instance.ResetTimer();

        Debug.Log("Restart: start over.");
        SceneManager.LoadScene(mainSceneName);
    }
}