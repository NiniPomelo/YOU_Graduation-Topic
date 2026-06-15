using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("進入的主場景名稱")]
    public string mainSceneName = "MR_Main";

    public void OnClickStart()
    {
        // 還沒做存檔前：
        // Start = 保留目前 ResourceManager 內已有的資料，直接進場
        Debug.Log("Start：沿用目前進度");
        SceneManager.LoadScene(mainSceneName);
    }

    public void OnClickRestart()
    {
        // Restart = 清空所有資源，再重新開始
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResetAllResources();
        }

        Debug.Log("Restart：重新開始");
        SceneManager.LoadScene(mainSceneName);
    }
}