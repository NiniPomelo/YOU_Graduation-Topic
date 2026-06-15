using UnityEngine;

public class HandTouchTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        Start,
        Restart
    }

    [Header("這顆方塊是哪種功能")]
    public TriggerType triggerType;

    [Header("主選單控制器")]
    public MainMenuBlockController menuController;

    [Header("避免連續觸發")]
    public float cooldown = 1f;

    private float lastTriggerTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - lastTriggerTime < cooldown) return;

        if (!other.CompareTag("Hand")) return;

        lastTriggerTime = Time.time;

        Debug.Log($"碰到方塊：{triggerType}，碰撞物件：{other.name}");

        if (menuController == null)
        {
            Debug.LogWarning("menuController 沒有指定！");
            return;
        }

        switch (triggerType)
        {
            case TriggerType.Start:
                menuController.StartGame();
                break;

            case TriggerType.Restart:
                menuController.RestartGame();
                break;
        }
    }
}