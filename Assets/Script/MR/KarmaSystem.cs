using UnityEngine;

public class KarmaSystem : MonoBehaviour
{
    public static KarmaSystem Instance;

    [Header("­t­±­È")]
    public int forestNegative;
    public int oceanNegative;
    public int mineNegative;

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

    public void AddForestNegative(int amount)
    {
        forestNegative += amount;
        NotifyValueChanged();
    }

    public void AddOceanNegative(int amount)
    {
        oceanNegative += amount;
        NotifyValueChanged();
    }

    public void AddMineNegative(int amount)
    {
        mineNegative += amount;
        NotifyValueChanged();
    }

    public int GetTotalNegative()
    {
        return forestNegative + oceanNegative + mineNegative;
    }

    void NotifyValueChanged()
    {
        if (EndingConditionManager.Instance != null)
            EndingConditionManager.Instance.CheckDisasterCondition();
    }

    public void ResetKarma()
    {
        forestNegative = 0;
        oceanNegative = 0;
        mineNegative = 0;
    }
}