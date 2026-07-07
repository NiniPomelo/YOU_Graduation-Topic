using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuBlockController : MonoBehaviour
{
    [Header("Main Scene")]
    public string mainSceneName = "MR_Main";

    private bool isLoading = false;

    public void StartGame()
    {
        if (isLoading) return;
        isLoading = true;

        if (SaveManager.Instance != null && SaveManager.Instance.HasSaveFile())
        {
            Debug.Log("Start: save found, loading previous progress.");
            SaveManager.Instance.LoadGame();
        }
        else
        {
            Debug.Log("Start: no save found, starting a new game.");

            if (GameTimer.Instance != null)
                GameTimer.Instance.ResetTimer();

            SceneManager.LoadScene(mainSceneName);
        }
    }

    public void RestartGame()
    {
        if (isLoading) return;
        isLoading = true;

        Debug.Log("Restart: delete save and start over.");

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

        SceneManager.LoadScene(mainSceneName);
    }
}