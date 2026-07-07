using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class AICausalEvaluator
{
    public class Result
    {
        public string riskLevel;
        public string mainCause;
        public string explanation;
        public string recommendation;
        public string reportText;
    }

    private struct Factor
    {
        public string name;
        public int value;

        public Factor(string name, int value)
        {
            this.name = name;
            this.value = value;
        }
    }

    public static Result Evaluate(KarmaSystem karma, int totalNegative, int totalBeforeRestoration, int restorationKarma, float elapsedGameYears)
    {
        Result result = new Result();

        if (karma == null)
        {
            result.riskLevel = "Unknown";
            result.mainCause = "資料不足";
            result.explanation = "AI 因果分析無法執行，因為目前找不到 KarmaSystem。";
            result.recommendation = "請確認 MR runtime managers 已在結算前載入。";
            result.reportText = BuildReport(result);
            return result;
        }

        List<Factor> factors = new List<Factor>
        {
            new Factor("森林破壞", karma.forestNegative),
            new Factor("海洋開採", karma.oceanNegative),
            new Factor("礦產開採", karma.mineNegative),
            new Factor("建設負荷", karma.constructionKarma),
            new Factor("長期排放", karma.timeKarma)
        };

        factors.Sort((left, right) => right.value.CompareTo(left.value));

        Factor primary = factors.Count > 0 ? factors[0] : new Factor("No major cause", 0);
        float restorationRatio = totalBeforeRestoration > 0 ? Mathf.Clamp01((float)restorationKarma / totalBeforeRestoration) : 0f;

        result.riskLevel = PredictRiskLevel(totalNegative, elapsedGameYears, restorationRatio);
        result.mainCause = primary.name;
        result.explanation = BuildExplanation(factors, totalBeforeRestoration, restorationKarma, restorationRatio);
        result.recommendation = BuildRecommendation(primary, karma, restorationRatio);
        result.reportText = BuildReport(result);

        return result;
    }

    private static string PredictRiskLevel(int totalNegative, float elapsedGameYears, float restorationRatio)
    {
        float riskScore = totalNegative;
        riskScore += Mathf.Max(0f, elapsedGameYears - 10f) * 12f;
        riskScore -= restorationRatio * 180f;

        if (riskScore < 500f)
            return "低風險";

        if (riskScore < 1000f)
            return "中風險";

        if (riskScore < 1500f)
            return "高風險";

        return "崩壞風險";
    }

    private static string BuildExplanation(List<Factor> factors, int totalBeforeRestoration, int restorationKarma, float restorationRatio)
    {
        StringBuilder builder = new StringBuilder();
        int denominator = Mathf.Max(1, totalBeforeRestoration);
        int shown = Mathf.Min(3, factors.Count);

        builder.Append("主要因果來源：");

        for (int i = 0; i < shown; i++)
        {
            Factor factor = factors[i];
            int percent = Mathf.RoundToInt((float)factor.value / denominator * 100f);

            if (i > 0)
                builder.Append(", ");

            builder.Append(factor.name);
            builder.Append(" ");
            builder.Append(percent);
            builder.Append("%");
        }

        builder.Append("。復育行為已抵銷累積影響的 ");
        builder.Append(Mathf.RoundToInt(restorationRatio * 100f));
        builder.Append("%。");

        if (restorationKarma <= 0)
            builder.Append("目前尚未偵測到復育行為。");

        return builder.ToString();
    }

    private static string BuildRecommendation(Factor primary, KarmaSystem karma, float restorationRatio)
    {
        if (primary.name == "海洋開採")
            return "優先降低石油與天然氣開採，再用復育行為放緩環境崩壞曲線。";

        if (primary.name == "建設負荷")
            return "先限制工廠與房屋數量，等復育量追上建設造成的環境負荷。";

        if (primary.name == "礦產開採")
            return "降低高衝擊礦產開採，特別是大理石與鐵礦，再考慮擴張建設。";

        if (primary.name == "森林破壞")
            return "增加種樹與保護森林，因為森林流失是目前最強的因果驅動。";

        if (primary.name == "長期排放")
            return "模型判斷延遲性環境成本正在上升，建議減少工廠與燃料開採。";

        if (restorationRatio < 0.15f && karma.RestorationKarma <= 0)
            return "加入更多復育任務，讓玩家明確看見修復選擇會改變結果。";

        return "維持各類衝擊來源的平衡，並讓復育量保持在目前抵銷比例以上。";
    }

    private static string BuildReport(Result result)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("AI 因果分析");
        builder.AppendLine("風險預測：" + result.riskLevel);
        builder.AppendLine("主要因果驅動：" + result.mainCause);
        builder.AppendLine(result.explanation);
        builder.Append("建議行動：");
        builder.Append(result.recommendation);
        return builder.ToString();
    }
}
