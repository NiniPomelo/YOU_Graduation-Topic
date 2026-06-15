using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuBlockController : MonoBehaviour
{
    [Header("重新開始要進入的場景")]
    public string mainSceneName = "MR_Main";

    private bool isLoading = false;

    public void StartGame()
    {
        if (isLoading) return;
        isLoading = true;

        if (SaveManager.Instance != null && SaveManager.Instance.HasSaveFile())
        {
            Debug.Log("Start：找到存檔，讀取上次進度");
            SaveManager.Instance.LoadGame();
        }
        else
        {
            Debug.Log("Start：沒有存檔，直接開始新遊戲");
            SceneManager.LoadScene(mainSceneName);
        }
    }

    public void RestartGame()
    {
        if (isLoading) return;
        isLoading = true;

        Debug.Log("Restart：刪除存檔並重新開始");

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSave();
        }

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResetAllResources();
        }

        if (KarmaSystem.Instance != null)
        {
            KarmaSystem.Instance.ResetKarma();
        }

        if (GameEndingState.Instance != null)
        {
            GameEndingState.Instance.ClearEndingData();
        }

        SceneManager.LoadScene(mainSceneName);
    }
}