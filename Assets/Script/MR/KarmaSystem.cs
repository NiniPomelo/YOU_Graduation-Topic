using UnityEngine;

public class KarmaSystem : MonoBehaviour
{
    public static KarmaSystem Instance;

    [Header("Negative Karma")]
    public int forestNegative;
    public int oceanNegative;
    public int mineNegative;
    public int constructionKarma;
    public int timeKarma;

    [Header("Restoration Karma")]
    public int restorationKarma;

    [Header("Time Karma Counters")]
    public int choppedTreeCount;
    public int oilExtractedCount;
    public int gasExtractedCount;
    public int houseCount;
    public int factoryCount;

    public int TotalBeforeRestoration => forestNegative + oceanNegative + mineNegative + constructionKarma + timeKarma;
    public int RestorationKarma => restorationKarma;

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

    public void AddChoppedTree()
    {
        choppedTreeCount++;
        AddForestNegative(10);
    }

    public void AddResourceKarma(string resourceName, int amount)
    {
        if (amount <= 0) return;

        resourceName = NormalizeResourceName(resourceName);

        switch (resourceName)
        {
            case "Wood":
                AddForestNegative(amount * 2);
                break;
            case "Sand":
                AddMineNegative(amount * 1);
                break;
            case "Limestone":
                AddMineNegative(amount * 2);
                break;
            case "IronOre":
                AddMineNegative(amount * 3);
                break;
            case "Marble":
                AddMineNegative(amount * 4);
                break;
            case "Gas":
                gasExtractedCount += amount;
                AddOceanNegative(amount * 6);
                break;
            case "Oil":
                oilExtractedCount += amount;
                AddOceanNegative(amount * 8);
                break;
        }
    }

    public void AddConstructionKarma(string objectName, int amount = 1)
    {
        if (amount <= 0) return;

        objectName = NormalizeResourceName(objectName);

        switch (objectName)
        {
            case "House":
                houseCount += amount;
                constructionKarma += amount * 15;
                NotifyValueChanged();
                break;
            case "Factory":
                factoryCount += amount;
                constructionKarma += amount * 25;
                NotifyValueChanged();
                break;
        }
    }

    public void AddRestorationKarma(int amount)
    {
        if (amount <= 0) return;

        restorationKarma += amount;
        NotifyValueChanged();
    }

    public void UpdateTimeKarma(float elapsedGameYears)
    {
        elapsedGameYears = Mathf.Max(0f, elapsedGameYears);

        int updatedTimeKarma = Mathf.FloorToInt(
            factoryCount * 2f * elapsedGameYears +
            oilExtractedCount * 1f * elapsedGameYears +
            gasExtractedCount * 1f * elapsedGameYears +
            choppedTreeCount * 0.5f * elapsedGameYears
        );

        if (updatedTimeKarma == timeKarma)
            return;

        timeKarma = updatedTimeKarma;
        NotifyValueChanged();
    }

    public void AddForestNegative(int amount)
    {
        forestNegative += Mathf.Max(0, amount);
        NotifyValueChanged();
    }

    public void AddOceanNegative(int amount)
    {
        oceanNegative += Mathf.Max(0, amount);
        NotifyValueChanged();
    }

    public void AddMineNegative(int amount)
    {
        mineNegative += Mathf.Max(0, amount);
        NotifyValueChanged();
    }

    public int GetTotalNegative()
    {
        return Mathf.Max(0, TotalBeforeRestoration - restorationKarma);
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
        constructionKarma = 0;
        timeKarma = 0;
        restorationKarma = 0;
        choppedTreeCount = 0;
        oilExtractedCount = 0;
        gasExtractedCount = 0;
        houseCount = 0;
        factoryCount = 0;
    }

    private string NormalizeResourceName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string normalized = value.Replace("(Clone)", "").Trim();

        if (normalized == "Iron Ore")
            return "IronOre";

        if (normalized == "Factory 1")
            return "Factory";

        if (normalized.StartsWith("House"))
            return "House";

        if (normalized.StartsWith("Factory"))
            return "Factory";

        return normalized;
    }
}